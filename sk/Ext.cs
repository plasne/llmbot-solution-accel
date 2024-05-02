using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using DistributedChat;
using Microsoft.SemanticKernel.ChatCompletion;

public static partial class Ext
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

    public static List<ChatRequestMessage> ToChatRequestMessages(this string template)
    {
        var messages = new List<ChatRequestMessage>();
        var regex = MessageRegex();
        var matches = regex.Matches(template);

        foreach (Match match in matches)
        {
            var role = match.Groups[1].Value.ToLower();
            var text = match.Groups[2].Value;

            switch (role)
            {
                case "system":
                    messages.Add(new ChatRequestSystemMessage(text));
                    break;
                case "user":
                    messages.Add(new ChatRequestUserMessage(text));
                    break;
                case "assistant":
                    messages.Add(new ChatRequestAssistantMessage(text));
                    break;
            }
        }

        return messages;
    }

    [GeneratedRegex("<message role=\"(.*?)\">(.*?)</message>", RegexOptions.Singleline)]
    private static partial Regex MessageRegex();
}