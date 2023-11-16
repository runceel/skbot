// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Model;
using Newtonsoft.Json;
using Plugins;
using SKBot.AI;
using SKBot.Plugins;

namespace Microsoft.BotBuilderSamples
{
    public class SKBot : StateManagementBot
    {
        private readonly AzureOpenAITextEmbeddingGeneration _embeddingClient;
        private readonly DocumentAnalysisClient _documentAnalysisClient;
        private readonly IAIAssistant _aiAssistant;

        public SKBot(IAIAssistant aiAssistant, 
            IConfiguration config,
            ConversationState conversationState, 
            UserState userState) : base(config, conversationState, userState)
        {
            var aoaiApiKey = config.GetValue<string>("AOAI_API_KEY");
            var aoaiApiEndpoint = config.GetValue<string>("AOAI_API_ENDPOINT");
            _embeddingClient = new AzureOpenAITextEmbeddingGeneration(modelId: "text-embedding-ada-002", aoaiApiEndpoint, aoaiApiKey);
            _aiAssistant = aiAssistant;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("GPTBot サンプルへようこそ。開始するには、何か入力してください。");
        }

        public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new ThoughtLoggerProvider(true, turnContext));
            });
            var prompt = FormatConversationHistory(conversationData);
            return await _aiAssistant.AskAsync(prompt, loggerFactory);
        }

        private async Task<string> HandleFileUpload(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            Uri fileUri = new Uri(turnContext.Activity.Attachments.First(x => x.ContentUrl != null).ContentUrl);

            var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(fileUri);

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            var operation = await _documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", ms);
            
            ms.Dispose();

            AnalyzeResult result = operation.Value;

            var attachment = new Attachment();
            foreach (DocumentPage page in result.Pages)
            {
                var attachmentPage = new AttachmentPage();
                attachmentPage.Content = "";
                for (int i = 0; i < page.Lines.Count; i++)
                {
                    DocumentLine line = page.Lines[i];
                    attachmentPage.Content += $"{line.Content}\n";
                }
                // Embed content
                var embedding = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { attachmentPage.Content });
                attachmentPage.Vector = embedding.First().ToArray();
                attachment.Pages.Add(attachmentPage);
            }
            conversationData.Attachments.Add(attachment);

            return $"File {turnContext.Activity.Attachments[0].Name} uploaded successfully! {result.Pages.Count()} pages ingested.";
        }
    }
}
