using System;
using Grpc.Net.Client;
using static DistributedChat.ChatService;

namespace Bot;

public class BotChannel : IDisposable
{
    private readonly GrpcChannel channel;
    private readonly ChatServiceClient client;
    private bool disposed = false;

    public ChatServiceClient Client => client;

    public BotChannel(IConfig config)
    {
        this.channel = GrpcChannel.ForAddress(config.INFERENCE_URL);
        this.client = new ChatServiceClient(channel);
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