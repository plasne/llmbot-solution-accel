using System.Collections.Generic;
using DistributedChat;

public class HistoryService
{
    private readonly Dictionary<string, List<Turn>> history = [];

    public void Add(string userId, string role, string msg)
    {
        var turn = new Turn
        {
            Role = role,
            Msg = msg
        };

        if (history.TryGetValue(userId, out List<Turn>? value))
        {
            value.Add(turn);
        }
        else
        {
            history[userId] = [turn];
        }
    }

    public List<Turn> Get(string userId)
    {
        if (history.TryGetValue(userId, out List<Turn>? value))
        {
            return value;
        }

        return [];
    }
}