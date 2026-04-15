namespace STS2Bridge.Tests.State;

public sealed class GameStateStoreTests
{
    [Fact]
    public void UpdateSnapshot_should_increment_version_and_replace_state()
    {
        var store = new GameStateStore();
        var snapshot = StateSnapshotDto.Empty with
        {
            RunId = "run-1",
            Floor = 2,
            Screen = new ScreenStateDto("combat", "player_turn"),
            Player = new PlayerStateDto(40, 70, 3, 0, 99)
        };

        store.Update(snapshot);
        var current = store.GetSnapshot();

        Assert.Equal("run-1", current.RunId);
        Assert.Equal(2, current.Floor);
        Assert.Equal("combat", current.Screen.Name);
        Assert.True(current.StateVersion > 0);
        Assert.True(current.UpdatedAtUnixMs > 0);
    }
}
