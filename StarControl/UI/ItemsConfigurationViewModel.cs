using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PropertyChanged.SourceGenerator;
using StarControl.Config;
using StarControl.Graphics;
using StardewValley.ItemTypeDefinitions;

namespace StarControl.UI;

// TODO: Provide a way to move items to a new, blank page, e.g. a dummy item to swap with.

internal partial class ItemsConfigurationViewModel
{
    private const int MAX_PAGE_SIZE = 16;

    public string AddButtonTooltip =>
        IsPageFull
            ? I18n.Config_ModMenu_MaxItems_Description()
            : I18n.Config_ModMenu_AddItem_Description();
    public IReadOnlyList<ApiItemViewModel> ApiItems { get; set; } = [];
    public bool CanAddItem => !IsReordering;
    public bool CanRemoveItem => IsReordering;
    public bool IsPageFull => SelectedPageSize >= MAX_PAGE_SIZE;
    public bool IsReordering => GrabbedItem is not null;
    public PagerViewModel<ModMenuPageConfigurationViewModel> Pager { get; } =
        new() { Pages = [new(0)] };

    private readonly Task<ParsedItemData[]> allItemsTask = Task.Run(
        () =>
            ItemRegistry
                .ItemTypes.SelectMany(t =>
                    t.GetAllIds()
                        .OrderBy(id =>
                            int.TryParse(id, out var numericId) ? numericId : int.MaxValue
                        )
                        .ThenBy(id => id)
                        .Select(id => t.Identifier + id)
                )
                .Select(ItemRegistry.GetDataOrErrorItem)
                .ToArray()
    );

    [Notify]
    private ModMenuItemConfigurationViewModel? grabbedItem;

    [Notify]
    private int inventoryPageSize = 12;

    [Notify]
    private bool isTrashCanHovered;

    [Notify]
    private int selectedPageSize;

    [Notify]
    private ButtonIconSet buttonIconSet = ButtonIconSet.Xbox;

    [Notify]
    private bool showInventoryBlanks;

    [Notify]
    private QuickSlotGroupConfigurationViewModel quickSlots = new();

    // Settings item can receive empty list for allItems because it is not editable.
    private readonly ModMenuItemConfigurationViewModel settingsItem = new(
        "focustense.StarControl.Settings",
        []
    )
    {
        EditableName = I18n.Config_ModMenu_SettingsItem_Name(),
        EditableDescription = I18n.Config_ModMenu_SettingsItem_Description(),
        CustomIcon = Sprites.Settings(),
        Editable = false,
        IconType = { SelectedValue = ItemIconType.Custom },
    };

    // Instant Actions item can receive empty list for allItems because it is not editable.
    private readonly ModMenuItemConfigurationViewModel instantActionsItem = new(
        "focustense.StarControl.InstantActions",
        []
    )
    {
        EditableName = I18n.Config_ModMenu_InstantActionsItem_Name(),
        EditableDescription = I18n.Config_ModMenu_InstantActionsItem_Description(),
        CustomIcon = Sprites.BugNet(),
        Editable = false,
        IconType = { SelectedValue = ItemIconType.Custom },
    };

    private int grabbedItemIndex; // Within the page
    private int grabbedItemPageIndex;

    public ItemsConfigurationViewModel()
    {
        Pager.PropertyChanging += Pager_PropertyChanging;
        Pager.PropertyChanged += Pager_PropertyChanged;
    }

    public bool AddNewItem()
    {
        if (!CanAddItem || IsPageFull)
        {
            return false;
        }
        var newItem = new ModMenuItemConfigurationViewModel(
            IdGenerator.NewId(6),
            allItemsTask.Result
        )
        {
            // Clone the items so we don't get selection state leaking between pickers.
            ApiItems = ApiItems.Select(item => item.Clone()).ToList(),
            ButtonIconSet = ButtonIconSet,
        };
        Pager.SelectedPage!.Items.Add(newItem);
        EditModMenuItem(newItem);
        return true;
    }

    public bool AddPage()
    {
        Game1.playSound("smallSelect");
        var nextIndex = Pager.Pages.Count;
        Pager.Pages.Add(new(nextIndex));
        Pager.SelectedPageIndex = nextIndex;
        return true;
    }

