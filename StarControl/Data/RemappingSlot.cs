namespace StarControl.Data;

/// <summary>
/// Configuration for a button remapping slot.
/// </summary>
/// <remarks>
/// <para>
/// Remapped buttons always perform the <see cref="Menus.ItemActivationType.Instant"/> action
/// associated with their corresponding item, and replace whatever vanilla feature the button would
/// normally perform, e.g. B and Back opening the menu or X using the current tool.
/// </para>
/// <para>
/// Even though it is "remapping" vanilla buttons, the actual assignments tend to be very fluid and
/// context-specific, like most top-down adventures. Because of this, remaps are not considered
/// configuration data; they are simply standalone mod data on which frequent changes are expected.
/// Another reason not to keep this in configuration is so separate participants in a split-screen
/// multiplayer/co-op game can each have their own moment-to-moment assignments.
/// </para>
/// </remarks>
public class RemappingSlot : IItemLookup
{
    /// <summary>
    /// The type of ID that the <see cref="Id"/> refers to.
    /// </summary>
    public ItemIdType IdType { get; set; }

    /// <summary>
    /// Identifies which item to select or use. Can be the ID of a regular game item or of a Mod
    /// Menu action, depending on the <see cref="IdType"/>.
    /// </summary>
    public string Id { get; set; } = "";

    public string? SubId { get; set; }
}
