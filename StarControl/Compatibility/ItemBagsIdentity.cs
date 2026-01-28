using System.Reflection;
using StardewValley;

namespace StarControl.Compatibility;

internal static class ItemBagsIdentity
{
    private static Type? itemBagBaseType;
    private static MethodInfo? getTypeIdMethod;

    public static string? TryGetBagTypeId(Item item)
    {
        itemBagBaseType ??= AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("ItemBags.Bags.ItemBag", throwOnError: false))
            .FirstOrDefault(t => t is not null);

        if (itemBagBaseType is null || !itemBagBaseType.IsInstanceOfType(item))
            return null;

        getTypeIdMethod ??= itemBagBaseType.GetMethod(
            "GetTypeId",
            BindingFlags.Public | BindingFlags.Instance
        );

        if (getTypeIdMethod?.ReturnType != typeof(string))
            return null;

        try
        {
            return getTypeIdMethod.Invoke(item, null) as string;
        }
        catch
        {
            return null;
        }
    }
}
