using Shared.Models.Memory;

public class ChangeTopicRequest(string activityId) : IChangeTopicRequest
{
    public string ActivityId { get; set; } = activityId;
}