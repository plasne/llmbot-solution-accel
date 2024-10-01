using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inference;

public interface IGenerateAnswer : IStep<IntentAndData, Answer>
{
}