namespace StarControl.Config;

/// <summary>
/// Configures the controller menu's interactions, including key bindings and activation behavior.
/// </summary>
public class InputConfiguration : IConfigEquatable<InputConfiguration>
{
    /// <summary>
    /// Button to open the inventory wheel during normal gameplay.
    /// </summary>
    public SButton InventoryMenuButton { get; set; } = SButton.LeftTrigger;

    /// <summary>
    /// Button to open the mod action wheel during normal gameplay.
    /// </summary>
    public SButton ModMenuButton { get; set; } = SButton.RightTrigger;

    /// <summary>
    /// Button to cycle to the previous page of any open wheel.
    /// </summary>
    public SButton PreviousPageButton { get; set; } = SButton.LeftShoulder;

    /// <summary>
    /// Button to cycle to the next page of any open wheel.
    /// </summary>
    public SButton NextPageButton { get; set; } = SButton.RightShoulder;

    /// <summary>
    /// Button to perform the primary action on the selected menu item (typically "use").
    /// </summary>
    public SButton PrimaryActionButton { get; set; } = SButton.ControllerA;

    /// <summary>
    /// Alternate method to invoke an item's primary action in place of the
    /// <see cref="PrimaryActionButton"/>.
    /// </summary>
    public ItemActivationMethod PrimaryActivationMethod { get; set; } =
        ItemActivationMethod.Keybind;

    /// <summary>
    /// Button to perform the secondary action on the selected menu item (typically "select").
    /// </summary>
    public SButton SecondaryActionButton { get; set; } = SButton.ControllerX;

    /// <summary>
    /// Alternate method to invoke an item's secondary action in place of the
    /// <see cref="SecondaryActionButton"/>.
    /// </summary>
    public ItemActivationMethod SecondaryActivationMethod { get; set; } =
        ItemActivationMethod.TriggerRelease;

    /// <summary>
    /// Selects which thumbstick is used to navigate the wheel after opening.
    /// </summary>
    public ThumbStickPreference ThumbStickPreference { get; set; } =
        ThumbStickPreference.AlwaysLeft;

    /// <summary>
    /// Selects which thumbstick is used to navigate the inventory wheel after opening.
    /// </summary>
    public ThumbStickPreference InventoryThumbStickPreference { get; set; } =
        ThumbStickPreference.AlwaysLeft;

    /// <summary>
    /// Selects which thumbstick is used to navigate the mod wheel after opening.
    /// </summary>
    public ThumbStickPreference ModMenuThumbStickPreference { get; set; } =
        ThumbStickPreference.AlwaysLeft;

    /// <summary>
    /// Whether mouse/trackpad movement can be used to navigate radial menus.
    /// </summary>
    public bool EnableMouseWheelNavigation { get; set; }

    /// <summary>
    /// Whether the <see cref="InventoryMenuButton"/> or <see cref="ModMenuButton"/> are used to
    /// hold the menu open (default) or toggle it on/off.
    /// </summary>
    public MenuToggleMode ToggleMode { get; set; } = MenuToggleMode.Hold;

    /// <summary>
    /// Button to open the remapping menu, allowing the default in-world behavior (i.e. when no
    /// radial menu is active) to be changed.
    /// </summary>
    public SButton RemappingMenuButton { get; set; } = SButton.LeftStick;

    /// <summary>
    /// Button to toggle the remapping HUD, showing world actions assigned to each button.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This HUD is the same as the Quick Slots shown while the pie menu UI is open, but showing
    /// Instant Actions instead of Quick Actions.
    /// </para>
    /// <para>
    /// Note: Logically this could be assigned to the Right Stick by default, but doing so would
    /// conflict with the vanilla chatbox toggle, and <see cref="SuppressRightStickChatBox"/> is not
    /// enabled by default in order to avoid too many initially unwanted side effects for new
    /// players. If assigning this to the right stick, that option should be enabled as well.
    /// </para>
    /// </remarks>
    public SButton RemappingHudButton { get; set; } = SButton.None;

    /// <summary>
    /// Whether to allow reopening the menu after an item activation if the corresponding trigger
    /// button has been held the whole time.
    /// </summary>
    /// <remarks>
    /// Only affects <see cref="MenuToggleMode.Hold"/>, since <see cref="MenuToggleMode.Toggle"/>
    /// already watches for press-release transitions.
    /// </remarks>
    public bool ReopenOnHold { get; set; }

    /// <summary>
    /// Specifies which actions chosen from the inventory/mod wheels should be delayed (blink)
    /// before being executed.
    /// </summary>
    public DelayedActions DelayedActions { get; set; } = DelayedActions.ToolSwitch;

