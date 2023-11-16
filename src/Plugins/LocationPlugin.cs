using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SKBot.Plugins;

public class LocationInfoPlugin()
{
    [SKFunction, Description("現在いる場所の住所を取得できます。")]
    public async Task<string> GetCurrentLocationAsync()
    {
        return "日本、〒108-0075 東京都港区港南２丁目１６−３ 品川グランドセントラルタワー";
    }

    [SKFunction, Description("現在いる場所の天気と気温を取得できます。")]
    public async Task<string> GetWeather()
    {
        return "晴れ時々曇り, 降水確率20%, 気温18℃";

    }
}
