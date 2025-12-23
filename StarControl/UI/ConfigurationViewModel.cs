using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PropertyChanged.SourceGenerator;
using StarControl.Config;

namespace StarControl.UI;

internal partial class ConfigurationViewModel : IDisposable
{
    private const int BaseMenuWidth = 800;
    private const int BaseMenuHeight = 680;
    private const int MinMenuWidth = 520;
    private const int MinMenuHeight = 420;
    private const int ViewportPaddingWidth = 128;
    private const int ViewportPaddingHeight = 240;
    private const int PreviewMaxSize = 500;
    private const int PreviewMinSize = 240;
    private const int PreviewHorizontalPadding = 80;
    private const float MenuSlideDurationMs = 450f;
    private const int MenuVerticalNudge = -24;
    private const double RightStickScrollRepeatMinMs = 35;
    private const double RightStickScrollRepeatMaxMs = 140;

    public static event EventHandler<EventArgs>? Saved;

    public IMenuController? Controller { get; set; }
    public DebugSettingsViewModel Debug { get; } = new();
    public bool Dismissed { get; set; }
    public InputConfigurationViewModel Input { get; } = new();
    public bool IsNavigationDisabled => !IsNavigationEnabled;
    public ItemsConfigurationViewModel Items { get; } = new();
    public ModIntegrationsViewModel Mods { get; }
    public PagerViewModel<NavPageViewModel> Pager { get; } = new();
    public RadialMenuPreview Preview { get; }

    public SoundSettingsViewModel Sound { get; } = new();
    public StyleConfigurationViewModel Style { get; } = new();

    [Notify]
    private Vector2 contentPanelSize;

    [Notify]
    private bool isNavigationEnabled = true;

    [Notify]
    private string menuLayout = $"{BaseMenuWidth}px {BaseMenuHeight}px";

    [Notify]
    private string previewLayout = $"{PreviewMaxSize}px";

    [Notify]
    private bool isPreviewEnabled = true;

    [Notify]
    private bool isPreviewVisible;

    private readonly ModConfig config;
    private readonly IModHelper helper;

    private int loadingFrameCount;
    private int loadingPageIndex = 1;
    private float menuPositionX;
    private float slideStartX;
    private float slideTargetX;
    private double slideStartMs;
    private bool isSliding;
    private double lastRightStickScrollMs;
    private bool menuPositionInitialized;

    public int MenuWidth { get; private set; } = BaseMenuWidth;
    public int MenuHeight { get; private set; } = BaseMenuHeight;
    public int PreviewWidth { get; private set; } = PreviewMaxSize;

    public ConfigurationViewModel(IModHelper helper, ModConfig config)
    {
        this.helper = helper;
        this.config = config;
        UpdateLayoutForViewport();
        var modId = helper.ModContent.ModID;
        var selfPriority = ModPriorityViewModel.Self(modId, Items);
        Mods = new(helper.ModRegistry, selfPriority);
        Pager.Pages =
        [
            new(
                NavPage.Controls,
                I18n.Config_Tab_Controls_Title(),
                $"Mods/{modId}/Views/Controls",
                autoLoad: true
            ),
            new(NavPage.Style, I18n.Config_Tab_Style_Title(), $"Mods/{modId}/Views/Style"),
            new(NavPage.Actions, I18n.Config_Tab_Actions_Title(), $"Mods/{modId}/Views/Actions"),
            new(NavPage.Sound, I18n.Config_Tab_Sound_Title(), $"Mods/{modId}/Views/Sound"),
            new(NavPage.Mods, I18n.Config_Tab_Mods_Title(), $"Mods/{modId}/Views/ModIntegrations"),
            new(NavPage.Debug, I18n.Config_Tab_Debug_Title(), $"Mods/{modId}/Views/Debug"),
        ];
        Pager.PropertyChanged += Pager_PropertyChanged;
        Items.PropertyChanged += Items_PropertyChanged;
        Preview = new(Style, 500, 500);
    }

    public bool CancelBlockingAction()
    {
        if (Items.IsReordering)
        {
            return Items.EndReordering();
        }
        return false;
    }

