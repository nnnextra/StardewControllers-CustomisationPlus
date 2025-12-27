using System.ComponentModel;
using PropertyChanged.SourceGenerator;
using StardewValley.ItemTypeDefinitions;

namespace StarControl.UI;

internal enum QuickSlotItemSource
{
    Inventory,
    GameItems,
    ModItems,
}

internal partial class QuickSlotPickerViewModel
{
    private const int MAX_RESULTS = 40;

    public Color AllModPagesTint =>
        ModMenuPageIndex < 0 ? new Color(0xaa, 0xcc, 0xee) : Color.White;

    public Vector2 ContentPanelSize
    {
        get => Pager.ContentPanelSize;
        set => Pager.ContentPanelSize = value;
    }

    public bool HasLoadedGame => Game1.hasLoadedGame;
    public bool HasMoreModItems =>
        ModMenuPageIndex < 0 && ModMenuPages.Sum(page => page.Items.Count) > MAX_RESULTS;
    public IEnumerable<QuickSlotPickerItemViewModel> InventoryItems =>
        Game1
            .player.Items.Where(item => item is not null)
            .Select(QuickSlotPickerItemViewModel.ForItem)
            .ToArray();
    public EnumSegmentsViewModel<QuickSlotItemSource> ItemSource { get; } = new();
    public IEnumerable<QuickSlotPickerItemViewModel> ModMenuItems =>
        (
            ModMenuPageIndex < 0
                ? ModMenuPages.SelectMany(page => page.Items).Take(MAX_RESULTS)
                : ModMenuPages[ModMenuPageIndex].Items
        ).Select(QuickSlotPickerItemViewModel.ForModAction);
    public IReadOnlyList<ModMenuPageConfigurationViewModel> ModMenuPages { get; }
    public string MoreModItemsMessage =>
        HasMoreModItems ? I18n.Config_QuickSlot_ModItems_LimitedResults(MAX_RESULTS) : "";
    public string MoreResultsMessage =>
        HasMoreSearchResults ? I18n.Config_QuickSlot_Search_LimitedResults(MAX_RESULTS) : "";
    public PagerViewModel<PageViewModel> Pager { get; } =
        new() { Pages = [new(0), new(1), new(2)] };
    public bool SecondaryActionProhibited => !SecondaryActionAllowed;
    public QuickSlotConfigurationViewModel Slot { get; }

    private readonly IReadOnlyList<ParsedItemData> allItems;

    [Notify(Setter.Private)]
    private bool hasMoreSearchResults;

    [Notify]
    private int modMenuPageIndex = -1; // Use -1 for "all", if they fit

    [Notify(Setter.Private)]
    private IReadOnlyList<QuickSlotPickerItemViewModel> searchResults = [];

    [Notify]
    private string searchText = "";

    [Notify]
    private bool secondaryActionAllowed;

    private readonly object searchLock = new();

    private CancellationTokenSource searchCancellationTokenSource = new();

    public QuickSlotPickerViewModel(
        QuickSlotConfigurationViewModel slot,
        IReadOnlyList<ParsedItemData> allItems,
        IReadOnlyList<ModMenuPageConfigurationViewModel> modMenuPages
    )
    {
        Slot = slot;
        SecondaryActionAllowed = Slot.ItemData is not null || Slot.ModAction is not null;
        this.allItems = allItems;
        ModMenuPages = modMenuPages;
        UpdateSearchResults();
        ItemSource.PropertyChanged += ItemSource_PropertyChanged;
    }

    public void AssignItem(QuickSlotPickerItemViewModel item)
    {
        Game1.playSound("drumkit6");
        item.UpdateSlot(Slot);
        SecondaryActionAllowed = Slot.ItemData is not null || Slot.ModAction is not null;
    }

    public void ClearAssignment()
    {
        if (Slot.ItemData is null && Slot.ModAction is null)
        {
            return;
        }
        Game1.playSound("trashcan");
        Slot.Clear();
        SecondaryActionAllowed = false;
    }

    public bool HandleButtonPress(SButton button)
    {
        if (!Pager.HandleButtonPress(button))
        {
            return false;
        }
        ItemSource.SelectedIndex = Pager.SelectedPageIndex;
        return true;
    }

    public void SelectModMenuPage(int index)
    {
        if (index == ModMenuPageIndex)
        {
            return;
        }
        Game1.playSound("smallSelect");
        ModMenuPageIndex = index;
    }

    private void ItemSource_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Pager.SelectedPageIndex = ItemSource.SelectedIndex;
    }

    private void OnModMenuPageIndexChanged(int oldValue, int newValue)
    {
        if (oldValue >= 0)
        {
            ModMenuPages[oldValue].Selected = false;
        }
        if (newValue >= 0)
        {
            ModMenuPages[newValue].Selected = true;
        }
    }

    private void OnSearchTextChanged()
    {
        UpdateSearchResults();
    }

    private void UpdateSearchResults()
    {
        searchCancellationTokenSource.Cancel();
        searchCancellationTokenSource = new();
        var cancellationToken = searchCancellationTokenSource.Token;
        IEnumerable<ParsedItemData> searchResults;
        try
        {
            searchResults = allItems.Search(SearchText, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed searching for items: {ex}", LogLevel.Error);
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        lock (searchLock)
        {
            var results = new List<QuickSlotPickerItemViewModel>();
            // Results can easily be limited using .Take(limit), but we also need to know if there are more,
            // which no Linq extension can tell us.
            using var resultEnumerator = searchResults.GetEnumerator();
            while (results.Count < MAX_RESULTS && resultEnumerator.MoveNext())
            {
                results.Add(QuickSlotPickerItemViewModel.ForItemData(resultEnumerator.Current));
            }
            SearchResults = results;
            HasMoreSearchResults = resultEnumerator.MoveNext();
        }
    }
}
