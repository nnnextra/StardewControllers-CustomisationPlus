using PropertyChanged.SourceGenerator;
using StarControl.Config;

namespace StarControl.UI;

internal partial class StyleConfigurationViewModel
{
    public Func<float, string> FormatPixels { get; } = v => v.ToString("f0") + " px";
    public Func<float, string> FormatScale { get; } = v => v.ToString("0.00") + "x";
    public Func<float, string> FormatPercent { get; } = v => (v * 100).ToString("f0") + "%";

    /// <inheritdoc cref="Styles.InnerBackgroundColor" />
    [Notify]
    private Color innerBackgroundColor;

    /// <inheritdoc cref="Styles.InnerRadius" />
    [Notify]
    private float innerRadius;

    /// <inheritdoc cref="Styles.OuterBackgroundColor" />
    [Notify]
    private Color outerBackgroundColor;

    /// <inheritdoc cref="Styles.OuterRadius" />
    [Notify]
    private float outerRadius;

    /// <inheritdoc cref="Styles.SelectionColor" />
    [Notify]
    private Color selectionColor;

    /// <inheritdoc cref="Styles.HighlightColor" />
    [Notify]
    private Color highlightColor;

    /// <inheritdoc cref="Styles.GapWidth" />
    [Notify]
    private float gapWidth;

    /// <inheritdoc cref="Styles.MenuSpriteHeight" />
    [Notify]
    private int menuSpriteHeight;

    /// <inheritdoc cref="Styles.StackSizeColor" />
    [Notify]
    private Color stackSizeColor;

    /// <inheritdoc cref="Styles.CursorDistance" />
    [Notify]
    private float cursorDistance;

    /// <inheritdoc cref="Styles.CursorSize" />
    [Notify]
    private float cursorSize;

    /// <inheritdoc cref="Styles.CursorColor" />
    [Notify]
    private Color cursorColor;

    /// <inheritdoc cref="Styles.SelectionSpriteHeight" />
    [Notify]
    private int selectionSpriteHeight;

    /// <inheritdoc cref="Styles.SelectionTitleColor" />
    [Notify]
    private Color selectionTitleColor;

    /// <inheritdoc cref="Styles.SelectionTitleScale" />
    [Notify]
    private float selectionTitleScale;

    /// <inheritdoc cref="Styles.SelectionTitleShadowColor" />
    [Notify]
    private Color selectionTitleShadowColor;

    /// <inheritdoc cref="Styles.SelectionTitleShadowAlpha" />
    [Notify]
    private float selectionTitleShadowAlpha;

    /// <inheritdoc cref="Styles.SelectionTitleShadowOffset" />
    [Notify]
    private Vector2 selectionTitleShadowOffset;

    /// <inheritdoc cref="Styles.ShowSelectionIcon" />
    [Notify]
    private bool showSelectionIcon;

    /// <inheritdoc cref="Styles.ShowSelectionTitle" />
    [Notify]
    private bool showSelectionTitle;

    /// <inheritdoc cref="Styles.SelectionDescriptionColor" />
    [Notify]
    private Color selectionDescriptionColor;

    /// <inheritdoc cref="Styles.SelectionDescriptionScale" />
    [Notify]
    private float selectionDescriptionScale;

    /// <inheritdoc cref="Styles.ShowSelectionDescription" />
    [Notify]
    private bool showSelectionDescription;

    /// <inheritdoc cref="Styles.MenuVerticalOffset" />
    [Notify]
    private float menuVerticalOffset;

    /// <inheritdoc cref="Styles.MenuHorizontalOffset" />
    [Notify]
    private float menuHorizontalOffset;

    /// <inheritdoc cref="Styles.ShowQuickActions" />
    [Notify]
    private bool showQuickActions;

    /// <inheritdoc cref="Styles.QuickActionScale" />
    [Notify]
    private float quickActionScale;

    public void Load(Styles config)
    {
        InnerBackgroundColor = config.InnerBackgroundColor;
        InnerRadius = config.InnerRadius;
        OuterBackgroundColor = config.OuterBackgroundColor;
        OuterRadius = config.OuterRadius;
        SelectionColor = config.SelectionColor;
        HighlightColor = config.HighlightColor;
        GapWidth = config.GapWidth;
        MenuSpriteHeight = config.MenuSpriteHeight;
        StackSizeColor = config.StackSizeColor;
        CursorDistance = config.CursorDistance;
        CursorSize = config.CursorSize;
        CursorColor = config.CursorColor;
        SelectionSpriteHeight = config.SelectionSpriteHeight;
        SelectionTitleColor = config.SelectionTitleColor;
        SelectionTitleScale = config.SelectionTitleScale;
        SelectionTitleShadowColor = config.SelectionTitleShadowColor;
        SelectionTitleShadowAlpha = config.SelectionTitleShadowAlpha;
        SelectionTitleShadowOffset = config.SelectionTitleShadowOffset;
        ShowSelectionIcon = config.ShowSelectionIcon;
        ShowSelectionTitle = config.ShowSelectionTitle;
        SelectionDescriptionColor = config.SelectionDescriptionColor;
        SelectionDescriptionScale = config.SelectionDescriptionScale;
        ShowSelectionDescription = config.ShowSelectionDescription;
        MenuVerticalOffset = config.MenuVerticalOffset;
        MenuHorizontalOffset = config.MenuHorizontalOffset;
        ShowQuickActions = config.ShowQuickActions;
        QuickActionScale = config.QuickActionScale;
    }

    public void Save(Styles config)
    {
        config.InnerBackgroundColor = new(InnerBackgroundColor);
        config.InnerRadius = InnerRadius;
        config.OuterBackgroundColor = new(OuterBackgroundColor);
        config.OuterRadius = OuterRadius;
        config.SelectionColor = new(SelectionColor);
        config.HighlightColor = new(HighlightColor);
        config.GapWidth = GapWidth;
        config.MenuSpriteHeight = MenuSpriteHeight;
        config.StackSizeColor = new(StackSizeColor);
        config.CursorDistance = CursorDistance;
        config.CursorSize = CursorSize;
        config.CursorColor = new(CursorColor);
        config.SelectionSpriteHeight = SelectionSpriteHeight;
        config.SelectionTitleColor = new(SelectionTitleColor);
        config.SelectionTitleScale = SelectionTitleScale;
        config.SelectionTitleShadowColor = new(SelectionTitleShadowColor);
        config.SelectionTitleShadowAlpha = SelectionTitleShadowAlpha;
        config.SelectionTitleShadowOffset = SelectionTitleShadowOffset;
        config.ShowSelectionIcon = ShowSelectionIcon;
        config.ShowSelectionTitle = ShowSelectionTitle;
        config.SelectionDescriptionColor = new(SelectionDescriptionColor);
        config.SelectionDescriptionScale = SelectionDescriptionScale;
        config.ShowSelectionDescription = ShowSelectionDescription;
        config.MenuVerticalOffset = MenuVerticalOffset;
        config.MenuHorizontalOffset = MenuHorizontalOffset;
        config.ShowQuickActions = ShowQuickActions;
        config.QuickActionScale = QuickActionScale;
    }
}
