public class Doc : IDoc
{
    public double SearchScore { get; set; }
    public string? ChunkId { get; set; }
    public string? Chunk { get; set; }
    public string? Title { get; set; }
}