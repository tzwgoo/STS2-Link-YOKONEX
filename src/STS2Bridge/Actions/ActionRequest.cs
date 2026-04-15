namespace STS2Bridge.Actions;

public sealed record ActionRequest(string RequestId, string Action, IReadOnlyDictionary<string, object?>? Params);
