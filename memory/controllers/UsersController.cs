using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Memory;

namespace Memory;

[Route("api/users/{userId}")]
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet("conversations/:last")]
    public async Task<ActionResult<Conversation>> GetLastConversationAsync(
        [FromRoute] string userId,
        [FromQuery(Name = "max-tokens")] int? maxTokens,
        [FromQuery(Name = "model-name")] string? modelName,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest($"User ID cannot be null or empty.");
        }
        var conversation = await store.GetLastConversationAsync(userId, maxTokens, modelName, cancellationToken);
        return Ok(conversation);
    }

    [HttpPost("conversations/:last/turns")]
    public async Task<ActionResult<StartGenerationResponse>> StartGenerationAsync(
        [FromRoute] string userId,
        [FromServices] IConfig config,
        [FromServices] IMemoryStore store,
        [FromBody] StartGenerationRequest body,
        CancellationToken cancellationToken)
    {
        var (req, res) = body.ToInteractions(userId);
        var conversationId = await store.StartGenerationAsync(req, res, config.DEFAULT_RETENTION, cancellationToken);
        return Ok(new StartGenerationResponse { ConversationId = conversationId });
    }

    [HttpPut("conversations/:last/turns/:last")]
    public async Task<IActionResult> CompleteGenerationAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] CompleteGenerationRequest body,
        CancellationToken cancellationToken)
    {
        await store.CompleteGenerationAsync(body.ToInteraction(userId), cancellationToken);
        return Ok();
    }

    [HttpPut("activities/{activityId}")]
    public async Task<IActionResult> UpdateUserMessage(
        [FromRoute] string userId,
        [FromRoute] string activityId,
        [FromServices] IMemoryStore store,
        [FromBody] UserMessageRequest body,
        CancellationToken cancellationToken)
    {
        await store.UpdateUserMessageAsync(body.ToInteraction(userId), cancellationToken);
        return Ok();
    }

    [HttpDelete("activities/{activityId}")]
    public async Task<IActionResult> DeleteActivityAsync(
        [FromRoute] string userId,
        [FromRoute] string activityId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        await store.DeleteActivityAsync(userId, activityId.Decode(), cancellationToken);
        return Ok();
    }

    [HttpDelete("activities/:last")]
    public async Task<ActionResult<List<DeletedUserMessage>>> DeleteLastActivitiesAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken,
        [FromQuery] int count = 1)
    {
        return new OkObjectResult(await store.DeleteActivitiesAsync(userId, count, cancellationToken));
    }

    [HttpPut("conversations")]
    public async Task<IActionResult> ChangeConversationTopicAsync(
        [FromRoute] string userId,
        [FromServices] IConfig config,
        [FromServices] IMemoryStore store,
        [FromBody] ChangeTopicRequest body,
        CancellationToken cancellationToken)
    {
        if (body.Intent != Intents.TOPIC_CHANGE)
        {
            return BadRequest("intent must be 'TOPIC_CHANGE'.");
        }

        await store.ChangeConversationTopicAsync(
            body.ToInteraction(userId),
            config.DEFAULT_RETENTION,
            cancellationToken);
        return Ok();
    }

    [HttpPut("activities/:last/feedback")]
    public async Task<IActionResult> RateMessageAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] FeedbackRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(body.Rating) && string.IsNullOrEmpty(body.Comment))
        {
            return BadRequest("you must supply at least one of rating or comment.");
        }

        if (!string.IsNullOrEmpty(body.Rating))
        {
            await store.RateLastMessageAsync(userId, body.Rating, cancellationToken);
        }

        if (!string.IsNullOrEmpty(body.Comment))
        {
            await store.CommentOnLastMessageAsync(userId, body.Comment, cancellationToken);
        }

        return Ok();
    }

    [HttpPut("activities/{activityId}/feedback")]
    public async Task<IActionResult> RateMessageAsync(
        [FromRoute] string userId,
        [FromRoute] string activityId,
        [FromServices] IMemoryStore store,
        [FromBody] FeedbackRequest body,
        CancellationToken cancellationToken)
    {
        var decodedActivityId = activityId.Decode();
        if (string.IsNullOrEmpty(body.Rating) && string.IsNullOrEmpty(body.Comment))
        {
            return BadRequest("you must supply at least one of rating or comment.");
        }

        if (!string.IsNullOrEmpty(body.Rating))
        {
            await store.RateMessageAsync(userId, decodedActivityId, body.Rating, cancellationToken);
        }

        if (!string.IsNullOrEmpty(body.Comment))
        {
            await store.CommentOnMessageAsync(userId, decodedActivityId, body.Comment, cancellationToken);
        }

        return Ok();
    }

    [HttpGet("activities/:last/feedback")]
    public async Task<ActionResult<Interaction>> GetFeedbackAsync(
            [FromRoute] string userId,
            [FromServices] IMemoryStore store,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return this.BadRequest("userId cannot be null or empty.");
        }

        var interaction = await store.GetLastInteractionAsync(userId, cancellationToken);
        if (string.IsNullOrEmpty(interaction?.ActivityId))
        {
            return NotFound($"Last Interaction for user ID '{userId}' not found.");
        }

        var feedback = interaction.ToFeedBackRequest();

        return Ok(feedback);
    }

    [HttpGet("activities/{activityId}/feedback")]
    public async Task<ActionResult<Interaction>> GetFeedbackAsync(
            [FromRoute] string userId,
            [FromRoute] string activityId,
            [FromServices] IMemoryStore store,
            CancellationToken cancellationToken)
    {
        var decodedActivityId = activityId.Decode();
        if (string.IsNullOrEmpty(decodedActivityId))
        {
            return this.BadRequest("activityId cannot be null or empty.");
        }

        if (string.IsNullOrEmpty(userId))
        {
            return this.BadRequest("userId cannot be null or empty.");
        }

        var interaction = await store.GetInteractionAsync(userId, decodedActivityId, cancellationToken);

        if (string.IsNullOrEmpty(interaction?.ActivityId))
        {
            return NotFound($"Last Interaction for user ID '{userId}' not found.");
        }

        var feedback = interaction.ToFeedBackRequest();
        return Ok(feedback);
    }

    [HttpDelete("activities/:last/feedback")]
    public async Task<IActionResult> ClearFeedbackAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        await store.ClearLastFeedbackAsync(userId, cancellationToken);
        return Ok();
    }

    [HttpDelete("activities/{activityId}/feedback")]
    public async Task<IActionResult> ClearFeedbackAsync(
        [FromRoute] string userId,
        [FromRoute] string activityId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        await store.ClearFeedbackAsync(userId, activityId.Decode(), cancellationToken);
        return Ok();
    }

    [HttpPut("instructions")]
    public async Task<IActionResult> SetCustomInstructionsAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] CustomInstructions body,
        CancellationToken cancellationToken)
    {
        await store.SetCustomInstructionsAsync(userId, body, cancellationToken);
        return Ok();
    }

    [HttpDelete("instructions")]
    public async Task<IActionResult> DeleteCustomInstructionsAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        await store.DeleteCustomInstructionsAsync(userId, cancellationToken);
        return Ok();
    }

    [HttpGet("instructions")]
    public async Task<ActionResult<CustomInstructions>> GetCustomInstructionsAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        var instructions = await store.GetCustomInstructionsAsync(userId, cancellationToken);
        return Ok(instructions);
    }
}