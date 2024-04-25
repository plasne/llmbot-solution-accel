using System.Collections.Generic;

public interface IConversation
{
    string ConversationId { get; set; }

    SortedList<int, IInteraction> Interactions { get; set; }
}