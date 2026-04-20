namespace STS2Bridge.Integration;

public sealed record ExternalImStatus
{
    public ExternalImConnectionState ConnectionState { get; init; } = ExternalImConnectionState.Disconnected;
    public string? CurrentUid { get; init; }
    public string? CurrentUserId { get; init; }
    public string? LastError { get; init; }
    public string? LastServerMessage { get; init; }
    public string? ConnectedUrl { get; init; }

    public bool IsLoggedIn => ConnectionState == ExternalImConnectionState.LoggedIn && !string.IsNullOrWhiteSpace(CurrentUserId);
}
