using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inference;

public interface IPickDocuments : IStep<DeterminedIntent, List<Doc>>
{
}