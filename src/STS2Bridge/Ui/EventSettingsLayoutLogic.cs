using Godot;

namespace STS2Bridge.Ui;

public static class EventSettingsLayoutLogic
{
    private const float ColumnGap = 16f;
    private const float LeftColumnRatio = 0.38f;
    private const float LeftColumnMinWidth = 320f;
    private const float LeftColumnMaxWidth = 420f;

    public static bool CanApplyViewportBounds(bool isInsideTree, Vector2 viewportSize)
    {
        return isInsideTree && viewportSize != Vector2.Zero;
    }

    public static Rect2 CalculatePopupRect(Vector2 viewportSize)
    {
        const float maxWidth = 1040f;
        const float maxHeight = 700f;
        const float minWidth = 860f;
        const float minHeight = 520f;
        const float safeMargin = 36f;

        var availableWidth = Mathf.Max(0f, viewportSize.X - (safeMargin * 2));
        var availableHeight = Mathf.Max(0f, viewportSize.Y - (safeMargin * 2));

        var width = Mathf.Clamp(availableWidth, minWidth, maxWidth);
        var height = Mathf.Clamp(availableHeight, minHeight, maxHeight);

        width = Mathf.Min(width, availableWidth);
        height = Mathf.Min(height, availableHeight);

        var x = (viewportSize.X - width) * 0.5f;
        var y = (viewportSize.Y - height) * 0.5f;
        return new Rect2(x, y, width, height);
    }

    public static (float LeftWidth, float RightWidth) CalculateColumnWidths(float popupWidth)
    {
        var availableWidth = Mathf.Max(0f, popupWidth - (ColumnGap * 2));
        var leftWidth = Mathf.Clamp(Mathf.Ceil(availableWidth * LeftColumnRatio), LeftColumnMinWidth, LeftColumnMaxWidth);
        var rightWidth = Mathf.Max(0f, availableWidth - leftWidth);
        return (leftWidth, rightWidth);
    }

    public static float CalculateEventListMinHeight(float popupHeight)
    {
        return Mathf.Max(220f, popupHeight - 384f);
    }

    public static float CalculateSideColumnMinHeight(float popupHeight)
    {
        return Mathf.Max(220f, popupHeight - 296f);
    }
}
