using Godot;
using STS2Bridge.Ui;

namespace STS2Bridge.Tests.Ui;

public sealed class EventSettingsLayoutLogicTests
{
    [Fact]
    public void CanApplyViewportBounds_should_require_tree_and_non_zero_size()
    {
        Assert.False(EventSettingsLayoutLogic.CanApplyViewportBounds(false, new Vector2(1920, 1080)));
        Assert.False(EventSettingsLayoutLogic.CanApplyViewportBounds(true, Vector2.Zero));
        Assert.True(EventSettingsLayoutLogic.CanApplyViewportBounds(true, new Vector2(1920, 1080)));
    }

    [Fact]
    public void CalculatePopupRect_should_stay_inside_small_viewport()
    {
        var rect = EventSettingsLayoutLogic.CalculatePopupRect(new Vector2(1280, 720));

        Assert.True(rect.Position.X >= 0);
        Assert.True(rect.Position.Y >= 0);
        Assert.True(rect.End.X <= 1280);
        Assert.True(rect.End.Y <= 720);
    }

    [Fact]
    public void CalculatePopupRect_should_use_max_bounds_on_large_viewport()
    {
        var rect = EventSettingsLayoutLogic.CalculatePopupRect(new Vector2(1920, 1080));

        Assert.Equal(1040f, rect.Size.X);
        Assert.Equal(700f, rect.Size.Y);
    }

    [Fact]
    public void CalculateColumnWidths_should_prefer_compact_left_column_and_expanded_right_column()
    {
        var (leftWidth, rightWidth) = EventSettingsLayoutLogic.CalculateColumnWidths(1040f);

        Assert.Equal(384f, leftWidth);
        Assert.Equal(624f, rightWidth);
    }

    [Fact]
    public void CalculateEventListMinHeight_should_leave_more_room_for_event_rows()
    {
        var height = EventSettingsLayoutLogic.CalculateEventListMinHeight(700f);

        Assert.Equal(316f, height);
    }
}
