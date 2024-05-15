using System.Collections.Generic;
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

    public static DistributedChat.Citation ToGrpcCitation(this Citation source)
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
}