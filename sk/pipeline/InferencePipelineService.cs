using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

public class InferencePipelineService(
    IConfig config,
    DefaultAzureCredential defaultAzureCredential,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider,
    ILogger<InferencePipelineService> logger)
    : IHostedService
{
    private readonly IConfig config = config;
    private readonly DefaultAzureCredential defaultAzureCredential = defaultAzureCredential;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<InferencePipelineService> logger = logger;
    private readonly IDeserializer yamlDeserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

    private async Task ProcessMessageAsync(
        PipelineRequest request,
        QueueClient evaluationQueueClient,
        CancellationToken cancellationToken = default)
    {
        // validate
        if (request.GroundTruthUri is null)
        {
            throw new Exception("ground_truth_uri is required.");
        }

        // attempt to download the ground truth file
        this.logger.LogDebug("attempting to download the ground truth file: {u}...", request.GroundTruthUri);
        var httpClient = this.httpClientFactory.CreateClient();
        var groundTruthResponse = await httpClient.GetAsync(request.GroundTruthUri, cancellationToken);
        var groundTruthBody = await groundTruthResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!groundTruthResponse.IsSuccessStatusCode)
        {
            throw new Exception($"{groundTruthResponse.StatusCode} : {groundTruthBody}");
        }

        // serialize the payload
        GroundTruthFile? inputFile;
        var inputFilepath = request.GroundTruthUri.Split("?").First();
        if (inputFilepath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
        {
            inputFile = JsonConvert.DeserializeObject<GroundTruthFile>(groundTruthBody)
                ?? throw new Exception($"could not deserialize ground truth file {request.GroundTruthUri} as JSON.");
        }
        else if (inputFilepath.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase))
        {
            inputFile = yamlDeserializer.Deserialize<GroundTruthFile>(groundTruthBody)
                ?? throw new Exception($"could not deserialize ground truth file {request.GroundTruthUri} as YAML.");
        }
        else
        {
            throw new Exception($"cannot determine ground truth file type for {request.GroundTruthUri}.");
        }
        this.logger.LogInformation("successfully downloaded the ground truth file: {u}.", request.GroundTruthUri);

        // build grounding data
        var turns = inputFile.History?.ToList();
        var userQuery = (turns?.LastOrDefault())
            ?? throw new Exception($"could not find user query in ground truth file {request.GroundTruthUri}.");
        turns?.Remove(userQuery);
        var groundingData = new GroundingData
        {
            UserQuery = userQuery.Msg,
            History = turns,
        };

        // process through the workflow
        using var scope = this.serviceProvider.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();
        var workflowResponse = await workflow.Execute(groundingData, cancellationToken);

        // build the evaluation request
        var generateAnswerStep = workflowResponse.Steps.FirstOrDefault(x => x.Name == "GenerateAnswer") as WorkflowStepResponse<IntentAndData, Answer>;
        var content = generateAnswerStep?.Input?.Data?.Content?.Where(x =>
            generateAnswerStep.Output?.Citations?.ToList().Any(y => y.Ref == x.Citation?.Ref) ?? false);
        var evaluationRequest = new InferenceFile
        {
            Ref = request.Ref,
            History = inputFile.History,
            GroundTruth = inputFile.GroundTruth,
            Answer = workflowResponse.Answer?.Text,
            Content = content?.ToList(),
        };

        // attempt to upload the inference file
        this.logger.LogDebug("attempting to upload the inference file: {u}...", request.InferenceUri);
        var jsonOptions = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
        var evaluationRequestFileJson = JsonConvert.SerializeObject(evaluationRequest, jsonOptions);
        using var inferenceRequest = new HttpRequestMessage(HttpMethod.Put, request.InferenceUri)
        {
            Content = new StringContent(evaluationRequestFileJson, Encoding.UTF8, "application/json")
        };
        inferenceRequest.Headers.Add("x-ms-blob-type", "BlockBlob");
        var inferenceResponse = await httpClient.SendAsync(inferenceRequest, cancellationToken);
        if (!inferenceResponse.IsSuccessStatusCode)
        {
            var outputBody = await inferenceResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"{inferenceResponse.StatusCode} : {outputBody}");
        }
        this.logger.LogInformation("successfully uploaded the inference file: {u}.", request.InferenceUri);

        // write to the evaluation queue
        this.logger.LogDebug("attempting to write to the evaluation queue...");
        var evaluationRequestJson = JsonConvert.SerializeObject(request);
        await evaluationQueueClient.SendMessageAsync(evaluationRequestJson, cancellationToken);
        this.logger.LogInformation("successfully wrote to the evaluation queue.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // start a background process for processing incoming requests via a queue
        var _ = Task.Run(async () =>
        {
            // create the connection
            var inferenceUrl = $"https://{this.config.AZURE_STORAGE_ACCOUNT_NAME}.queue.core.windows.net/{this.config.AZURE_STORAGE_INFERENCE_QUEUE}";
            var inferenceQueueClient = new QueueClient(new Uri(inferenceUrl), this.defaultAzureCredential);
            var evaluationUrl = $"https://{this.config.AZURE_STORAGE_ACCOUNT_NAME}.queue.core.windows.net/{this.config.AZURE_STORAGE_EVALUATION_QUEUE}";
            var evaluationQueueClient = new QueueClient(new Uri(evaluationUrl), this.defaultAzureCredential);

            // verify inference queue connectivity
            try
            {
                this.logger.LogDebug(
                    "attempting to authenticate to inference queue {q}...",
                    this.config.AZURE_STORAGE_INFERENCE_QUEUE);
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                var properties = await inferenceQueueClient.GetPropertiesAsync(cts.Token);
                this.logger.LogInformation(
                    "successfully authenticated to inference queue {q} and found ~{c} messages.",
                    this.config.AZURE_STORAGE_INFERENCE_QUEUE,
                    properties.Value.ApproximateMessagesCount);
            }
            catch (TaskCanceledException)
            {
                this.logger.LogError("The InferencePipelineService will NOT run - credentials could not be obtained to get to the AZURE_STORAGE_INFERENCE_QUEUE.");
                return;
            }

            // verify evaluation queue connectivity
            try
            {
                this.logger.LogDebug(
                    "attempting to authenticate to evaluation queue {q}...",
                    this.config.AZURE_STORAGE_EVALUATION_QUEUE);
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                var properties = await inferenceQueueClient.GetPropertiesAsync(cts.Token);
                this.logger.LogInformation(
                    "successfully authenticated to evaluation queue {q} and found ~{c} messages.",
                    this.config.AZURE_STORAGE_EVALUATION_QUEUE,
                    properties.Value.ApproximateMessagesCount);
            }
            catch (TaskCanceledException)
            {
                this.logger.LogError("The InferencePipelineService will NOT run - credentials could not be obtained to get to the AZURE_STORAGE_EVALUATION_QUEUE.");
                return;
            }

            // start the loop
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // look for a message
                    var message = await inferenceQueueClient.ReceiveMessageAsync(TimeSpan.FromMinutes(2), cancellationToken);
                    var body = message?.Value?.Body?.ToString();
                    if (string.IsNullOrEmpty(body))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        continue;
                    }

                    // process the message
                    var request = JsonConvert.DeserializeObject<PipelineRequest>(body)
                        ?? throw new Exception("could not deserialize inference request.");
                    await this.ProcessMessageAsync(request, evaluationQueueClient, cancellationToken);
                    await inferenceQueueClient.DeleteMessageAsync(message!.Value.MessageId, message.Value.PopReceipt, cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "error with message in the inference pipeline service...");
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}