using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SKBot.Plugins;

public class LocationInfoPlugin
{
    private readonly Func<string, Task> _eventCallback;

    public LocationInfoPlugin(Func<string, Task> eventCallback)
    {
        _eventCallback = eventCallback;
    }

    [SKFunction, Description("現在いる場所の住所を取得できます。")]
    public async Task<string> GetCurrentLocation()
    {
        await _eventCallback($"現在地を取得しています。");
        return "日本、〒108-0075 東京都港区港南２丁目１６−３ 品川グランドセントラルタワー";
    }

    [SKFunction, Description("現在いる場所の天気と気温を取得できます。")]
    public async Task<string> GetWeather()
    {
        await _eventCallback($"現在地の天気を取得しています。");
        return "晴れ時々曇り, 降水確率20%, 気温18℃";
    }
}
