using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inference;

public interface IGetDocuments : IStep<DeterminedIntent, List<Doc>>
{
}