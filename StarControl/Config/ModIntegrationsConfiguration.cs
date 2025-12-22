namespace StarControl.Config;

/// <summary>
/// Configures settings and priorities for all mod integrations.
/// </summary>
public class ModIntegrationsConfiguration : IConfigEquatable<ModIntegrationsConfiguration>
{
    /// <summary>
    /// The priority to assign to the <see cref="ItemsConfiguration.ModMenuPages"/> relative to any
    /// third-party integrations.
    /// </summary>
    /// <remarks>
    /// The default value of <c>0</c> means that custom (user-specified) items will appear first.
    /// Changing it can allow the pages of other mods, such as Iconic, to appear first instead.
    /// </remarks>
    public int CustomItemsPriority { get; set; }

    /// <summary>
    /// The relative priorities of all third-party mods, independent of the
    /// <see cref="CustomItemsPriority"/>.
    /// </summary>
    /// <remarks>
    /// The order that the mods appear in this list is the same order in which their pages will
    /// appear in the Mod Menu when using the next/previous page buttons.
    /// </remarks>
    public List<ModPriorityConfiguration> Priorities { get; set; } =
        [new ModPriorityConfiguration { ModId = "KCC.SnS", Enabled = true }];

    /// <inheritdoc />
    public bool Equals(ModIntegrationsConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return CustomItemsPriority == other.CustomItemsPriority
            && Priorities.Count == other.Priorities.Count
            && Priorities.SequenceEqual(
                other.Priorities,
                (priority1, priority2) => priority1.Equals(priority2)
            );
    }
}
