using PropertyChanged.SourceGenerator;
using StarControl.Config;

namespace StarControl.UI;

internal partial class QuickSlotGroupConfigurationViewModel
{
    [Notify]
    private QuickSlotConfigurationViewModel dPadDown = new();

    [Notify]
    private QuickSlotConfigurationViewModel dPadLeft = new();

    [Notify]
    private QuickSlotConfigurationViewModel dPadRight = new();

    [Notify]
    private QuickSlotConfigurationViewModel dPadUp = new();

    [Notify]
    private QuickSlotConfigurationViewModel east = new();

    [Notify]
    private QuickSlotConfigurationViewModel north = new();

    [Notify]
    private QuickSlotConfigurationViewModel south = new();

    [Notify]
    private QuickSlotConfigurationViewModel west = new();

    public void Clear()
    {
        DPadDown.Clear();
        DPadLeft.Clear();
        DPadRight.Clear();
        DPadUp.Clear();
        East.Clear();
        North.Clear();
        South.Clear();
        West.Clear();
    }

    public void Load(
        IReadOnlyDictionary<SButton, QuickSlotConfiguration> configs,
        IReadOnlyCollection<ModMenuPageConfigurationViewModel> modMenuPages
    )
    {
        Load(DPadDown, SButton.DPadDown, configs, modMenuPages);
        Load(DPadLeft, SButton.DPadLeft, configs, modMenuPages);
        Load(DPadRight, SButton.DPadRight, configs, modMenuPages);
        Load(DPadUp, SButton.DPadUp, configs, modMenuPages);
        Load(East, SButton.ControllerB, configs, modMenuPages);
        Load(North, SButton.ControllerY, configs, modMenuPages);
        Load(South, SButton.ControllerA, configs, modMenuPages);
        Load(West, SButton.ControllerX, configs, modMenuPages);
    }

    private static void Load(
        QuickSlotConfigurationViewModel target,
        SButton button,
        IReadOnlyDictionary<SButton, QuickSlotConfiguration> configs,
        IReadOnlyCollection<ModMenuPageConfigurationViewModel> modMenuPages
    )
    {
        target.Clear();
        if (!configs.TryGetValue(button, out var config))
        {
            return;
        }
        switch (config.IdType)
        {
            case ItemIdType.GameItem:
                target.ItemData = ItemRegistry.GetDataOrErrorItem(config.Id);
                target.ItemSubId = config.SubId;
                break;
            case ItemIdType.ModItem:
                target.ModAction = modMenuPages
                    .SelectMany(page => page.Items)
                    .FirstOrDefault(item => item.Id == config.Id);
                break;
            default:
                Logger.Log(
                    $"Item with ID '{config.Id}' has unsupported ID type '{config.IdType}'."
                );
                break;
        }
        target.RequireConfirmation = config.RequireConfirmation;
        target.UseSecondaryAction = config.UseSecondaryAction;
    }

    public void Save(IDictionary<SButton, QuickSlotConfiguration> configs)
    {
        Save(DPadDown, SButton.DPadDown, configs);
        Save(DPadLeft, SButton.DPadLeft, configs);
        Save(DPadRight, SButton.DPadRight, configs);
        Save(DPadUp, SButton.DPadUp, configs);
        Save(East, SButton.ControllerB, configs);
        Save(North, SButton.ControllerY, configs);
        Save(South, SButton.ControllerA, configs);
        Save(West, SButton.ControllerX, configs);
    }

    private static void Save(
        QuickSlotConfigurationViewModel target,
        SButton button,
        IDictionary<SButton, QuickSlotConfiguration> configs
    )
    {
        if (target.IsAssigned)
        {
            configs[button] = new()
            {
                IdType = target.ItemData is not null ? ItemIdType.GameItem : ItemIdType.ModItem,
                Id = target.ItemData?.QualifiedItemId ?? target.ModAction?.Id ?? "",
                SubId = target.ItemData is not null ? target.ItemSubId : null,
                RequireConfirmation = target.RequireConfirmation,
                UseSecondaryAction = target.UseSecondaryAction,
            };
        }
        else
        {
            configs.Remove(button);
        }
    }
}