    /// <summary>
    /// Duration to hold the menu open (ignoring further inputs, and with gameplay paused) and blink
    /// the selected item before the item activation completes. Only applies to actions meeting the
    /// <see cref="DelayedActions"/> criteria.
    /// </summary>
    public int ActivationDelayMs { get; set; } = 150;

    /// <summary>
    /// Whether to remember the player's previous selection per menu.
    /// </summary>
    /// <remarks>
    /// If enabled, closing and reopening a menu will cause it to reopen to the last open page,
    /// instead of resetting to the default (current backpack page for inventory, first page for
    /// custom menu).
    /// </remarks>
    public bool RememberSelection { get; set; }

    /// <summary>
    /// Dead zone for the left/right trigger buttons for activating/deactivating the menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Triggers are generally used as regular buttons in Stardew Valley, but are technically
    /// analog inputs. Due to a variety of technical issues, this mod needs to ignore the
    /// simpler on/off behavior and read the analog input directly. Increase the dead zone if
    /// necessary to prevent accidental presses, or reduce it for hair-trigger response.
    /// </para>
    /// <para>
    /// Only applicable when either or both of the <see cref="InventoryMenuButton"/> and
    /// <see cref="ModMenuButton"/> properties are set to use triggers.
    /// </para>
    /// </remarks>
    public float TriggerDeadZone { get; set; } = 0.2f;

    /// <summary>
    /// Dead-zone for the thumbstick when selecting from a radial menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only applies to menu selection; changing this setting will not affect the dead zone used
    /// for any other mods or controls in the vanilla game.
    /// </para>
    /// <para>
    /// Many, if not most controllers suffer from drift issues in the analog sticks. Setting
    /// this value too low could cause items to get selected even when the thumbstick has not
    /// been moved.
    /// </para>
    /// </remarks>
    public float ThumbstickDeadZone { get; set; } = 0.2f;

    /// <summary>
    /// Whether to suppress the game's default behavior of opening the chat box when the right
    /// stick is pressed.
    /// </summary>
    /// <remarks>
    /// This does not perform a blanket suppression and will not interfere with the emote menu or
    /// other right-stick functions; it exclusively blocks only the chat box, and only when it is
    /// activated using the right stick, i.e. not when using the keyboard bindings.
    /// </remarks>
    public bool SuppressRightStickChatBox { get; set; }

    /// <inheritdoc />
    public bool Equals(InputConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return InventoryMenuButton == other.InventoryMenuButton
            && ModMenuButton == other.ModMenuButton
            && PreviousPageButton == other.PreviousPageButton
            && NextPageButton == other.NextPageButton
            && PrimaryActionButton == other.PrimaryActionButton
            && PrimaryActivationMethod == other.PrimaryActivationMethod
            && SecondaryActionButton == other.SecondaryActionButton
            && SecondaryActivationMethod == other.SecondaryActivationMethod
            && ThumbStickPreference == other.ThumbStickPreference
            && InventoryThumbStickPreference == other.InventoryThumbStickPreference
            && ModMenuThumbStickPreference == other.ModMenuThumbStickPreference
            && EnableMouseWheelNavigation == other.EnableMouseWheelNavigation
            && ToggleMode == other.ToggleMode
            && RemappingMenuButton == other.RemappingMenuButton
            && RemappingHudButton == other.RemappingHudButton
            && ReopenOnHold == other.ReopenOnHold
            && DelayedActions == other.DelayedActions
            && ActivationDelayMs == other.ActivationDelayMs
            && RememberSelection == other.RememberSelection
            && TriggerDeadZone.Equals(other.TriggerDeadZone)
            && ThumbstickDeadZone.Equals(other.ThumbstickDeadZone);
    }

    public void ApplyLegacyThumbStickPreference()
    {
        if (ThumbStickPreference == ThumbStickPreference.SameAsTrigger)
        {
            InventoryThumbStickPreference = ThumbStickPreference.AlwaysLeft;
            ModMenuThumbStickPreference = ThumbStickPreference.AlwaysRight;
            return;
        }
        if (
            InventoryThumbStickPreference == ThumbStickPreference.AlwaysLeft
            && ModMenuThumbStickPreference == ThumbStickPreference.AlwaysLeft
            && ThumbStickPreference != ThumbStickPreference.AlwaysLeft
        )
        {
            InventoryThumbStickPreference = ThumbStickPreference;
            ModMenuThumbStickPreference = ThumbStickPreference;
        }
    }
}
