namespace STS2Bridge.Integration;

public interface IExternalImClient
{
    ExternalImStatus Status { get; }

    Task ConnectAsync(string url, CancellationToken cancellationToken = default);

    Task LoginAsync(string uid, string token, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task SendCommandAsync(string userId, string commandId, CancellationToken cancellationToken = default);
}
