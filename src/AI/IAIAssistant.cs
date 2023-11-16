using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Services;
using Model;
using Newtonsoft.Json;
using Plugins;
using SKBot.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SKBot.AI;

public interface IAIAssistant
{
    Task<string> AskAsync(string goal, ILoggerFactory? loggerFactory = null);
}

public class AIAssistant : IAIAssistant
{
    private readonly bool _isDebug;
    private readonly string _aoaiApiKey;
    private readonly string _aoaiApiEndpoint;
    private readonly string _aoaiModel;
    private readonly IConfiguration _configuration;

    public AIAssistant(IConfiguration configuration)
    {
        _configuration = configuration;
        _isDebug = _configuration.GetValue<bool>("DEBUG");
        _aoaiApiKey = _configuration.GetValue<string>("AOAI_API_KEY");
        _aoaiApiEndpoint = _configuration.GetValue<string>("AOAI_API_ENDPOINT");
        _aoaiModel = _configuration.GetValue<string>("AOAI_MODEL");
    }

    private IStepwisePlanner CreatePlanner(IKernel kernel)
    {
        var stepwiseConfig = new StepwisePlannerConfig
        {
            GetPromptTemplate = new StreamReader("./PromptConfig/StepwiseStepPrompt.json").ReadToEnd,
            MaxIterations = 15
        };

        return new StepwisePlanner(kernel, stepwiseConfig);
    }

    private IKernel CreateKernel(ILoggerFactory? loggerFactory = null)
    {
        var builder = new KernelBuilder()
            .WithAzureOpenAIChatCompletionService(
                deploymentName: _aoaiModel,
                endpoint: _aoaiApiEndpoint,
                apiKey: _aoaiApiKey
            );
        if (loggerFactory is not null)
        {
            builder.WithLoggerFactory(loggerFactory);
        }

        var kernel = builder.Build();
        kernel.ImportFunctions(new TimePlugin(), "TimePlugin");
        kernel.ImportFunctions(new LocationInfoPlugin(), nameof(LocationInfoPlugin));
        if (!_configuration.GetValue<string>("SQL_CONNECTION_STRING").IsNullOrEmpty()) 
            kernel.ImportFunctions(new SQLPlugin(_configuration), "SQLPlugin");
        if (!_configuration.GetValue<string>("BING_SEARCH_API_KEY").IsNullOrEmpty()) 
            kernel.ImportFunctions(new BingSearchPlugin(_configuration), nameof(BingSearchPlugin));

        return kernel;
    }

    public async Task<string> AskAsync(string goal, ILoggerFactory loggerFactory)
    {
        var kernel = CreateKernel(loggerFactory);
        var planner = CreatePlanner(kernel);
        var plan = planner.CreatePlan(goal);
        var res = await kernel.RunAsync(plan);
        return res.GetValue<string>() ?? "";
    }
}
