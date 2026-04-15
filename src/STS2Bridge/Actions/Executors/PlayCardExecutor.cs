using STS2Bridge.State.Dtos;

namespace STS2Bridge.Actions.Executors;

public sealed class PlayCardExecutor : IActionExecutor
{
    public string ActionName => "play_card";

    public ActionResponse Execute(ActionRequest request, ActionExecutionContext context)
    {
        var snapshot = context.StateStore.GetSnapshot();
        if (!string.Equals(snapshot.Screen.Name, "combat", StringComparison.OrdinalIgnoreCase))
        {
            return ActionResponse.Fail(request.RequestId, "INVALID_STATE", "Current screen is not combat.");
        }

        if (request.Params is null || !request.Params.TryGetValue("cardInstanceId", out var cardId) || cardId is not string instanceId)
        {
            return ActionResponse.Fail(request.RequestId, "CARD_NOT_FOUND", "cardInstanceId is required.");
        }

        var card = snapshot.Hand.FirstOrDefault(item => item.InstanceId == instanceId);
        if (card is null)
        {
            return ActionResponse.Fail(request.RequestId, "CARD_NOT_FOUND", "Card instance was not found in hand.");
        }

        if (!card.Playable)
        {
            return ActionResponse.Fail(request.RequestId, "INVALID_STATE", "Card is not currently playable.");
        }

        if (snapshot.Player.Energy < card.Cost)
        {
            return ActionResponse.Fail(request.RequestId, "NOT_ENOUGH_ENERGY", "Current energy is not enough.");
        }

        if (card.TargetRequired && (request.Params.TryGetValue("targetInstanceId", out var targetId) is false || targetId is not string))
        {
            return ActionResponse.Fail(request.RequestId, "TARGET_REQUIRED", "targetInstanceId is required.");
        }

        return ActionResponse.Ok(request.RequestId, "queued", new { card = card.InstanceId });
    }
}
