using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;

namespace StarControl.Menus;

internal class QuickSlotResolver(Farmer player, ModMenu modMenu)
{
    public static Item? ResolveInventoryItem(string id, string? subId, ICollection<Item> items)
    {
        if (string.IsNullOrEmpty(subId))
            return ResolveInventoryItem(id, items);

        return items.FirstOrDefault(i =>
            i is not null
            && i.QualifiedItemId == id
            && Compat.ItemBagsIdentity.TryGetBagTypeId(i) == subId
        );
    }

    public static Item? ResolveInventoryItem(string id, ICollection<Item> items)
    {
        // Allow exact inventory matches even if the item is not registered in ItemRegistry (e.g. Item Bags)
        var exact = items.FirstOrDefault(i => i is not null && i.QualifiedItemId == id);
        if (exact is not null)
            return exact;

        Logger.Log(LogCategory.QuickSlots, $"Searching for inventory item equivalent to '{id}'...");
        if (ItemRegistry.GetData(id) is not { } data)
        {
            Logger.Log(
                LogCategory.QuickSlots,
                $"'{id}' does not have validx item data; aborting search."
            );
            return null;
        }
        // Melee weapons don't have upgrades or base items, but if we didn't find an exact match, it
        // is often helpful to find any other melee weapon that's available.
        // Only apply fuzzy matching to melee weapons; slingshots must match exactly
        if (
            data.ItemType.Identifier == "(W)"
            && !data.QualifiedItemId.Contains("Slingshot")
            && !data.QualifiedItemId.Contains("Bow")
        )
        {
            // We'll match scythes to scythes, and non-scythes to non-scythes.
            // Most likely, the player wants Iridium Scythe if the slot says Scythe. The upgraded
            // version is simply better, like a more typical tool.
            //
            // With real weapons it's fuzzier because the highest-level weapon isn't necessarily
            // appropriate for the situation. If there's one quick slot for Galaxy Sword and another
            // for Galaxy Hammer, then activating those slots should activate their *specific*
            // weapons respectively if both are in the inventory.
            //
            // So we match on the inferred "type" (scythe vs. weapon) and then for non-scythe
            // weapons specifically (and only those), give preference to exact matches before
            // sorting by level.
            var isScythe = IsScythe(data);
            Logger.Log(
                LogCategory.QuickSlots,
                $"Item '{id}' appears to be a weapon with (scythe = {isScythe})."
            );
            var bestWeapon = items
                .OfType<MeleeWeapon>()
                .Where(weapon => weapon.Name.Contains("Scythe") == isScythe)
                .OrderByDescending(weapon => !isScythe && weapon.QualifiedItemId == id)
                .ThenByDescending(weapon => weapon.getItemLevel())
                .FirstOrDefault();
            if (bestWeapon is not null)
            {
                Logger.Log(
                    LogCategory.QuickSlots,
                    "Best weapon match in inventory is "
                        + $"{bestWeapon.Name} with ID {bestWeapon.QualifiedItemId}."
                );
                return bestWeapon;
            }
        }
        var baseItem = data.GetBaseItem();
        Logger.Log(
            LogCategory.QuickSlots,
            "Searching for regular item using base item "
                + $"{baseItem.InternalName} with ID {baseItem.QualifiedItemId}."
        );
        var match = items
            .Where(item => item is not null)
            .Where(item =>
                item.QualifiedItemId == id
                || ItemRegistry
                    .GetDataOrErrorItem(item.QualifiedItemId)
                    .GetBaseItem()
                    .QualifiedItemId == baseItem.QualifiedItemId
            )
            .OrderByDescending(item => item is Tool tool ? tool.UpgradeLevel : 0)
            .ThenByDescending(item => item.Quality)
            .FirstOrDefault();
        Logger.Log(
            LogCategory.QuickSlots,
            $"Best match by quality/upgrade level is "
                + $"{match?.Name ?? "(nothing)"} with ID {match?.QualifiedItemId ?? "N/A"}."
        );
        return match;
    }

    private static bool IsScythe(ParsedItemData data)
    {
        var method = typeof(MeleeWeapon).GetMethod(
            "IsScythe",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null
        );
        if (method is not null)
        {
            try
            {
                return (bool)method.Invoke(null, [data.QualifiedItemId])!;
            }
            catch
            {
                // Fall through to heuristic below.
            }
        }
        var name = data.InternalName ?? string.Empty;
        return name.Contains("Scythe", StringComparison.OrdinalIgnoreCase)
            || data.QualifiedItemId.Contains("Scythe", StringComparison.OrdinalIgnoreCase);
    }

    private static Type? ItemBagType;
    private static PropertyInfo? ItemBagContentsProp;

    private static Type? OmniBagType;
    private static PropertyInfo? OmniNestedBagsProp;

    private static Type? BundleBagType;

