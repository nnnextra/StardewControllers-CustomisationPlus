using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PropertyChanged.SourceGenerator;
using StarControl.Config;
using StarControl.Data;
using StarControl.Graphics;
using StarControl.Menus;

namespace StarControl.UI;

internal partial class RemappingViewModel(
    IInputHelper inputHelper,
    Farmer who,
    IEnumerable<IRadialMenuItem> modItems,
    SButton menuToggleButton,
    float thumbstickDeadZone,
    ButtonIconSet buttonIconSet,
    Action<Dictionary<SButton, RemappingSlot>> onSave
)
{
    private const int BaseMenuWidth = 950;
    private const int BaseMenuHeight = 700;
    private const int MinMenuWidth = 720;
    private const int MinMenuHeight = 520;
    private const int ViewportPaddingWidth = 128;
    private const int ViewportPaddingHeight = 120;
    private const int MenuVerticalNudge = -24;

    public IMenuController? Controller { get; set; }
    public bool IsItemHovered => HoveredItem is not null;
    public bool IsSlotHovered => HoveredSlot is not null;
    public bool IsSlotHoveredAndAssigned => HoveredSlot?.Item is not null;
    public IReadOnlyList<RemappableItemGroupViewModel> ItemGroups { get; } =
        [
            new()
            {
                Name = I18n.Enum_QuickSlotItemSource_Inventory_Name(),
                Items = who
                    .Items.Where(item => item is not null)
                    .Select(RemappableItemViewModel.FromInventoryItem)
                    .ToList(),
            },
            new()
            {
                Name = I18n.Enum_QuickSlotItemSource_ModItems_Name(),
                Items = modItems.Select(RemappableItemViewModel.FromMenuItem).ToList(),
            },
        ];
    public IEnumerable<RemappingSlotViewModel> Slots => slotsByButton.Values;

    [Notify]
    private ButtonIconSet buttonIconSet = buttonIconSet;

    [Notify]
    private string menuLayout = $"{BaseMenuWidth}px {BaseMenuHeight}px";

    [Notify]
    private bool canReassign;

    [Notify]
    private RemappableItemViewModel? hoveredItem;

    [Notify]
    private RemappingSlotViewModel? hoveredSlot;

    [Notify]
    private bool showAssignTip = true;

    [Notify]
    private bool showUnassignTip;

    private const double RightStickScrollRepeatMinMs = 35;
    private const double RightStickScrollRepeatMaxMs = 140;

    private double lastRightStickScrollMs;
    private bool menuLayoutInitialized;
    private int menuWidth = BaseMenuWidth;
    private int menuHeight = BaseMenuHeight;
    private readonly Dictionary<SButton, RemappingSlotViewModel> slotsByButton = new()
    {
        { SButton.DPadLeft, new(SButton.DPadLeft) },
        { SButton.DPadUp, new(SButton.DPadUp) },
        { SButton.DPadRight, new(SButton.DPadRight) },
        { SButton.DPadDown, new(SButton.DPadDown) },
        { SButton.ControllerX, new(SButton.ControllerX) },
        { SButton.ControllerY, new(SButton.ControllerY) },
        { SButton.ControllerB, new(SButton.ControllerB) },
        { SButton.LeftShoulder, new(SButton.LeftShoulder) },
        { SButton.RightShoulder, new(SButton.RightShoulder) },
    };

    public bool AssignToSlot(SButton button, RemappableItemViewModel item)
    {
        if (!CanReassign || !slotsByButton.TryGetValue(button, out var slot))
        {
            return false;
        }
        Game1.playSound("drumkit6");
        if (slot.Item is not null)
        {
            slot.Item.AssignedButton = SButton.None;
        }
        item.AssignedButton = button;
        slot.Item = item;
        Save();
        return true;
    }

    public void Load(Dictionary<SButton, RemappingSlot> data)
    {
        foreach (var slot in slotsByButton.Values)
        {
            if (slot.Item is { } previousItem)
            {
                previousItem.AssignedButton = SButton.None;
            }
            slot.Item = null;
        }
        foreach (var (button, slotData) in data)
        {
            if (
                string.IsNullOrEmpty(slotData.Id)
                || !slotsByButton.TryGetValue(button, out var slot)
            )
            {
                continue;
            }
            var item = slotData.IdType switch
            {
                ItemIdType.GameItem => ItemGroups[0]
                    .Items.FirstOrDefault(item => item.Id == slotData.Id)
                    ?? RemappableItemViewModel.FromInventoryItem(
                        ItemRegistry.Create(slotData.Id),
                        who.Items
                    ),
                ItemIdType.ModItem => ItemGroups[1]
                    .Items.FirstOrDefault(item => item.Id == slotData.Id),
                _ => null,
            };
            item ??= RemappableItemViewModel.Invalid(slotData.IdType, slotData.Id);
            item.AssignedButton = button;
            slot.Item = item;
        }
        UpdateTipVisibility();
    }

    public void Save()
    {
        var data = new Dictionary<SButton, RemappingSlot>();
        foreach (var (button, slot) in slotsByButton)
        {
            if (slot.Item is { } item && !string.IsNullOrEmpty(item.Id))
            {
                data[button] = new() { Id = slot.Item.Id, IdType = slot.Item.IdType };
            }
        }
        onSave(data);
    }

    public void SetItemHovered(RemappableItemViewModel? item)
    {
        if (HoveredItem == item)
        {
            return;
        }
        if (HoveredItem is not null)
        {
            HoveredItem.Hovered = false;
        }
        HoveredItem = item;
        if (item is not null)
        {
            item.Hovered = true;
        }
    }

    public void SetSlotHovered(RemappingSlotViewModel? slot)
    {
        HoveredSlot = slot;
        UpdateTipVisibility();
    }

    public void UnassignSlot(RemappingSlotViewModel slot)
    {
        if (slot.Item is null)
        {
            return;
        }
        Game1.playSound("trashcan");
        slot.Item.AssignedButton = SButton.None;
        slot.Item = null;
        OnPropertyChanged(new(nameof(IsSlotHoveredAndAssigned)));
        UpdateTipVisibility();
        Save();
    }

    public void Update()
    {
        if (!menuLayoutInitialized)
        {
            UpdateLayoutForViewport();
            menuLayoutInitialized = true;
        }
        CanReassign =
            inputHelper.IsDown(SButton.LeftTrigger) || inputHelper.IsDown(SButton.RightTrigger);
        HandleRightStickScroll();
        // IClickableMenu.receiveGamePadButton bizarrely does not receive some buttons such as the
        // left/right stick. We have to check them for through the helper.
        if (
            !CanReassign
            && menuToggleButton
                is not SButton.DPadUp
                    or SButton.DPadDown
                    or SButton.DPadLeft
                    or SButton.DPadRight
            && inputHelper.GetState(menuToggleButton) == SButtonState.Pressed
        )
        {
            Controller?.Close();
        }
    }

    public void InitializeLayout()
    {
        menuLayoutInitialized = false;
        UpdateLayoutForViewport();
        menuLayoutInitialized = true;
    }

    public Point GetMenuPosition()
    {
        var viewport = Game1.uiViewport;
        var x = (viewport.Width - menuWidth) / 2;
        var y = (viewport.Height - menuHeight) / 2 + MenuVerticalNudge;
        return new Point(Math.Max(0, x), Math.Max(0, y));
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
        menuWidth = width;
        menuHeight = height;
        MenuLayout = $"{width}px {height}px";
    }

    private void UpdateTipVisibility()
    {
        ShowUnassignTip = IsSlotHoveredAndAssigned;
        ShowAssignTip = !ShowUnassignTip;
    }

    private void HandleRightStickScroll()
    {
        if (Controller?.Menu is null)
        {
            return;
        }
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        var state = Game1.playerOneIndex >= PlayerIndex.One ? Game1.input.GetGamePadState() : new();
        var stickY = state.ThumbSticks.Right.Y;
        var absY = Math.Abs(stickY);
        if (absY <= thumbstickDeadZone)
        {
            return;
        }
        var intensity = Math.Clamp((absY - thumbstickDeadZone) / (1f - thumbstickDeadZone), 0f, 1f);
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
        var viewProp = Controller?.Menu?.GetType().GetProperty("View");
        var rootView = viewProp?.GetValue(Controller?.Menu!);
        if (rootView is null)
        {
            return null;
        }
        return FindScrollContainer(rootView);
    }

    private static object? FindScrollContainer(object view)
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
                return innerView;
            }
        }
        if (viewTypeName == "StardewUI.Widgets.ScrollContainer")
        {
            return view;
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
            var result = FindScrollContainer(childView);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }
}

