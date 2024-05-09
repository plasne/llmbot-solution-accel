using System.Collections.Generic;
using Shared.Models.Memory;

public class GroundingData(string userQuery) : IGroundingData
{
    public IList<IDoc>? Docs { get; set; }
    public IList<IContent>? Content { get; set; }
    public string UserQuery { get; set; } = userQuery;
    public IList<ITurn>? History { get; set; }
}