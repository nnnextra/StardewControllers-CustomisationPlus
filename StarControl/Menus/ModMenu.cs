using StarControl.Config;
using StarControl.Input;

namespace StarControl.Menus;

/// <summary>
/// Radial menu displaying the shortcuts set up in the
/// <see cref="ItemsConfiguration.ModMenuPages"/>, as well as any mod-added pages.
/// </summary>
internal class ModMenu(
    IMenuToggle toggle,
    ModConfig config,
    ModMenuItem settingsItem,
    ModMenuItem instantActionsItem,
    Action<ModMenuItemConfiguration> shortcutActivator,
    IInvalidatableList<IRadialMenuPage> additionalPages,
    Func<int> userPageIndexSelector,
    IEnumerable<IRadialMenuItem> standaloneItems
) : IRadialMenu
{
    public IReadOnlyList<IRadialMenuPage> Pages
    {
        get
        {
            if (isDirty)
            {
                combinedPages = GetCombinedPages();
                isDirty = false;
            }
            return combinedPages;
        }
    }

    public int SelectedPageIndex { get; set; }

    public IMenuToggle Toggle { get; } = toggle;

    private IReadOnlyList<IRadialMenuPage> combinedPages = [];
    private bool isDirty = true;

    /// <summary>
    /// Retries the item (on any page) given its ID.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>The item matching the specified <paramref name="id"/>, or <c>null</c> if not
    /// found.</returns>
    public IRadialMenuItem? GetItem(string id)
    {
        return Pages
            .OfType<MenuPage<IRadialMenuItem>>()
            .SelectMany(page => page.InternalItems)
            .FirstOrDefault(item => item?.Id == id);
    }

    /// <summary>
    /// Recreates the items on the shortcut page (first page of this menu) and marks all other (mod) pages invalid,
    /// causing them to be recreated when next accessed.
    /// </summary>
    /// <remarks>
    /// Use when shortcuts have changed or may have changed, e.g. after the configuration was edited or upstream mod
    /// keybindings were changed.
    /// </remarks>
    public void Invalidate()
    {
        Logger.Log(LogCategory.Menus, "Mod menu invalidated.", LogLevel.Info);
        isDirty = true;
        additionalPages.Invalidate();
    }

    public void ResetSelectedPage()
    {
        Logger.Log(LogCategory.Menus, "Resetting page selection for mod menu.");
        for (int i = 0; i < Pages.Count; i++)
        {
            if (Pages[i].IsEmpty())
            {
                continue;
            }
            Logger.Log(LogCategory.Menus, $"Defaulting selection to non-empty page {i}");
            SelectedPageIndex = i;
            return;
        }
        Logger.Log(
            LogCategory.Menus,
            "Couldn't find non-empty page; defaulting selection to first page."
        );
        SelectedPageIndex = 0;
    }

    private IReadOnlyList<IRadialMenuPage> GetCombinedPages()
    {
        var userPages = new List<IRadialMenuPage>();
        int pageIndex = 0;
        foreach (var pageConfig in config.Items.ModMenuPages)
        {
            Logger.Log(LogCategory.Menus, $"Creating page {pageIndex} of mod menu...");
            userPages.Add(
                MenuPage.FromModItemConfigurations(
                    pageConfig,
                    standaloneItems,
                    shortcutActivator,
                    pageIndex == config.Items.SettingsItemPageIndex && config.Items.ShowSettingsItem
                    || pageIndex == config.Items.InstantActionsItemPageIndex
                        && config.Items.ShowInstantActionsItem
                        ? items => InsertBuiltInItems(items, pageIndex)
                        : null
                )
            );
            pageIndex++;
        }
        if (
            userPages.Count == 0
            && (config.Items.ShowSettingsItem || config.Items.ShowInstantActionsItem)
        )
        {
            var pageItems = new List<IRadialMenuItem>();
            InsertBuiltInItems(pageItems, pageIndex: 0);
            userPages.Add(new MenuPage<IRadialMenuItem>(pageItems, _ => false));
        }
        var pages = new List<IRadialMenuPage>(additionalPages);
        pages.InsertRange(userPageIndexSelector(), userPages);
        Logger.Log(
            LogCategory.Menus,
            $"Added {userPages.Count} user pages and {additionalPages.Count} "
                + "external pages to mod menu."
        );
        return pages;

        void InsertBuiltInItems(List<IRadialMenuItem> items, int pageIndex)
        {
            var inserts = new List<(int Index, IRadialMenuItem Item, string LogName)>();
            if (config.Items.ShowSettingsItem && pageIndex == config.Items.SettingsItemPageIndex)
            {
                inserts.Add((config.Items.SettingsItemPositionIndex, settingsItem, "Mod Settings"));
            }
            if (
                config.Items.ShowInstantActionsItem
                && pageIndex == config.Items.InstantActionsItemPageIndex
            )
            {
                inserts.Add(
                    (
                        config.Items.InstantActionsItemPositionIndex,
                        instantActionsItem,
                        "Instant Actions"
                    )
                );
            }
            if (inserts.Count == 0)
            {
                return;
            }
            foreach (var insert in inserts.OrderBy(entry => entry.Index))
            {
                var index = Math.Clamp(insert.Index, 0, items.Count);
                items.Insert(index, insert.Item);
                Logger.Log(
                    LogCategory.Menus,
                    $"Inserted built-in {insert.LogName} item at position {index}."
                );
            }
        }
    }
}
