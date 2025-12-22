using System.ComponentModel;
using PropertyChanged.SourceGenerator;

namespace StarControl.UI;

/// <summary>
/// View model for displaying and choosing an enum value using a segmented control.
/// </summary>
/// <typeparam name="T">Type of enum.</typeparam>
/// <param name="translations">The translation helper for retrieving enum display names and
/// descriptions.</param>
public partial class EnumSegmentsViewModel<T> : INotifyPropertyChanged
    where T : struct, Enum
{
    /// <summary>
    /// Raised when the <see cref="SelectedValue"/> changes.
    /// </summary>
    public event EventHandler? ValueChanged;

    private static readonly T[] AllValues = Enum.GetValues<T>();
    private readonly T[] values;

    /// <summary>
    /// List of all segments, one per enum value.
    /// </summary>
    public IReadOnlyList<Segment> Segments { get; }

    /// <summary>
    /// The enum value that is currently selected.
    /// </summary>
    public T SelectedValue
    {
        get => values[SelectedIndex];
        set => SelectedIndex = Array.IndexOf(values, value);
    }

    /// <summary>
    /// The index of the selected enum value.
    /// </summary>
    [Notify]
    private int selectedIndex;

    private static string GetDescription(T value)
    {
        return I18n.GetByKey($"Enum.{typeof(T).Name}.{value}.Description").UsePlaceholder(false);
    }

    private static string GetName(T value)
    {
        return I18n.GetByKey($"Enum.{typeof(T).Name}.{value}.Name");
    }

    private void OnSelectedIndexChanged(int oldValue, int newValue)
    {
        Segments[oldValue].Selected = false;
        Segments[newValue].Selected = true;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public EnumSegmentsViewModel(IEnumerable<T>? allowedValues = null)
    {
        values = (allowedValues ?? AllValues).Distinct().ToArray();
        Segments = values
            .Select((v, i) => new Segment(v, GetName(v), GetDescription(v)) { Selected = i == 0 })
            .ToList();
    }

    /// <summary>
    /// Model for a single segment representing a single enum value.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <param name="name">Name to display in the segment as text.</param>
    /// <param name="description">Optional description to display as a tooltip.</param>
    public partial class Segment(T value, string name, string description)
    {
        /// <summary>
        /// Optional description to display as a tooltip.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Name to display in the segment as text.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// The enum value.
        /// </summary>
        public T Value => value;

        /// <summary>
        /// Whether this is the selected segment.
        /// </summary>
        [Notify]
        private bool selected;
    }
}
