using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Plugins.Core;
using Plugins;
using SKBot.Plugins;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SKBot.AI;

public interface IAIAssistant
{
    Task<string> AskAsync(string goal, Func<string, Task> eventCallback);
}

public class AIAssistant : IAIAssistant
{
    private readonly bool _isDebug;
    private readonly string _aoaiApiKey;
    private readonly string _aoaiApiEndpoint;
    private readonly string _aoaiModel;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIAssistant(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _aoaiApiKey = _configuration.GetValue<string>("AOAI_API_KEY") ?? throw new ArgumentNullException("configuration.AOAI_API_KEY");
        _aoaiApiEndpoint = _configuration.GetValue<string>("AOAI_API_ENDPOINT") ?? throw new ArgumentNullException("configuration.AOAI_API_ENDPOINT");
        _aoaiModel = _configuration.GetValue<string>("AOAI_MODEL") ?? throw new ArgumentNullException("configuration.AOAI_MODEL");
    }

    private FunctionCallingStepwisePlanner CreatePlanner(IKernel kernel)
    {
        var stepwiseConfig = new FunctionCallingStepwisePlannerConfig
        {
            MaxIterations = 15,
            MaxTokens = 7000,
        };
        return new FunctionCallingStepwisePlanner(kernel, stepwiseConfig);
    }

    private IKernel CreateKernel(Func<string, Task> eventCallback)
    {
        var builder = new KernelBuilder()
            .WithAzureOpenAIChatCompletionService(
                deploymentName: _aoaiModel,
                endpoint: _aoaiApiEndpoint,
                apiKey: _aoaiApiKey
            );

        var kernel = builder.Build();
        kernel.ImportFunctions(new TimePlugin(), nameof(TimePlugin));
        kernel.ImportFunctions(new LocationInfoPlugin(eventCallback), nameof(LocationInfoPlugin));
        kernel.ImportFunctions(new SQLPlugin(_configuration, eventCallback), nameof(SQLPlugin));
        kernel.ImportFunctions(new BingSearchPlugin(_configuration, _httpClientFactory, eventCallback), nameof(BingSearchPlugin));
        return kernel;
    }

    public async Task<string> AskAsync(string goal, Func<string, Task> eventCallback)
    {
        var kernel = CreateKernel(eventCallback);
        var planner = CreatePlanner(kernel);
        var res = await planner.ExecuteAsync(goal);
        return res.FinalAnswer;
    }
}
