namespace StarControl.Graphics;

/// <summary>
/// Common sprites used in the mod.
/// </summary>
public static class Sprites
{
    /// <summary>
    /// Asset path for the mod's own UI tile sheet.
    /// </summary>
    public const string UI_TEXTURE_PATH = "Mods/focustense.StarControl/Sprites/UI";
    public const string UI_PLAYSTATION_TEXTURE_PATH =
        "Mods/focustense.StarControl/Sprites/UI.PlayStation";

    /// <summary>
    /// Sprite of a book, used for the built-in journal item.
    /// </summary>
    public static Sprite? Book() => Sprite.TryLoad(UI_TEXTURE_PATH, new(32, 64, 15, 16));

    /// <summary>
    /// Sprite of a gamepad, used for the Instant Actions menu item.
    /// </summary>
    public static Sprite? Gamepad() => Sprite.TryLoad(UI_TEXTURE_PATH, new(0, 0, 16, 16));

    /// <summary>
    /// Gets the default error item sprite used for missing items.
    /// </summary>
    public static Sprite Error() => Sprite.ForItemId("Error_Invalid");

    /// <summary>
    /// Sprite of a hammer, used for the built-in crafting item.
    /// </summary>
    public static Sprite? Hammer() => Sprite.TryLoad(UI_TEXTURE_PATH, new(0, 64, 16, 16));

    /// <summary>
    /// Sprite of a bug net, used for the Instant Actions menu item.
    /// </summary>
    public static Sprite? BugNet() => Sprite.TryLoad(UI_TEXTURE_PATH, new(64, 0, 16, 16));

    /// <summary>
    /// Sprite of a letter (envelope), used for the built-in mailbox item.
    /// </summary>
    public static Sprite? Letter() => Sprite.TryLoad(UI_TEXTURE_PATH, new(16, 64, 15, 16));

    /// <summary>
    /// Sprite of a stack of menu options, used for the built-in main-menu item.
    /// </summary>
    public static Sprite? Menu() => Sprite.TryLoad(UI_TEXTURE_PATH, new(48, 64, 16, 16));

    /// <summary>
    /// Sprite used for the mod settings menu item.
    /// </summary>
    public static Sprite? Settings() => Sprite.TryLoad(UI_TEXTURE_PATH, new(80, 0, 16, 16));
}
