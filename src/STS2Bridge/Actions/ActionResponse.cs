namespace STS2Bridge.Actions;

public sealed record ActionResponse(string RequestId, bool Success, string? ErrorCode, string? Message, object? Data)
{
    public static ActionResponse Ok(string requestId, string? message = null, object? data = null) =>
        new(requestId, true, null, message, data);

    public static ActionResponse Fail(string requestId, string errorCode, string message) =>
        new(requestId, false, errorCode, message, null);
}
