using STS2Bridge.Ui;

namespace STS2Bridge.Tests.Ui;

public sealed class EventSettingsPopupHostLogicTests
{
    [Fact]
    public void ShouldCreatePopupDuringSettingsInstall_should_be_false()
    {
        Assert.False(EventSettingsPopupHostLogic.ShouldCreatePopupDuringSettingsInstall());
    }
}
