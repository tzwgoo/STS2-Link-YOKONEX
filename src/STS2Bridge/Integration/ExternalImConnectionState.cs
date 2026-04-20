namespace STS2Bridge.Integration;

public enum ExternalImConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    LoggingIn,
    LoggedIn,
    LoginFailed,
    Error
}
