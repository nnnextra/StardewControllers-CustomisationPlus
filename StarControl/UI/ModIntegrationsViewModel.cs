using System.Collections.ObjectModel;
using PropertyChanged.SourceGenerator;
using StarControl.Config;

namespace StarControl.UI;

internal partial class ModIntegrationsViewModel(
    IModRegistry modRegistry,
    ModPriorityViewModel selfItem
)
{
    // Helps avoid some instability due to the frame delays between binding update (observable collection change),
    // subsequent layout and the next drag event. Without this, the drag handler can oscillate temporarily at the moment
    // the cursor goes out of bounds, especially noticeable when changing direction.
    // Since people generally can't respond or even perceive faster than about 50 ms, the delay should not be a problem.
    private const int DRAG_THROTTLE_FRAMES = 3;

    [Notify]
    private ObservableCollection<ModPriorityViewModel> priorities = [];

    [Notify]
    private ButtonIconSet buttonIconSet = ButtonIconSet.Xbox;

    private ModPriorityViewModel? controllerReorderingItem;
    private int dragFrameCount;

    public void SetButtonIconSet(ButtonIconSet value)
    {
        ButtonIconSet = value;
    }

    public void BeginDrag(ModPriorityViewModel mod)
    {
        if (mod.Dragging)
        {
            return;
        }
        Game1.playSound("dwop");
        dragFrameCount = 0;
        mod.Dragging = true;
    }

    public void Drag(ModPriorityViewModel mod, Vector2 position)
    {
        if (dragFrameCount < DRAG_THROTTLE_FRAMES)
        {
            return;
        }
        if (position.Y < 0)
        {
            MoveUp(mod, fromDrag: true);
            dragFrameCount = 0;
        }
        else if (position.Y > mod.LayoutSize.Y)
        {
            MoveDown(mod, fromDrag: true);
            dragFrameCount = 0;
        }
    }

    public void EndDrag(ModPriorityViewModel mod)
    {
        mod.Dragging = false;
    }

    public bool HandleItemButton(ModPriorityViewModel mod, SButton button)
    {
        if (controllerReorderingItem is null)
        {
            if (button != SButton.ControllerX)
            {
                return false;
            }
            Game1.playSound("dwop");
            dragFrameCount = 0;
            mod.Dragging = true;
            controllerReorderingItem = mod;
            return true;
        }

        if (controllerReorderingItem != mod)
        {
            return false;
        }

        switch (button)
        {
            case SButton.DPadUp
            or SButton.LeftThumbstickUp:
                MoveUp(mod);
                return true;
            case SButton.DPadDown
            or SButton.LeftThumbstickDown:
                MoveDown(mod);
                return true;
            default:
                return false;
        }
    }

    public bool HandleListButton(SButton button)
    {
        if (controllerReorderingItem is null)
        {
            return false;
        }
        if (
            button
                is SButton.ControllerX
                    or SButton.ControllerY
                    or SButton.ControllerA
                    or SButton.ControllerB
                    or SButton.ControllerBack
            || (
                button.TryGetStardewInput(out var inputButton)
                && Game1.options.menuButton.Contains(inputButton)
            )
        )
        {
            Game1.playSound("stoneStep");
            controllerReorderingItem.Dragging = false;
            // Silly and probably very fragile hack to work around the A button (click) being handled
            // at the level underneath this one and therefore not being suppressed. It will always
            // flip the enabled state, so we just flip it back.
            if (button == SButton.ControllerA)
            {
                controllerReorderingItem.Enabled =
                    !controllerReorderingItem.Enabled || controllerReorderingItem.Required;
            }
            controllerReorderingItem = null;
            return true;
        }
        // Trigger buttons would navigate away from this page.
        // Release the item but don't block the page change.
        if (button is SButton.LeftTrigger or SButton.RightTrigger)
        {
            controllerReorderingItem.Dragging = false;
            controllerReorderingItem = null;
        }
        return false;
    }

    public void Load(ModIntegrationsConfiguration config)
    {
        Priorities.Clear();
        foreach (var mod in config.Priorities)
        {
            var manifest = modRegistry.Get(mod.ModId)?.Manifest;
            if (manifest is null)
            {
                continue;
            }
            var item = new ModPriorityViewModel(mod.ModId)
            {
                Name = manifest.Name,
                Description = manifest.Description,
                Enabled = mod.Enabled,
            };
            Priorities.Add(item);
        }
        int selfIndex = Math.Clamp(config.CustomItemsPriority, 0, Priorities.Count);
        Priorities.Insert(selfIndex, selfItem);
    }

    public void Save(ModIntegrationsConfiguration config)
    {
        config.CustomItemsPriority = Priorities.IndexOf(selfItem);
        config.Priorities.Clear();
        foreach (var priority in Priorities)
        {
            if (priority == selfItem)
            {
                continue;
            }
            config.Priorities.Add(new() { ModId = priority.Id, Enabled = priority.Enabled });
        }
    }

    public bool MoveDown(ModPriorityViewModel mod, bool fromDrag = false)
    {
        int priority = Priorities.IndexOf(mod);
        if (priority < 0 || priority >= Priorities.Count - 1)
        {
            return false;
        }
        Game1.playSound(fromDrag ? "stoneStep" : "drumkit5");
        Priorities.Move(priority, priority + 1);
        return true;
    }

    public bool MoveUp(ModPriorityViewModel mod, bool fromDrag = false)
    {
        int priority = Priorities.IndexOf(mod);
        if (priority <= 0)
        {
            return false;
        }
        Game1.playSound(fromDrag ? "stoneStep" : "drumkit3");
        Priorities.Move(priority, priority - 1);
        return true;
    }

    public void Update()
    {
        if (dragFrameCount < DRAG_THROTTLE_FRAMES)
        {
            dragFrameCount++;
        }
    }
}

internal partial class ModPriorityViewModel(string id)
{
    public string Id { get; } = id;

    public bool Required { get; init; } = false;

    [Notify]
    private string description = "";

    [Notify]
    private bool dragging;

    [Notify]
    private bool enabled = true;

    [Notify]
    private Vector2 layoutSize;

    [Notify]
    private string name = "";

    public static ModPriorityViewModel Self(string id, ItemsConfigurationViewModel items)
    {
        var result = new ModPriorityViewModel(id)
        {
            Name = I18n.ModTitle(),
            Description = GetDescription(items.Pager.Pages.Count),
            Required = true,
        };
        items.Pager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PagerViewModel<PageViewModel>.Pages))
            {
                result.Description = GetDescription(items.Pager.Pages.Count);
            }
        };
        items.Pager.Pages.CollectionChanged += (_, _) =>
            result.Description = GetDescription(items.Pager.Pages.Count);
        return result;

        string GetDescription(int pageCount)
        {
            return I18n.Config_ModIntegrations_CustomItems(pageCount);
        }
    }

    private void OnEnabledChanged()
    {
        if (!Enabled && Required)
        {
            Enabled = true;
        }
    }
}
