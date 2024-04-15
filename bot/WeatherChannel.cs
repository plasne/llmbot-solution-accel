namespace Weather;

using System;
using Grpc.Net.Client;
using static WeatherForecast.WeatherForecasts;

public class WeatherChannel : IDisposable
{
    private readonly GrpcChannel channel;
    private readonly WeatherForecastsClient client;
    private bool disposed = false;

    public WeatherForecastsClient Client => client;

    public WeatherChannel(IConfig config)
    {
        this.channel = GrpcChannel.ForAddress(config.LLM_URI);
        this.client = new WeatherForecastsClient(channel);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                this.channel?.Dispose();
            }
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}