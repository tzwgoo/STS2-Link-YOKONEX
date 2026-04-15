namespace STS2Bridge.Api.Http;

public sealed record ApiEnvelope<T>(bool Success, T? Data, string? ErrorCode, string? Message)
{
    public static ApiEnvelope<T> Ok(T data) => new(true, data, null, null);

    public static ApiEnvelope<T> Fail(string errorCode, string message) => new(false, default, errorCode, message);
}