    private static bool TryInitItemBagsReflection()
    {
        if (ItemBagType is not null && ItemBagContentsProp is not null && BundleBagType is not null)
            return true;

        // Find ItemBags.Bags.ItemBag
        ItemBagType ??= AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("ItemBags.Bags.ItemBag", throwOnError: false))
            .FirstOrDefault(t => t is not null);

        if (ItemBagType is null)
            return false;

        // public List<Object> Contents { get; set; }
        ItemBagContentsProp ??= ItemBagType.GetProperty(
            "Contents",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (ItemBagContentsProp is null)
            return false;

        // Find BundleBag type so we can EXCLUDE traversing into its contents
        BundleBagType ??= AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("ItemBags.Bags.BundleBag", throwOnError: false))
            .FirstOrDefault(t => t is not null);

        if (BundleBagType is null)
            return false;

        // Omni bag support (nested bags)
        OmniBagType ??= AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("ItemBags.Bags.OmniBag", throwOnError: false))
            .FirstOrDefault(t => t is not null);

        if (OmniBagType is not null)
            OmniNestedBagsProp ??= OmniBagType.GetProperty(
                "NestedBags",
                BindingFlags.Public | BindingFlags.Instance
            );

        return true;
    }

    internal static bool IsItemBag(Item item) =>
        ItemBagType is not null && ItemBagType.IsInstanceOfType(item);

    private static bool IsBundleBag(Item item) =>
        BundleBagType is not null && BundleBagType.IsInstanceOfType(item);

    private static IEnumerable<Item> EnumerateBagContents(Item bag)
    {
        // BundleBag is explicitly excluded
        if (IsBundleBag(bag))
            yield break;

        if (ItemBagContentsProp?.GetValue(bag) is not IList list || list.Count == 0)
            yield break;

        foreach (var obj in list)
            if (obj is Item inner)
                yield return inner;
    }

    private static IEnumerable<Item> EnumerateOmniNestedBags(Item bag)
    {
        if (OmniBagType is null || OmniNestedBagsProp is null || !OmniBagType.IsInstanceOfType(bag))
            yield break;

        if (OmniNestedBagsProp.GetValue(bag) is not IList list || list.Count == 0)
            yield break;

        foreach (var obj in list)
            if (obj is Item innerBag)
                yield return innerBag;
    }

    /// <summary>
    /// Returns an "effective inventory" which includes:
    /// - the player's inventory
    /// - contents of any ItemBags bags in the player's inventory (EXCEPT BundleBag contents)
    /// - nested bags inside OmniBags (and then their contents too)
    /// </summary>
    public static ICollection<Item> GetExpandedPlayerItems(Farmer who)
    {
        var baseItems = who.Items;

        if (!TryInitItemBagsReflection())
            return baseItems;

        var expanded = new List<Item>(baseItems.Count + 16);

        // BFS over bags we discover (supports OmniBag nesting)
        var seen = new HashSet<Item>();
        var bagQueue = new Queue<Item>();

        // 1) Start with player inventory
        foreach (var it in baseItems)
        {
            if (it is null)
                continue;
            expanded.Add(it);

            if (IsItemBag(it))
            {
                if (seen.Add(it))
                    bagQueue.Enqueue(it);
            }
        }

        // 2) Expand bags: add nested bags (omni) + contents (except bundle)
        int depth = 0;
        while (bagQueue.Count > 0 && depth < 6)
        {
            int layer = bagQueue.Count;
            for (int i = 0; i < layer; i++)
            {
                var bag = bagQueue.Dequeue();

                // Omni nested bags
                foreach (var nestedBag in EnumerateOmniNestedBags(bag))
                {
                    expanded.Add(nestedBag);
                    if (IsItemBag(nestedBag) && seen.Add(nestedBag))
                        bagQueue.Enqueue(nestedBag);
                }

                // Bag contents (except BundleBag)
                foreach (var innerItem in EnumerateBagContents(bag))
                {
                    expanded.Add(innerItem);

                    // If someone manages to store a bag as an Item (or modded bag item), expand it too.
                    if (IsItemBag(innerItem) && seen.Add(innerItem))
                        bagQueue.Enqueue(innerItem);
                }
            }

            depth++;
        }

        return expanded;
    }

    public IRadialMenuItem? ResolveItem(string id, ItemIdType idType)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        return idType switch
        {
            ItemIdType.GameItem => ResolveInventoryItem(id, GetExpandedPlayerItems(player))
                is { } item
                ? new InventoryMenuItem(item)
                : null,
            ItemIdType.ModItem => modMenu.GetItem(id),
            _ => null,
        };
    }

    public IRadialMenuItem? ResolveItem(string id, string? subId, ItemIdType idType)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return idType switch
        {
            ItemIdType.GameItem => ResolveInventoryItem(id, subId, GetExpandedPlayerItems(player))
                is { } item
                ? new InventoryMenuItem(item)
                : null,
            ItemIdType.ModItem => modMenu.GetItem(id),
            _ => null,
        };
    }
}
