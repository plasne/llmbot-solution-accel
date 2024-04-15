using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WeatherForecast;
using static WeatherForecast.WeatherForecasts;

public class WeatherService : WeatherForecastsBase
{
    private readonly WeatherForecaster forecaster;

    public WeatherService(WeatherForecaster forecaster)
    {
        this.forecaster = forecaster;
    }

    public override async Task GetWeatherStream(
        Empty _,
        IServerStreamWriter<WeatherData> responseStream,
        ServerCallContext context)
    {
        try
        {
            await foreach (var chunk in this.forecaster.GetForecast())
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var forecast = new WeatherData
                {
                    Summary = chunk.ToString()
                };
                await responseStream.WriteAsync(forecast);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}