    public void Dispose()
    {
        Preview.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool HandleButtonPress(SButton button)
    {
        if (!IsNavigationEnabled && IsCancelButton(button))
        {
            return CancelBlockingAction();
        }
        return Pager.HandleButtonPress(button);
    }

    public bool HasUnsavedChanges()
    {
        var dummyConfig = new ModConfig();
        SaveSections(dummyConfig);
        return !dummyConfig.Equals(config);
    }

    public void PerformAction(ConfigurationAction action)
    {
        switch (action)
        {
            case ConfigurationAction.Save:
                Save();
                Dismissed = true;
                Controller?.Close();
                break;
            case ConfigurationAction.Cancel:
                Dismissed = true;
                Controller?.Close();
                break;
            case ConfigurationAction.Reset:
                Game1.playSound("drumkit6");
                Load(new ModConfig());
                break;
            default:
                throw new ArgumentException($"Unsupported menu action: {action}");
        }
    }

    public void OpenStylePreview()
    {
        Preview.Refresh();
        var controller = ViewEngine.OpenChildMenu("StylePreview", Preview);
        controller.CloseOnOutsideClick = true;
        controller.DimmingAmount = 0.5f;
    }

    public Point GetMenuPosition()
    {
        var viewport = Game1.uiViewport;
        var targetX = GetTargetMenuX(viewport);
        if (!menuPositionInitialized)
        {
            menuPositionX = targetX;
            menuPositionInitialized = true;
        }
        var y = (viewport.Height - MenuHeight) / 2 + MenuVerticalNudge;
        return new Point(Math.Max(0, (int)MathF.Round(menuPositionX)), Math.Max(0, y));
    }

    public void InitializeMenuPosition()
    {
        menuPositionInitialized = false;
        UpdateMenuPosition();
    }

    public void Reload()
    {
        Load(config);
    }

    public void Save()
    {
        SaveSections(config);
        helper.WriteConfig(config);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    public void ShowCloseConfirmation()
    {
        var portrait =
            Game1.getCharacterFromName("Krobus")?.Portrait
            ?? Game1.content.Load<Texture2D>("Portraits\\Krobus");
        var context = new ConfirmationViewModel()
        {
            DialogTitle = I18n.Confirmation_Config_Title(),
            DialogDescription = I18n.Confirmation_Config_Description(),
            SaveTitle = I18n.Confirmation_Config_Save_Title(),
            SaveDescription = I18n.Confirmation_Config_Save_Description(),
            RevertTitle = I18n.Confirmation_Config_Revert_Title(),
            RevertDescription = I18n.Confirmation_Config_Revert_Description(),
            CancelTitle = I18n.Confirmation_Config_Cancel_Title(),
            CancelDescription = I18n.Confirmation_Config_Cancel_Description(),
            Sprite = new(portrait, Game1.getSourceRectForStandardTileSheet(portrait, 3)),
        };
        var confirmationController = ViewEngine.OpenChildMenu("Confirmation", context);
        confirmationController.CloseOnOutsideClick = true;
        context.Close = confirmationController.Close;
        confirmationController.Closed += () => ConfirmClose(context.Result);
    }

    public void Update()
    {
        UpdateMenuPosition();
        ClampMouseToMenuWhileScrolling();
        if (loadingPageIndex >= Pager.Pages.Count)
        {
            return;
        }
        loadingFrameCount = (loadingFrameCount + 1) % 3;
        if (loadingFrameCount > 0)
        {
            return;
        }
        Pager.Pages[loadingPageIndex].Loaded = true;
        loadingPageIndex++;
    }

    private void ConfirmClose(ConfirmationResult result)
    {
        switch (result)
        {
            case ConfirmationResult.Yes:
                PerformAction(ConfigurationAction.Save);
                break;
            case ConfirmationResult.No:
                PerformAction(ConfigurationAction.Cancel);
                break;
        }
    }

    private static bool IsCancelButton(SButton button)
    {
        return button is SButton.ControllerB or SButton.ControllerBack
            || (
                button.TryGetStardewInput(out var inputButton)
                && Game1.options.menuButton.Contains(inputButton)
            );
    }

    private void Items_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemsConfigurationViewModel.GrabbedItem))
        {
            IsNavigationEnabled = Items.GrabbedItem is null;
            if (Items.GrabbedItem is { } item)
            {
                Controller?.SetCursorAttachment(
                    item.Icon.Texture,
                    item.Icon.SourceRect,
                    new(64 * item.Icon.SourceRect.Width / item.Icon.SourceRect.Height, 64)
                );
            }
            else
            {
                Controller?.ClearCursorAttachment();
            }
        }
    }

    private void Load(ModConfig config)
    {
        Input.Load(config.Input);
        Style.Load(config.Style);
        Items.Load(config.Items);
        Sound.Load(config.Sound);
        Mods.Load(config.Integrations);
        Debug.Load(config.Debug);
    }

    private void OnContentPanelSizeChanged()
    {
        // Explicitly assigning it here, as opposed to passing in a selector to the constructor, guarantees that the
        // property change event will fire for the dependent models as well.
        Pager.ContentPanelSize = contentPanelSize;
        Items.Pager.ContentPanelSize = ContentPanelSize;
    }

    private void Pager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Pager.SelectedPageIndex))
        {
            UpdatePreviewVisibility();
            UpdateMenuPosition();
        }
    }

    private void UpdateLayoutForViewport()
    {
        var viewport = Game1.uiViewport;
        var width = Math.Min(
            BaseMenuWidth,
            Math.Max(MinMenuWidth, viewport.Width - ViewportPaddingWidth)
        );
        var height = Math.Min(
            BaseMenuHeight,
            Math.Max(MinMenuHeight, viewport.Height - ViewportPaddingHeight)
        );
        MenuWidth = width;
        MenuHeight = height;
        MenuLayout = $"{width}px {height}px";

        var availablePreviewWidth = Math.Max(0, viewport.Width - width - PreviewHorizontalPadding);
        var previewMaxSize = Math.Min(PreviewMaxSize, availablePreviewWidth);
        var previewSize = Math.Min(
            previewMaxSize,
            Math.Max(PreviewMinSize, height - ViewportPaddingHeight / 2)
        );
        PreviewWidth = previewSize;
        PreviewLayout = $"{previewSize}px";
        IsPreviewEnabled = previewSize >= PreviewMinSize && availablePreviewWidth >= PreviewMinSize;
        UpdatePreviewVisibility();
        UpdateMenuPosition();
    }

    private void UpdatePreviewVisibility()
    {
        IsPreviewVisible = IsPreviewEnabled && Pager.SelectedPageIndex == 1; // Styles
    }

    private void ClampMouseToMenuWhileScrolling()
    {
        if (Controller?.Menu is null)
        {
            return;
        }
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        var state = Game1.playerOneIndex >= PlayerIndex.One ? Game1.input.GetGamePadState() : new();
        var stickY = state.ThumbSticks.Right.Y;
        var absY = Math.Abs(stickY);
        var deadZone = config.Input.ThumbstickDeadZone;
        if (absY <= deadZone)
        {
            return;
        }
        var intensity = Math.Clamp((absY - deadZone) / (1f - deadZone), 0f, 1f);
        var repeatMs =
            RightStickScrollRepeatMaxMs
            - (RightStickScrollRepeatMaxMs - RightStickScrollRepeatMinMs) * intensity;
        if (nowMs - lastRightStickScrollMs < repeatMs)
        {
            return;
        }
        lastRightStickScrollMs = nowMs;
        TryScrollActiveContainer(stickY > 0);
    }

    private bool TryScrollActiveContainer(bool scrollUp)
    {
        var container = GetActiveScrollContainer();
        if (container is null)
        {
            return false;
        }
        var containerType = container.GetType();
        var scrollSizeProp = containerType.GetProperty("ScrollSize");
        var scrollSizeValue = scrollSizeProp?.GetValue(container);
        var scrollSize = scrollSizeValue is float size ? size : 0f;
        if (scrollSize <= 0f)
        {
            return false;
        }
        var methodName = scrollUp ? "ScrollBackward" : "ScrollForward";
        var scrollMethod = containerType.GetMethod(methodName);
        if (scrollMethod is null)
        {
            return false;
        }
        var result = scrollMethod.Invoke(container, Array.Empty<object>());
        var scrolled = result is bool didScroll && didScroll;
        if (scrolled)
        {
            Game1.playSound("shwip");
        }
        return scrolled;
    }

    private object? GetActiveScrollContainer()
    {
        if (Controller?.Menu is null)
        {
            return null;
        }
        var viewProp = Controller.Menu.GetType().GetProperty("View");
        var rootView = viewProp?.GetValue(Controller.Menu);
        if (rootView is null)
        {
            return null;
        }
        var selectedIndex = Pager.SelectedPageIndex;
        var currentIndex = 0;
        return FindScrollContainerByIndex(rootView, selectedIndex, ref currentIndex);
    }

    private static object? FindScrollContainerByIndex(
        object view,
        int targetIndex,
        ref int currentIndex
    )
    {
        var viewType = view.GetType();
        var viewTypeName = viewType.FullName ?? string.Empty;
        if (viewTypeName == "StardewUI.Widgets.ScrollableView")
        {
            var innerView = viewType
                .GetProperty("View", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(view);
            if (innerView is not null)
            {
                if (currentIndex == targetIndex)
                {
                    return innerView;
                }
                currentIndex++;
                return null;
            }
        }
        if (viewTypeName == "StardewUI.Widgets.ScrollContainer")
        {
            if (currentIndex == targetIndex)
            {
                return view;
            }
            currentIndex++;
        }
        var getChildren = viewType.GetMethod("GetChildren", new[] { typeof(bool) });
        var children = getChildren?.Invoke(view, new object[] { true }) as IEnumerable;
        if (children is null)
        {
            return null;
        }
        foreach (var child in children)
        {
            var childView = child?.GetType().GetProperty("View")?.GetValue(child);
            if (childView is null)
            {
                continue;
            }
            var result = FindScrollContainerByIndex(childView, targetIndex, ref currentIndex);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }

    private void StartMenuSlide(float targetX, double nowMs)
    {
        slideStartX = menuPositionX;
        slideTargetX = targetX;
        slideStartMs = nowMs;
        isSliding = true;
    }

    private static float EaseOutCubic(float t)
    {
        var oneMinusT = 1f - t;
        return 1f - (oneMinusT * oneMinusT * oneMinusT);
    }

    private float GetTargetMenuX(xTile.Dimensions.Rectangle viewport)
    {
        var previewWidth =
            IsPreviewEnabled && IsPreviewVisible ? PreviewWidth + PreviewHorizontalPadding : 0;
        return (viewport.Width - (MenuWidth + previewWidth)) / 2f;
    }

    private void UpdateMenuPosition()
    {
        if (Controller?.Menu is null)
        {
            return;
        }
        var viewport = Game1.uiViewport;
        var targetX = GetTargetMenuX(viewport);
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        if (!menuPositionInitialized)
        {
            menuPositionX = targetX;
            slideStartX = targetX;
            slideTargetX = targetX;
            slideStartMs = nowMs;
            isSliding = false;
            menuPositionInitialized = true;
        }
        else
        {
            if (!isSliding && Math.Abs(targetX - menuPositionX) > 0.5f)
            {
                StartMenuSlide(targetX, nowMs);
            }
            if (isSliding)
            {
                var elapsedMs = (float)(nowMs - slideStartMs);
                var t = Math.Min(1f, elapsedMs / MenuSlideDurationMs);
                var eased = EaseOutCubic(t);
                menuPositionX = slideStartX + (slideTargetX - slideStartX) * eased;
                if (t >= 1f)
                {
                    menuPositionX = slideTargetX;
                    isSliding = false;
                }
            }
        }
        Controller.Menu.xPositionOnScreen = Math.Max(0, (int)MathF.Round(menuPositionX));
        Controller.Menu.yPositionOnScreen = Math.Max(
            0,
            (viewport.Height - MenuHeight) / 2 + MenuVerticalNudge
        );
    }

    private void SaveSections(ModConfig config)
    {
        Input.Save(config.Input);
        Style.Save(config.Style);
        Items.Save(config.Items);
        Sound.Save(config.Sound);
        Mods.Save(config.Integrations);
        Debug.Save(config.Debug);
    }
}

internal enum NavPage
{
    Controls,
    Style,
    Actions,
    Sound,
    Mods,
    Debug,
}

internal partial class NavPageViewModel(
    NavPage id,
    string title,
    string pageAssetName,
    bool autoLoad = false
) : PageViewModel((int)id)
{
    public NavPage Id { get; } = id;
    public string PageAssetName { get; } = pageAssetName;
    public string Title { get; } = title;

    [Notify]
    private bool loaded = autoLoad;
}
