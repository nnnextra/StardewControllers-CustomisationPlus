namespace StarControl.Config;

/// <summary>
/// Configures the items to be shown when a menu is open.
/// </summary>
public class ItemsConfiguration : IConfigEquatable<ItemsConfiguration>
{
    /// <summary>
    /// Number of items to be shown on a single page of the Inventory Menu.
    /// </summary>
    /// <remarks>
    /// The default value of <c>12</c> is equivalent to the vanilla size of a backpack page, and
    /// this is also the recommended value in order to keep radial menu pages in sync with the
    /// vanilla toolbar.
    /// </remarks>
    public int InventoryPageSize { get; set; } = 12;

    /// <summary>
    /// Whether to show blank spaces in the radial menu for empty inventory slots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, all radial menu items are equally spaced, ensuring that the cursor is always
    /// pointing to something. This looks cleaner, but some users may find it interferes with muscle
    /// memory because the size and position of items in the menu may change according to how full
    /// the inventory is.
    /// </para>
    /// <para>
    /// Turning this option on will cause empty slots to render as unselectable blank spaces in the
    /// Inventory Radial Menu - meaning that there are always exactly as many "regions" as the
    /// <see cref="InventoryPageSize"/> specifies, and the position/size of items cannot shift as
    /// long as they stay in the same backpack slot.
    /// </para>
    /// </remarks>
    public bool ShowInventoryBlanks { get; set; }

    /// <summary>
    /// The set of Mod Menu items, grouped by page.
    /// </summary>
    /// <remarks>
    /// This is a list of lists, where each inner list is a single page of mod items, similar to a
    /// single inventory page and supporting the same type of gamepad-controlled pagination.
    /// </remarks>
    public List<List<ModMenuItemConfiguration>> ModMenuPages { get; set; } = [];

    /// <summary>
    /// Whether to display the settings item on page <see cref="SettingsItemPageIndex"/> and at
    /// position <see cref="SettingsItemPositionIndex"/>.
    /// </summary>
    /// <remarks>
    /// The settings item always exists and always has a specific location, but can be suppressed
    /// from the Mod Menu during gameplay. It will still appear in the configuration menu when
    /// editing the menu items.
    /// </remarks>
    public bool ShowSettingsItem { get; set; } = true;

    /// <summary>
    /// The zero-based page number on which to show the Star Control Settings item, which brings up
    /// the configuration menu.
    /// </summary>
    public int SettingsItemPageIndex { get; set; }

    /// <summary>
    /// The zero-based position within the specific Mod Menu Page identified by
    /// <see cref="SettingsItemPageIndex"/> where the Star Control Settings item should be shown.
    /// </summary>
    /// <remarks>
    /// The item will be inserted <em>before</em> the specified index, so an index of <c>0</c> means
    /// it will be the first item on that page.
    /// </remarks>
    public int SettingsItemPositionIndex { get; set; }

    /// <summary>
    /// Map of all quick-slot buttons to the corresponding quick slot item (if any).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Quick slots allow any specific item or mod action to be activated with a single button press
    /// while the menu is open, i.e. without having to use the stick to point.
    /// </para>
    /// <para>
    /// Note that while the dictionary may contain any <see cref="SButton"/> as a key, only the four
    /// cardinal directional pad buttons and the four primary buttons (generally A, B, X, Y) are
    /// supported for quick slots. Other buttons will be ignored.
    /// </para>
    /// </remarks>
    public Dictionary<SButton, QuickSlotConfiguration> QuickSlots { get; set; } =
        new()
        {
            [SButton.DPadDown] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(W)DN.SnS_Bow",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.DPadLeft] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(T)Pickaxe",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.DPadRight] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(T)Axe",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.DPadUp] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(W)4",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.ControllerB] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(O)688",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.ControllerY] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(T)WateringCan",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.ControllerA] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(T)Shears",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
            [SButton.ControllerX] = new()
            {
                IdType = ItemIdType.GameItem,
                Id = "(O)395",
                RequireConfirmation = false,
                UseSecondaryAction = false,
            },
        };

    /// <inheritdoc />
    public bool Equals(ItemsConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return InventoryPageSize == other.InventoryPageSize
            && ShowInventoryBlanks == other.ShowInventoryBlanks
            && ModMenuPages.Count == other.ModMenuPages.Count
            && ModMenuPages.SequenceEqual(
                other.ModMenuPages,
                (page1, page2) => page1.SequenceEqual(page2, (item1, item2) => item1.Equals(item2))
            )
            && ShowSettingsItem == other.ShowSettingsItem
            && SettingsItemPageIndex == other.SettingsItemPageIndex
            && SettingsItemPositionIndex == other.SettingsItemPositionIndex
            && QuickSlots.Count == other.QuickSlots.Count
            && QuickSlots.IsEquivalentTo(other.QuickSlots, (slot1, slot2) => slot1.Equals(slot2));
    }
}
