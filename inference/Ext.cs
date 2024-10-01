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
        if (source.Uris is not null)
        {
            target.Uris.AddRange(source.Uris);
        }
        return target;
    }

    public static decimal AsDecimal(this string str, Func<decimal> dflt)
    {
        return decimal.TryParse(str, out decimal val)
            ? val
            : dflt();
    }

    public static decimal? AsOptionalDecimal(this string str, Func<decimal?> dflt)
    {
        return decimal.TryParse(str, out decimal val)
            ? val
            : dflt();
    }

    public static long? AsOptionalLong(this string str, Func<long?> dflt)
    {
        return long.TryParse(str, out long val)
            ? val
            : dflt();
    }

    public static SearchMode AsSearchMode(this string str, Func<SearchMode> dflt)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return dflt();
        }

        return Enum.TryParse(str, true, out SearchMode searchMode)
            ? searchMode
            : throw new ArgumentException($"Unknown SearchMode: {str}");
    }

    public static List<ModelConnectionDetails> AsModelConnectionDetails(this string str, Func<List<ModelConnectionDetails>> dflt)
    {
        try
        {
            var list = new List<ModelConnectionDetails>();
            var connectionStrings = str.Split(";;");
            foreach (var connectionString in connectionStrings)
            {
                var parts = connectionString
                    .Split(';')
                    .Select(part => part.Split('='))
                    .ToDictionary(split => split[0].Trim(), split => split[1].Trim());
                var details = new ModelConnectionDetails
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
                case "X-INTENT-TEMPERATURE":
                    parameters.INTENT_TEMPERATURE = header.Value.ToString().AsOptionalDecimal(() => null);
                    break;
                case "X-CHAT-TEMPERATURE":
                    parameters.CHAT_TEMPERATURE = header.Value.ToString().AsOptionalDecimal(() => null);
                    break;
            }
        }
        return parameters;
    }
}