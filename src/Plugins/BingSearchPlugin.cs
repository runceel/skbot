using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.Linq;
using Newtonsoft.Json;

namespace Plugins;

public class BingSearchPlugin
{
    private readonly HttpClient _client;
    private readonly string _bingSearchApiKey;
    private readonly Func<string, Task> _eventCallback;

    public BingSearchPlugin(IConfiguration config, IHttpClientFactory httpClientFactory, Func<string, Task> eventCallback)
    {
        _bingSearchApiKey = config.GetValue<string>("BING_SEARCH_API_KEY")!;
        _client = httpClientFactory.CreateClient();
        _eventCallback = eventCallback;
    }

    [SKFunction, Description("Bingでインターネット上の情報を検索します。")]
    public async Task<string> BingSearch([Description("検索に使用するクエリ")] string query)
    {
        string url = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}";
        await _eventCallback.Invoke($"{query} について Bing でインターネット上の情報を検索しています。");

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", _bingSearchApiKey);
        var response = await _client.SendAsync(requestMessage);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            // JSONを解析して必要な情報を取り出す
            var searchResult = JsonConvert.DeserializeObject<BingSearchResult>(content);
            // 必要な情報を文字列に変換して返す
            return string.Join(", ", searchResult?.WebPages?.Value?.Select(page => page.Snippet) ?? []);
        }
        else
        {
            return $"Error: {response.StatusCode}";
        }
    }
}

public class BingSearchResult
{
    public WebPages? WebPages { get; set; }
}

public class WebPages
{
    public List<Value>? Value { get; set; }
}

public class Value
{
    public string? Name { get; set; }
    public string? Snippet { get; set; }
}