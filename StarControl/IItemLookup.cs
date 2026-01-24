namespace StarControl;

/// <summary>
/// Provides the fields required to look up a menu item in any configured menu.
/// </summary>
public interface IItemLookup
{
    /// <summary>
    /// Identifies an item to select or use. Can be the ID of a regular game item or of a Mod Menu
    /// action, depending on the <see cref="IdType"/>.
    /// </summary>
    string Id { get; }

    string? SubId { get; }

    /// <summary>
    /// The type of ID that the <see cref="Id"/> refers to.
    /// </summary>
    ItemIdType IdType { get; }
}