    public bool BeginReordering(ModMenuItemConfigurationViewModel item)
    {
        var itemIndex = Pager.SelectedPage?.Items.IndexOf(item) ?? -1;
        if (itemIndex < 0)
        {
            return false;
        }
        Game1.playSound("dwop");
        GrabbedItem = item;
        grabbedItemPageIndex = Pager.SelectedPageIndex;
        grabbedItemIndex = itemIndex;
        item.IsReordering = true;
        return true;
    }

    public bool EditModMenuItem(ModMenuItemConfigurationViewModel item)
    {
        if (item.Editable)
        {
            ViewEngine.OpenChildMenu("ModMenuItem", item);
        }
        else
        {
            Game1.playSound("drumkit6");
            item.Enabled = !item.Enabled;
        }
        return true;
    }

    public bool EditQuickSlot(QuickSlotConfigurationViewModel slot)
    {
        var context = new QuickSlotPickerViewModel(
            slot,
            allItemsTask.Result,
            Pager.Pages.Select(page => page.Clone()).ToList()
        );
        ViewEngine.OpenChildMenu("QuickSlotPicker", context);
        return true;
    }

    public bool EndReordering(ModMenuItemConfigurationViewModel? target = null)
    {
        if (GrabbedItem is null)
        {
            return false;
        }
        if (target is not null)
        {
            var targetIndex = Pager.SelectedPage?.Items.IndexOf(target) ?? -1;
            if (targetIndex < 0)
            {
                return false;
            }
            Game1.playSound("stoneStep");
            Pager.SelectedPage!.Items[targetIndex] = GrabbedItem;
            Pager.Pages[grabbedItemPageIndex].Items[grabbedItemIndex] = target;
        }
        GrabbedItem.IsReordering = false;
        GrabbedItem = null;
        return true;
    }

    public void HoverTrashCan()
    {
        Game1.playSound("trashcanlid");
        IsTrashCanHovered = true;
    }

    public void LeaveTrashCan()
    {
        IsTrashCanHovered = false;
    }

    public void Load(ItemsConfiguration config)
    {
        InventoryPageSize = config.InventoryPageSize;
        ShowInventoryBlanks = config.ShowInventoryBlanks;
        Pager.Pages.Clear();
        QuickSlots.Clear();
        allItemsTask.ContinueWith(t =>
        {
            var allItems = t.Result;
            foreach (var pageConfig in config.ModMenuPages)
            {
                var pageViewModel = new ModMenuPageConfigurationViewModel(Pager.Pages.Count);
                foreach (var itemConfig in pageConfig)
                {
                    var itemViewModel = new ModMenuItemConfigurationViewModel(
                        !string.IsNullOrWhiteSpace(itemConfig.Id)
                            ? itemConfig.Id
                            : IdGenerator.NewId(6),
                        allItems
                    )
                    {
                        ApiItems = ApiItems.Select(item => item.Clone()).ToList(),
                        ButtonIconSet = ButtonIconSet,
                    };
                    itemViewModel.Load(itemConfig);
                    pageViewModel.Items.Add(itemViewModel);
                }
                Pager.Pages.Add(pageViewModel);
            }
            if (Pager.Pages.Count == 0)
            {
                Pager.Pages.Add(new(0));
            }
            settingsItem.Enabled = config.ShowSettingsItem;
            settingsItem.ButtonIconSet = ButtonIconSet;
            instantActionsItem.Enabled = config.ShowInstantActionsItem;
            instantActionsItem.ButtonIconSet = ButtonIconSet;

            InsertBuiltInItems();

            QuickSlots.Load(config.QuickSlots, Pager.Pages);

            void InsertBuiltInItems()
            {
                var inserts =
                    new List<(
                        int PageIndex,
                        int PositionIndex,
                        ModMenuItemConfigurationViewModel Item
                    )>();
                inserts.Add(
                    (
                        Math.Clamp(config.SettingsItemPageIndex, 0, Pager.Pages.Count - 1),
                        config.SettingsItemPositionIndex,
                        settingsItem
                    )
                );
                inserts.Add(
                    (
                        Math.Clamp(config.InstantActionsItemPageIndex, 0, Pager.Pages.Count - 1),
                        config.InstantActionsItemPositionIndex,
                        instantActionsItem
                    )
                );
                foreach (var group in inserts.GroupBy(entry => entry.PageIndex))
                {
                    var page = Pager.Pages[group.Key];
                    foreach (var entry in group.OrderBy(e => e.PositionIndex))
                    {
                        var itemIndex = Math.Clamp(entry.PositionIndex, 0, page.Items.Count);
                        page.Items.Insert(itemIndex, entry.Item);
                    }
                }
            }
        });
    }

