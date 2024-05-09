using System.Collections.Generic;
using Shared.Models.Memory;

public class InferenceFile : IInferenceFile
{
    public string? Ref { get; set; }
    public IList<ITurn>? History { get; set; }
    public string? GroundTruth { get; set; }
    public string? Answer { get; set; }
    public IList<IContent>? Content { get; set; }
}