using System;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;

namespace Plugins;

public class SQLPlugin
{
    private readonly string connectionString;
    private readonly Func<string, Task> _eventCallback;

    public SQLPlugin(IConfiguration config, Func<string, Task> eventCallback)
    {
        connectionString = config.GetValue<string>("SQL_CONNECTION_STRING")!;
        _eventCallback = eventCallback;
    }




    [SKFunction, Description("顧客データと売上データを含む AdventureWorksLT のテーブル名を取得します。ユーザーが正しい名前を述べたと想定するのではなく、他のクエリを実行する前に必ずこれを実行してください。販売員情報は Customer テーブルに含まれていることを忘れないでください。")]
    public async Task<string> GetTables()
    {
        await _eventCallback($"AdventureWorksLT のテーブル名の取得をしています。");
        return await QueryAsCSV($"SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;");
    }



    [SKFunction, Description("AdventureWorksLT のテーブルのデータベース スキーマを取得します。")]
    public async Task<string> GetSchema(
        [Description("スキーマを取得するテーブル。スキーマ名は含めないでください。")] string tableName
    )
    {
        await _eventCallback($"AdventureWorksLT の {tableName} のスキーマを取得をしています。");
        return await QueryAsCSV($"SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}';");
    }



    [SKFunction, Description("AdventureWorksLT データベースに対して SQL を実行する")]
    public async Task<string> RunQuery(
        [Description("SQL Server で実行するクエリ。テーブルを参照するときは、必ずスキーマ名を追加してください。")] string query
    )
    {
        await _eventCallback($"AdventureWorksLT の {query} を実行しています。");
        return await QueryAsCSV(query);
    }




    private async Task<string> QueryAsCSV(string query)
    {
        var output = "[DATABASE RESULTS] \n";
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        try
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                output += reader.GetName(i);
                if (i < reader.FieldCount - 1)
                    output += ",";
            }
            output += "\n";
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    output += reader[columnName].ToString();
                    if (i < reader.FieldCount - 1)
                        output += ",";
                }
                output += "\n";
            }
        }
        catch (Exception e)
        {
            return $"[DATABASE RESULTS]\n{e.Message}";
        }

        return output;
    }

}