    public void SetButtonIconSet(ButtonIconSet value)
    {
        ButtonIconSet = value;
        settingsItem.ButtonIconSet = value;
        instantActionsItem.ButtonIconSet = value;
        foreach (var page in Pager.Pages)
        {
            foreach (var item in page.Items)
            {
                item.ButtonIconSet = value;
            }
        }
    }

    public bool RemoveGrabbedItem()
    {
        if (GrabbedItem is null || !GrabbedItem.Editable)
        {
            return false;
        }
        Game1.playSound("trashcan");
        Pager.Pages[grabbedItemPageIndex].Items.Remove(GrabbedItem);
        GrabbedItem = null;
        return true;
    }

    public void Save(ItemsConfiguration config)
    {
        config.InventoryPageSize = InventoryPageSize;
        config.ShowInventoryBlanks = ShowInventoryBlanks;
        config.ModMenuPages.Clear();
        int settingsItemPageIndex = 0;
        int settingsItemPositionIndex = 0;
        int instantActionsItemPageIndex = 0;
        int instantActionsItemPositionIndex = 0;
        foreach (var page in Pager.Pages)
        {
            if (page.Items.Count == 0)
            {
                continue;
            }
            var pageItems = new List<ModMenuItemConfiguration>(page.Items.Count);
            for (int i = 0; i < page.Items.Count; i++)
            {
                var item = page.Items[i];
                if (item == settingsItem)
                {
                    settingsItemPageIndex = page.Index;
                    settingsItemPositionIndex = i;
                    continue;
                }
                if (item == instantActionsItem)
                {
                    instantActionsItemPageIndex = page.Index;
                    instantActionsItemPositionIndex = i;
                    continue;
                }
                var itemConfig = new ModMenuItemConfiguration();
                item.Save(itemConfig);
                pageItems.Add(itemConfig);
            }
            if (pageItems.Count > 0)
            {
                config.ModMenuPages.Add(pageItems);
            }
        }
        config.ShowSettingsItem = settingsItem.Enabled;
        config.SettingsItemPageIndex = settingsItemPageIndex;
        config.SettingsItemPositionIndex = settingsItemPositionIndex;
        config.ShowInstantActionsItem = instantActionsItem.Enabled;
        config.InstantActionsItemPageIndex = instantActionsItemPageIndex;
        config.InstantActionsItemPositionIndex = instantActionsItemPositionIndex;
        QuickSlots.Save(config.QuickSlots);
    }

    private void Pager_PropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(Pager.SelectedPage) && Pager.SelectedPage is { } page)
        {
            page.Items.CollectionChanged += Pager_SelectedPage_ItemsChanged;
        }
    }

    private void Pager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Pager.SelectedPage))
        {
            SelectedPageSize = Pager.SelectedPage?.Items.Count ?? 0;
            if (Pager.SelectedPage is { } page)
            {
                page.Items.CollectionChanged += Pager_SelectedPage_ItemsChanged;
            }
        }
    }

    private void Pager_SelectedPage_ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SelectedPageSize = Pager.SelectedPage?.Items.Count ?? 0;
    }
}

internal partial class ModMenuPageConfigurationViewModel(int index) : PageViewModel(index)
{
    public Color ButtonTint => Selected ? new Color(0xaa, 0xcc, 0xee) : Color.White;

    [Notify]
    private ObservableCollection<ModMenuItemConfigurationViewModel> items = [];

    // View model also holds the selection state, so in order to copy this view model from the main
    // configuration menu to submenus like the Quick Slot editor, we clone them so that they have an
    // independent selection state (and visibility, transforms, etc).
    public ModMenuPageConfigurationViewModel Clone()
    {
        return new(Index) { Items = Items };
    }
}