internal partial class RemappingSlotViewModel(SButton button)
{
    public SButton Button { get; } = button;
    public int? Count => Item?.Count ?? 1;
    public bool IsCountVisible => Count > 1;
    public bool IsItemEnabled => Item?.Enabled == true;
    public int Quality => Item?.Quality ?? 0;

    public Sprite? Sprite => Item?.Sprite;

    public TooltipData? Tooltip => Item?.Tooltip;

    [Notify]
    private RemappableItemViewModel? item;
}

internal partial class RemappableItemGroupViewModel
{
    [Notify]
    private IReadOnlyList<RemappableItemViewModel> items = [];

    [Notify]
    private string name = "";
}

internal partial class RemappableItemViewModel
{
    public string Id { get; init; } = "";

    public ItemIdType IdType { get; init; }
    public bool IsCountVisible => Count > 1;

    [Notify]
    private SButton assignedButton;

    [Notify]
    private int count = 1;

    [Notify]
    private bool enabled;

    [Notify]
    private bool hovered;

    [Notify]
    private int quality;

    [Notify]
    private Sprite? sprite;

    [Notify]
    private TooltipData? tooltip;

    public static RemappableItemViewModel FromInventoryItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var qualifiedId = item.QualifiedItemId; // can be null/empty for some mod items
        var sprite = Sprite.FromItem(item);

        return new()
        {
            Id = qualifiedId ?? item.Name ?? item.GetType().FullName ?? "unknown",
            IdType = ItemIdType.GameItem,
            Enabled = true,
            Sprite = sprite,
            Quality = item.Quality,
            Count = item.Stack,
            Tooltip = new(item.getDescription(), item.DisplayName, item),
        };
    }

    public static RemappableItemViewModel FromInventoryItem(
        Item item,
        ICollection<Item> availableItems
    )
    {
        var result = FromInventoryItem(item);
        result.Enabled =
            QuickSlotResolver.ResolveInventoryItem(item.QualifiedItemId, availableItems)
                is not null;
        return result;
    }

    public static RemappableItemViewModel FromMenuItem(IRadialMenuItem item)
    {
        return new()
        {
            Id = item.Id,
            IdType = ItemIdType.ModItem,
            Enabled = item.Enabled,
            Sprite = item.Texture is not null
                ? new(item.Texture, item.SourceRectangle ?? item.Texture.Bounds)
                : Sprites.Error(),
            Tooltip = !string.IsNullOrEmpty(item.Description)
                ? new(item.Description, item.Title)
                : new(item.Title),
        };
    }

    public static RemappableItemViewModel Invalid(ItemIdType type, string id)
    {
        return new()
        {
            Id = id,
            IdType = type,
            Sprite = Sprites.Error(),
            Tooltip = new(I18n.Remapping_InvalidItem_Description(id)),
        };
    }
}
