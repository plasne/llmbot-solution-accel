using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models.Memory;

namespace Inference;

public static class Ext
{
    public static ChatHistory ToChatHistory(this IEnumerable<Turn> turns)
    {
        var history = new ChatHistory();

        foreach (var turn in turns)
        {
            switch (turn.Role)
            {
                case Roles.ASSISTANT:
                    history.AddAssistantMessage(turn.Msg);
                    break;
                case Roles.USER:
                    history.AddUserMessage(turn.Msg);
                    break;
            }
        }

        return history;
    }

    public static DistributedChat.Citation ToGrpcCitation(this Context source)
    {
        var target = new DistributedChat.Citation
        {
            Id = source.Id,
            Title = source.Title,
        };
        if (!string.IsNullOrEmpty(source.Uri))
        {
            target.Uri = source.Uri;
        }
        return target;
    }

    public static decimal AsDecimal(this string str, Func<decimal> dflt)
    {
        if (decimal.TryParse(str, out decimal val)) return val;
        return dflt();
    }

    public static List<LlmConnectionDetails> AsLlmConnectionDetails(this string str, Func<List<LlmConnectionDetails>> dflt)
    {
        try
        {
            var list = new List<LlmConnectionDetails>();
            var connectionStrings = str.Split(";;");
            foreach (var connectionString in connectionStrings)
            {
                var parts = connectionString
                    .Split(';')
                    .Select(part => part.Split('='))
                    .ToDictionary(split => split[0].Trim(), split => split[1].Trim());
                var details = new LlmConnectionDetails
                {
                    DeploymentName = parts["DeploymentName"],
                    Endpoint = parts["Endpoint"],
                    ApiKey = parts["ApiKey"],
                };
                list.Add(details);
            }
            return list;
        }
        catch
        {
            return dflt();
        }
    }

    public static WorkflowRequestParameters? ToParameters(this IHeaderDictionary headers)
    {
        var parameters = new WorkflowRequestParameters();
        foreach (var header in headers)
        {
            switch (header.Key.ToUpper())
            {
                case "X-INTENT-PROMPT-FILE":
                    parameters.INTENT_PROMPT_FILE = header.Value;
                    break;
                case "X-CHAT-PROMPT-FILE":
                    parameters.CHAT_PROMPT_FILE = header.Value;
                    break;
            }
        }
        return parameters;
    }
}