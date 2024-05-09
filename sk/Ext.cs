using System.Collections.Generic;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models.Memory;

public static class Ext
{
    public static ChatHistory ToChatHistory(this IEnumerable<ITurn> turns)
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
}