using Microsoft.Xna.Framework.Graphics;
using StarControl.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace StarControl.UI;

/// <summary>
/// View model for an item displayed in the quick-slot picker, supporting vanilla items and mod actions.
/// </summary>
/// <remarks>
/// <para>
/// Item images require a specialized context because they are not always simple textures. In many
/// cases, the item must be drawn with a tint or overlay, which is actually a second sprite.
/// </para>
/// </remarks>
/// <param name="update">Delegate to update a quick slot to reference this item.</param>
/// <param name="sourceTexture">The texture where the item image is located; both the base image and
/// the overlay (if applicable) are expected to be found in this texture.</param>
/// <param name="sourceRect">The region of the <paramref name="sourceTexture"/> where the base image
/// for the item is located.</param>
/// <param name="tintRect">The region of the <paramref name="sourceTexture"/> where the tint or
/// overlay image for the item is located.</param>
/// <param name="tintColor">The tint color, which either applies to the image in the
/// <paramref name="tintRect"/>, if one is specified, or the base image (in
/// <paramref name="sourceRect"/>) if no <paramref name="tintRect"/> is specified.</param>
internal class QuickSlotPickerItemViewModel(
    Action<QuickSlotConfigurationViewModel> update,
    Texture2D sourceTexture,
    Rectangle? sourceRect,
    Rectangle? tintRect = null,
    Color? tintColor = null,
    TooltipData? tooltip = null
)
{
    /// <summary>
    /// Whether the image uses a separate <see cref="TintSprite"/>.
    /// </summary>
    public bool HasTintSprite => TintSprite is not null;

    /// <summary>
    /// Sprite data for the base image.
    /// </summary>
    public Sprite? Sprite { get; } = new(sourceTexture, sourceRect ?? sourceTexture.Bounds);

    /// <summary>
    /// Tint color for the <see cref="Sprite"/>.
    /// </summary>
    public Color SpriteColor { get; } = (!tintRect.HasValue ? tintColor : null) ?? Color.White;

    /// <summary>
    /// Sprite data for the overlay image, if any.
    /// </summary>
    public Sprite? TintSprite { get; } =
        tintRect.HasValue ? new(sourceTexture, tintRect.Value) : null;

    /// <summary>
    /// Tint color for the <see cref="TintSprite"/>.
    /// </summary>
    public Color TintSpriteColor { get; } = tintColor ?? Color.White;

    public TooltipData? Tooltip { get; } = tooltip;

    private static readonly Color SmokedFishTintColor = new Color(80, 30, 10) * 0.6f;

    /// <summary>
    /// Creates a new instance using the game data for a known item.
    /// </summary>
    /// <param name="item">The item to display.</param>
    public static QuickSlotPickerItemViewModel ForItem(Item item)
    {
        // Keep SmokedFish behavior exactly as before (special tint/overlay logic)
        var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
        if (item is SObject obj && obj.preserve.Value == SObject.PreserveType.SmokedFish)
        {
            var fishData = ItemRegistry.GetDataOrErrorItem(obj.GetPreservedItemId());
            return new(
                slot =>
                {
                    slot.ItemData = data;
                    slot.ItemSubId = Compat.ItemBagsIdentity.TryGetBagTypeId(item);
                },
                fishData.GetTexture(),
                fishData.GetSourceRect(),
                fishData.GetSourceRect(),
                SmokedFishTintColor
            );
        }

        // Tooltip is shared by both paths
        TooltipData tooltip = !string.IsNullOrEmpty(item.getDescription())
            ? new(Title: item.DisplayName, Text: item.getDescription(), Item: item)
            : new(Text: item.getDescription(), Item: item);

        // If the registry data is an error item AND the item isn't vanilla, render via drawInMenu.
        if (data.IsErrorItem && item.GetType().Assembly != typeof(SObject).Assembly)
        {
            var sprite = Sprite.FromItem(item);
            return new(
                slot =>
                {
                    slot.ItemData = data;
                    slot.ItemSubId = Compat.ItemBagsIdentity.TryGetBagTypeId(item);
                },
                sprite.Texture,
                sprite.SourceRect,
                tooltip: tooltip
            );
        }

        // Normal (registered) items keep tint/overlay behavior
        Color? tintColor = null;
        Rectangle? tintRect = null;
        if (item is ColoredObject co)
        {
            tintColor = co.color.Value;
            if (!co.ColorSameIndexAsParentSheetIndex)
            {
                tintRect = data.GetSourceRect(1);
            }
        }

        return new(
            slot =>
            {
                slot.ItemData = data.GetBaseItem();
                slot.ItemSubId = Compat.ItemBagsIdentity.TryGetBagTypeId(item);
            },
            data.GetTexture(),
            data.GetSourceRect(),
            tintRect,
            tintColor,
            tooltip
        );
    }

    /// <summary>
    /// Creates a new instance using the base data for an in-game item.
    /// </summary>
    /// <param name="data">The item data.</param>
    public static QuickSlotPickerItemViewModel ForItemData(ParsedItemData data)
    {
        TooltipData tooltip = !string.IsNullOrEmpty(data.Description)
            ? new(Title: data.DisplayName, Text: data.Description)
            : new(data.DisplayName);
        return new(
            slot => slot.ItemData = data.GetBaseItem(),
            data.GetTexture(),
            data.GetSourceRect(),
            tooltip: tooltip
        );
    }

    /// <summary>
    /// Creates a new instance using the configuration for a mod action.
    /// </summary>
    /// <param name="item">The mod action.</param>
    public static QuickSlotPickerItemViewModel ForModAction(ModMenuItemConfigurationViewModel item)
    {
        var icon = item.Icon;
        return new(
            slot => slot.ModAction = item,
            icon.Texture,
            icon.SourceRect,
            tooltip: item.Tooltip
        );
    }

    /// <summary>
    /// Updates a quick slot to point to this item.
    /// </summary>
    /// <param name="slot">The quick slot to be updated.</param>
    public void UpdateSlot(QuickSlotConfigurationViewModel slot)
    {
        update(slot);
    }
}
