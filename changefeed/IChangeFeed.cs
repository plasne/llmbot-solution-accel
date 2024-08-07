namespace ChangeFeed;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This class allows an application to notify on change or be notified of changes.
/// </summary>
public interface IChangeFeed : IAsyncDisposable
{
    /// <summary>
    /// Represents a method that will be called when a notification is received.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public delegate Task OnNotifiedDelegateAsync(object sender, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Represents a method that will be called when a notification is received.
    /// </summary>
    public event OnNotifiedDelegateAsync? OnNotifiedAsync;

    /// <summary>
    /// Notify the change feed of the payload.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="cancellationToken">You may cancel the token to stop sending.</param>
    /// <returns>A Task that will be completed once the notification has been sent.</returns>
    Task NotifyAsync(string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Call this to start listening for notifications.
    /// </summary>
    /// <param name="cancellationToken">You may cancel the token to stop listening at any time.</param>
    /// <returns>A Task that will be completed once the component starts listening.</returns>
    Task ListenAsync(CancellationToken cancellationToken);
}
