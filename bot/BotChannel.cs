namespace Channels;

using System;
using Grpc.Net.Client;
using static BoardGameChat.BoardGameChats;

public class BotChannel : IDisposable
{
    private readonly GrpcChannel channel;
    private readonly BoardGameChatsClient client;
    private bool disposed = false;

    public BoardGameChatsClient Client => client;

    public BotChannel(IConfig config)
    {
        this.channel = GrpcChannel.ForAddress(config.LLM_URI);
        this.client = new BoardGameChatsClient(channel);
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