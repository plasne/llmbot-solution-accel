using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.Collections.Generic;
using System.IO;

public class WeatherForecaster
{
    private Kernel kernel;

    public WeatherForecaster(IConfig config)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            config.LLM_DEPLOYMENT_NAME,
            config.LLM_ENDPOINT_URI,
            config.LLM_API_KEY);
        this.kernel = builder.Build();
    }

    public IAsyncEnumerable<StreamingKernelContent> GetForecast()
    {
        var forecastTemplate = File.ReadAllText("forecastTemplate.txt");
        var forecastFunction = this.kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = forecastTemplate,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        return this.kernel.InvokeStreamingAsync(
            forecastFunction
        );
    }
}