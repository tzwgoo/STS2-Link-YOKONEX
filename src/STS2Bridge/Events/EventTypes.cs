namespace STS2Bridge.Events;

public static class EventTypes
{
    public const string PlayerDamaged = "player.damaged";
    public const string PlayerHealed = "player.healed";
    public const string PlayerEnergyChanged = "player.energy_changed";
    public const string PlayerBlockBroken = "player.block_broken";
    public const string PlayerDied = "player.died";
    public const string LightningOrbPassiveTriggered = "orb.lightning.passive_triggered";
    public const string LightningOrbEvoked = "orb.lightning.evoked";
    public const string FrostOrbPassiveTriggered = "orb.frost.passive_triggered";
    public const string FrostOrbEvoked = "orb.frost.evoked";
    public const string DarkOrbPassiveTriggered = "orb.dark.passive_triggered";
    public const string DarkOrbEvoked = "orb.dark.evoked";
    public const string PlasmaOrbPassiveTriggered = "orb.plasma.passive_triggered";
    public const string PlasmaOrbEvoked = "orb.plasma.evoked";
    public const string CardUpgraded = "card.upgraded";
    public const string ItemPurchased = "item.purchased";
    public const string RewardOpened = "reward.opened";
    public const string RewardSelected = "reward.selected";
    public const string EventOptionSelected = "event.option_selected";
}
