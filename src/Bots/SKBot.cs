// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using SKBot.AI;

namespace Microsoft.BotBuilderSamples;

public class SKBot : StateManagementBot
{
    private readonly IAIAssistant _aiAssistant;

    public SKBot(IAIAssistant aiAssistant, 
        IConfiguration config,
        ConversationState conversationState, 
        UserState userState) : base(config, conversationState, userState)
    {
        _aiAssistant = aiAssistant;
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, 
        ITurnContext<IConversationUpdateActivity> turnContext, 
        CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync("GPTBot サンプルへようこそ。開始するには、何か入力してください。");
    }

    public override async Task<string> ProcessMessage(
        ConversationData conversationData, 
        ITurnContext<IMessageActivity> turnContext)
    {
        var prompt = FormatConversationHistory(conversationData);
        return await _aiAssistant.AskAsync(goal: prompt, 
            eventCallback: message => turnContext.SendActivityAsync(message));
    }
}
