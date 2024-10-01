using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Shared.Models.Memory;

namespace Memory;

public class Interaction
{
    public Guid ConversationId { get; set; }

    public string? ActivityId { get; set; }

    public string? UserId { get; set; }

    public Roles Role { get; set; }

    public string? Message { get; set; }

    public string? Citations { get; set; }

    public Intents Intent { get; set; }

    public States State { get; set; }

    public string? Rating { get; set; }

    public string? Comment { get; set; }

    public int PromptTokenCount { get; set; }

    public int CompletionTokenCount { get; set; }

    public int EmbeddingTokenCount { get; set; }

    public int TimeToFirstResponse { get; set; }

    public int TimeToLastResponse { get; set; }

    public static async Task<Interaction> FromReader(SqlDataReader reader, CancellationToken cancellationToken = default)
    {
        var interaction = new Interaction();
        if (!await reader.IsDBNullAsync(0, cancellationToken))
        {
            interaction.ActivityId = reader.GetString(0);
        }
        if (!await reader.IsDBNullAsync(1, cancellationToken))
        {
            interaction.Message = reader.GetString(1);
        }
        if (!await reader.IsDBNullAsync(2, cancellationToken))
        {
            interaction.Citations = reader.GetString(2);
        }
        if (!await reader.IsDBNullAsync(3, cancellationToken))
        {
            interaction.Rating = reader.GetString(3);
        }
        if (!await reader.IsDBNullAsync(4, cancellationToken))
        {
            interaction.Comment = reader.GetString(4);
        }
        return interaction;
    }
}