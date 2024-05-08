namespace Shared.Models.Memory;

public interface IFeedbackRequest
{
    public string ActivityId { get; set; }
    public string Rating { get; set; }
    public string Comment { get; set; }
}