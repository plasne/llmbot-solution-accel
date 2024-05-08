using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Memory;

[Route("api/users/{userId}")]
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet("conversations/current")]
    public async Task<ActionResult<IConversation>> GetCurrentConversationAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        // TODO: support max-size
        var conversation = await store.GetCurrentConversationAsync(userId, cancellationToken);
        return Ok(conversation);
    }

    [HttpPost("conversations/current/turns")]
    public async Task<IActionResult> StartGenerationAsync(
        [FromRoute] string userId,
        [FromServices] IConfig config,
        [FromServices] IMemoryStore store,
        [FromBody] IStartGenerationRequest body,
        CancellationToken cancellationToken)
    {
        var (req, res) = body.ToInteractions(userId);
        await store.StartGenerationAsync(req, res, config.DEFAULT_RETENTION, cancellationToken);
        return Ok();
    }

    [HttpPut("conversations/current/turns")]
    public async Task<IActionResult> CompleteGenerationAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] ICompleteGenerationRequest body,
        CancellationToken cancellationToken)
    {
        await store.CompleteGenerationAsync(body.ToInteraction(userId), cancellationToken);
        return Ok();
    }

    [HttpDelete("turns")]
    public async Task<IActionResult> DeleteLastInteractionsAsync(
    [FromRoute] string userId,
    [FromServices] IMemoryStore store,
    CancellationToken cancellationToken,
    [FromQuery] int count = 1)
    {
        await store.DeleteLastInteractionsAsync(userId, count, cancellationToken);
        return Ok();
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> ChangeConversationTopicAsync(
        [FromRoute] string userId,
        [FromServices] IConfig config,
        [FromServices] IMemoryStore store,
        [FromBody] IChangeTopicRequest body,
        CancellationToken cancellationToken)
    {
        await store.ChangeConversationTopicAsync(
            body.ToInteraction(userId),
            config.DEFAULT_RETENTION,
            cancellationToken);
        return Ok();
    }

    [HttpPost("conversations/current/feedback")]
    public async Task<IActionResult> RateMessageAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] IFeedbackRequest body,
        CancellationToken cancellationToken)
    {
        var hasActivity = string.IsNullOrEmpty(body.ActivityId);
        var hasRating = string.IsNullOrEmpty(body.Rating);
        var hasComment = string.IsNullOrEmpty(body.Comment);
        if (!hasActivity && !hasRating && !hasComment)
        {
            return BadRequest("you must supply at least one of activityId, rating, or comment.");
        }

        if (hasRating)
        {
            await (hasActivity
                ? store.RateMessageAsync(userId, body.ActivityId, body.Rating, cancellationToken)
                : store.RateMessageAsync(userId, body.Rating, cancellationToken));
        }

        if (hasComment)
        {
            await (hasActivity
                ? store.CommentOnMessageAsync(userId, body.ActivityId, body.Rating, cancellationToken)
                : store.CommentOnMessageAsync(userId, body.Rating, cancellationToken));
        }

        return Ok();
    }

    [HttpDelete("conversations/current/feedback")]
    public async Task<IActionResult> ClearFeedbackAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] IClearFeedbackRequest body,
        CancellationToken cancellationToken)
    {
        var hasActivity = string.IsNullOrEmpty(body.ActivityId);
        await (hasActivity
            ? store.ClearFeedbackAsync(userId, body.ActivityId, cancellationToken)
            : store.ClearFeedbackAsync(userId, cancellationToken));
        return Ok();
    }

    [HttpPut("instructions")]
    public async Task<IActionResult> SetCustomInstructionsAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        [FromBody] ICustomInstructions body,
        CancellationToken cancellationToken)
    {
        await store.SetCustomInstructionsAsync(userId, body.Prompt, cancellationToken);
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
    public async Task<ActionResult<ICustomInstructions>> GetCustomInstructionsAsync(
        [FromRoute] string userId,
        [FromServices] IMemoryStore store,
        CancellationToken cancellationToken)
    {
        var instructions = await store.GetCustomInstructionsAsync(userId, cancellationToken);
        return Ok(instructions);
    }
}