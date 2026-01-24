using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;
using StarControl.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace StarControl.UI;

internal partial class QuickSlotConfigurationViewModel
{
    private static readonly Color AssignedColor = new(50, 100, 50);
    private static readonly Color UnassignedColor = new(60, 60, 60);
    private static readonly Color UnavailableColor = new(0x44, 0x44, 0x44, 0x44);

    public Color CurrentAssignmentColor => IsAssigned ? AssignedColor : UnassignedColor;
    public string CurrentAssignmentLabel =>
        IsAssigned
            ? I18n.Config_QuickSlot_Assigned_Title()
            : I18n.Config_QuickSlot_Unassigned_Title();

    [DependsOn(nameof(ItemData), nameof(ModAction))]
    public Sprite? Icon => GetIcon();
    public bool IsAssigned => ItemData is not null || ModAction is not null;

    public Color Tint =>
        ItemData is not null && Game1.player.Items.FindUsableItem(ItemData.QualifiedItemId) is null
            ? UnavailableColor
            : Color.White;

    [DependsOn(nameof(ItemData), nameof(ModAction))]
    public TooltipData Tooltip => GetTooltip();

    [Notify]
    private ParsedItemData? itemData;

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
    }

    private Sprite? GetIcon()
    {
        if (ItemData is not null)
        {
            // Weird-item-proofing: Item Bags and similar mods don't register ItemRegistry data.
            // If this is an error item, try to find the real inventory item and render it via drawInMenu.
            if (ItemData.IsErrorItem)
            {
                var invItem = Game1.player?.Items?.FirstOrDefault(i =>
                    i is not null && i.QualifiedItemId == ItemData.QualifiedItemId
                );

                if (invItem is not null)
                {
                    return Sprite.FromItem(invItem);
                }
            }

            // Normal path for registered items
            return new(ItemData.GetTexture(), ItemData.GetSourceRect());
        }

        return ModAction?.Icon;
    }

    private TooltipData GetTooltip()
    {
        if (ItemData is not null)
        {
            // Weird-item-proofing: if ItemData is an error item, use the real inventory item instead.
            if (ItemData.IsErrorItem)
            {
                var invItem = Game1.player?.Items?.FirstOrDefault(i =>
                    i is not null && i.QualifiedItemId == ItemData.QualifiedItemId
                );

                if (invItem is not null)
                {
                    return new(
                        Title: invItem.DisplayName,
                        Text: invItem.getDescription(),
                        Item: invItem
                    );
                }
            }

            // Normal path for registered items
            return new(
                Title: ItemData.DisplayName,
                Text: ItemData.Description,
                Item: ItemRegistry.Create(ItemData.QualifiedItemId)
            );
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
