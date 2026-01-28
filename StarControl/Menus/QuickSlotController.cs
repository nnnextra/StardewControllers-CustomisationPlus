using Microsoft.Xna.Framework.Graphics;
using StarControl.Config;

namespace StarControl.Menus;

internal class QuickSlotController(
    IInputHelper inputHelper,
    ModConfig config,
    QuickSlotResolver resolver,
    QuickSlotRenderer renderer
)
{
    private readonly Dictionary<SButton, IRadialMenuItem> slotItems = [];

    private bool isDirty = true;

    public void Draw(SpriteBatch spriteBatch, Rectangle viewport, float opacity = 1)
    {
        if (!config.Style.ShowQuickActions)
        {
            return;
        }
        if (isDirty)
        {
            RefreshSlots();
        }
        renderer.BackgroundOpacity = renderer.SpriteOpacity = opacity;
        renderer.Draw(spriteBatch, viewport);
    }

    public void Invalidate()
    {
        Logger.Log(LogCategory.QuickSlots, "Quick slots invalidated.", LogLevel.Info);
        renderer.Invalidate();
        isDirty = true;
    }

    public void ShowDelayedActivation(SButton button)
    {
        Logger.Log(LogCategory.QuickSlots, $"Starting blink ({button}) for delayed activation.");
        renderer.FlashDelay(button);
    }

    public PendingActivation? TryGetNextActivation(out SButton pressedButton)
    {
        foreach (var button in config.Items.QuickSlots.Keys)
        {
            if (inputHelper.GetState(button) != SButtonState.Pressed)
            {
                continue;
            }
            Logger.Log(LogCategory.QuickSlots, $"Detected button press for {button}.");
            if (!slotItems.TryGetValue(button, out var item))
            {
                Logger.Log(LogCategory.QuickSlots, $"No item in the slot for {button}.");
                Sound.Play(config.Sound.ItemErrorSound);
                renderer.FlashError(button);
                // Suppress explicitly, since it will not be suppressed implicitly as the result
                // of item activation.
                inputHelper.Suppress(button);
                continue;
            }
            var itemConfig = config.Items.QuickSlots[button];
            Logger.Log(
                LogCategory.QuickSlots,
                $"Found item in slot for {button}: ID = {itemConfig.IdType}:{itemConfig.Id}, "
                    + $"secondary action = {itemConfig.UseSecondaryAction}, "
                    + $"require confirmation = {itemConfig.RequireConfirmation}"
            );
            pressedButton = button;
            return new(
                item,
                itemConfig.UseSecondaryAction
                    ? ItemActivationType.Secondary
                    : ItemActivationType.Primary,
                IsRegularItem: itemConfig.IdType == ItemIdType.GameItem,
                RequireConfirmation: itemConfig.RequireConfirmation
            );
        }
        pressedButton = SButton.None;
        return null;
    }

    public void Update(TimeSpan elapsed)
    {
        renderer.Update(elapsed);
    }

    private void RefreshSlots()
    {
        Logger.Log(LogCategory.QuickSlots, "Starting refresh of Quick Slots.");
        renderer.Slots = config.Items.QuickSlots.ToDictionary(
            x => x.Key,
            x => x.Value as IItemLookup
        );
        slotItems.Clear();
        foreach (var (button, slot) in config.Items.QuickSlots)
        {
            if (string.IsNullOrEmpty(slot.Id))
            {
                continue;
            }
            Logger.Log(
                LogCategory.QuickSlots,
                $"Item data for quick slot {button}: ID = {slot.IdType}:{slot.Id}, "
                    + $"secondary action = {slot.UseSecondaryAction}, "
                    + $"require confirmation = {slot.RequireConfirmation}"
            );
            var slottedItem = resolver.ResolveItem(slot.Id, slot.SubId, slot.IdType);
            if (slottedItem is not null)
            {
                Logger.Log(
                    LogCategory.QuickSlots,
                    $"Created quick slot item '{slottedItem.Title}' for {button} slot.",
                    LogLevel.Info
                );
                slotItems.Add(button, slottedItem);
            }
            else
            {
                Logger.Log(
                    LogCategory.QuickSlots,
                    $"No item or invalid item data for quick slot button {button}."
                );
            }
        }
        renderer.SlotItems = slotItems;
        isDirty = false;
    }
}
