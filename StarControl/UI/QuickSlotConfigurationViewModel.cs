using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;
using StarControl.Graphics;
using StarControl.Menus;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace StarControl.UI;

internal partial class QuickSlotConfigurationViewModel
{
    private static readonly Color AssignedColor = new(50, 100, 50);
    private static readonly Color UnassignedColor = new(60, 60, 60);
    private static readonly Color UnavailableColor = new(0x44, 0x44, 0x44, 0x44);
    private static readonly Dictionary<string, Sprite> LastKnownIcons = new();
    private static readonly Dictionary<string, TooltipData> LastKnownTooltips = new();

    private string IconCacheKey =>
        ItemData is null ? "" : $"{ItemData.QualifiedItemId}::{ItemSubId ?? ""}";

    public Color CurrentAssignmentColor => IsAssigned ? AssignedColor : UnassignedColor;
    public string CurrentAssignmentLabel =>
        IsAssigned
            ? I18n.Config_QuickSlot_Assigned_Title()
            : I18n.Config_QuickSlot_Unassigned_Title();

    [DependsOn(nameof(ItemData), nameof(ModAction))]
    public Sprite? Icon => GetIcon();
    public bool IsAssigned => ItemData is not null || ModAction is not null;

    public Color Tint
    {
        get
        {
            if (ItemData is null || Game1.player is null)
                return Color.White;

            var items = QuickSlotResolver.GetExpandedPlayerItems(Game1.player);
            var resolved = QuickSlotResolver.ResolveInventoryItem(
                ItemData.QualifiedItemId,
                ItemSubId,
                items
            );

            return resolved is null ? UnavailableColor : Color.White;
        }
    }

    [DependsOn(nameof(ItemData), nameof(ModAction))]
    public TooltipData Tooltip => GetTooltip();

    [Notify]
    private ParsedItemData? itemData;

    [Notify]
    private string? itemSubId;

    [Notify]
    private ModMenuItemConfigurationViewModel? modAction;

    [Notify]
    private bool requireConfirmation;

    [Notify]
    private bool useSecondaryAction;

    public void Clear()
    {
        ItemData = null;
        ModAction = null;
        UseSecondaryAction = false;
        ItemSubId = null;
    }

    private Sprite? GetIcon()
    {
        if (ItemData is not null)
        {
            // Use expanded inventory so Item Bags + OmniBag nested bags resolve.
            if (Game1.player is not null)
            {
                var items = QuickSlotResolver.GetExpandedPlayerItems(Game1.player);
                var invItem = QuickSlotResolver.ResolveInventoryItem(
                    ItemData.QualifiedItemId,
                    ItemSubId,
                    items
                );

                if (invItem is not null)
                {
                    var sprite = Sprite.FromItem(invItem);
                    LastKnownIcons[IconCacheKey] = sprite;
                    return sprite;
                }
            }

            // If it’s an error item (common for Item Bags), try last-known icon before falling back.
            if (ItemData.IsErrorItem && LastKnownIcons.TryGetValue(IconCacheKey, out var cached))
                return cached;

            // Normal path for registered items
            return new(ItemData.GetTexture(), ItemData.GetSourceRect());
        }

        return ModAction?.Icon;
    }

    private TooltipData GetTooltip()
    {
        if (ItemData is not null)
        {
            // Prefer a real item instance from expanded inventory (bags + omni)
            if (Game1.player is not null)
            {
                var items = QuickSlotResolver.GetExpandedPlayerItems(Game1.player);
                var invItem = QuickSlotResolver.ResolveInventoryItem(
                    ItemData.QualifiedItemId,
                    ItemSubId,
                    items
                );

                if (invItem is not null)
                {
                    var tip = new TooltipData(
                        Title: invItem.DisplayName,
                        Text: invItem.getDescription(),
                        Item: invItem
                    );

                    LastKnownTooltips[IconCacheKey] = tip;
                    return tip;
                }
            }

            // IMPORTANT: do NOT call ItemRegistry.Create here for error/unresolvable items.
            if (LastKnownTooltips.TryGetValue(IconCacheKey, out var cachedTip))
                return cachedTip;

            // That’s what is throwing in your SMAPI log and breaking the UI binding updates.
            return new(Title: ItemData.DisplayName, Text: ItemData.Description);
        }

        if (ModAction is not null)
        {
            return !string.IsNullOrEmpty(ModAction.Description)
                ? new(Title: ModAction.Name, Text: ModAction.Description)
                : new(ModAction.Name);
        }

        return new(I18n.Config_QuickActions_EmptySlot_Title());
    }

    private void ModAction_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModAction.Icon))
        {
            OnPropertyChanged(new(nameof(Icon)));
        }
        else if (e.PropertyName is nameof(ModAction.Name) or nameof(ModAction.Description))
        {
            OnPropertyChanged(new(nameof(Tooltip)));
        }
    }

    private void OnItemDataChanged()
    {
        if (ItemData is not null)
        {
            ModAction = null;
        }
    }

    private void OnModActionChanged(
        ModMenuItemConfigurationViewModel? oldValue,
        ModMenuItemConfigurationViewModel? newValue
    )
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= ModAction_PropertyChanged;
        }
        if (newValue is not null)
        {
            ItemData = null;
            newValue.PropertyChanged += ModAction_PropertyChanged;
        }
    }
}
