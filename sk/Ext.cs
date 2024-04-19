using System.Collections.Generic;
using DistributedChat;
using Microsoft.SemanticKernel.ChatCompletion;

public static class Ext
{
    public static ChatHistory ToChatHistory(this List<Turn> turns)
    {
        var history = new ChatHistory();

        foreach (var turn in turns)
        {
            switch (turn.Role)
            {
                case "assistant":
                    history.AddAssistantMessage(turn.Msg);
                    break;
                case "user":
                    history.AddUserMessage(turn.Msg);
                    break;
            }
        }

        return history;
    }
}