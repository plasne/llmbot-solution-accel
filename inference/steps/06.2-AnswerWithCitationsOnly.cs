using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Shared;
using SharpToken;
using Xunit.Sdk;

namespace Inference;

public partial class AnswerWithCitationsOnly(
    IWorkflowContext context,
    KernelFactory kernelFactory,
    IMemory memory,
    ILogger<GenerateAnswerWithLlm> logger)
    : BaseStep<IntentAndData, Answer>(logger), IGenerateAnswer
{
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;
    private readonly IMemory memory = memory;

    public override string Name => "GenerateAnswer";

    public override async Task<Answer> ExecuteInternal(
        IntentAndData input,
        CancellationToken cancellationToken = default)
    {
        List<Context> citations = input.Data?.Context?.ToList() ?? [];
        var msg = "Please review citations...";
        await this.context.Stream("Generated.", msg, citations: citations);
        this.Continue = !this.context.Config.EXIT_WHEN_NO_CITATIONS || citations.Any();
        return new Answer { Text = msg, Context = citations };
    }
}