using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotBuilderSamples;
using SKBot.AI;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Services.AddHttpClient();
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// Create the Bot Framework Authentication to be used with the Bot Adapter.
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Create the Bot Adapter with error handling enabled.
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// Create the storage we'll be using for User and Conversation state.
// (Memory is great for testing purposes - examples of implementing storage with
// Azure Blob Storage or Cosmos DB are below).
// var storage = new MemoryStorage();

/* AZURE BLOB STORAGE - Uncomment the code in this section to use Azure blob storage */

// var storage = new BlobsStorage("<blob-storage-connection-string>", "bot-state");

/* END AZURE BLOB STORAGE */

/* COSMOSDB STORAGE - Uncomment the code in this section to use CosmosDB storage */

IStorage storage;
if (Environment.GetEnvironmentVariable("COSMOS_API_ENDPOINT") != null)
{
    var cosmosDbStorageOptions = new CosmosDbPartitionedStorageOptions()
    {
        CosmosDbEndpoint = Environment.GetEnvironmentVariable("COSMOS_API_ENDPOINT"),
        AuthKey = Environment.GetEnvironmentVariable("COSMOS_API_KEY"),
        DatabaseId = "SKBot",
        ContainerId = "Conversations"
    };
    storage = new CosmosDbPartitionedStorage(cosmosDbStorageOptions);
}
else
{
    storage = new MemoryStorage();
}

/* END COSMOSDB STORAGE */

// Create the User state passing in the storage layer.
var userState = new UserState(storage);
builder.Services.AddSingleton(userState);

// Create the Conversation state passing in the storage layer.
var conversationState = new ConversationState(storage);
builder.Services.AddSingleton(conversationState);

// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
builder.Services.AddTransient<IBot, Microsoft.BotBuilderSamples.SKBot>();

builder.Services.AddSingleton<IAIAssistant, AIAssistant>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
