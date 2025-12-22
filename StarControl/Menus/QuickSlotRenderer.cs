using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarControl.Config;
using StarControl.Graphics;

namespace StarControl.Menus;

internal class QuickSlotRenderer(GraphicsDevice graphicsDevice, ModConfig config)
{
    private const float MENU_SCALE = 0.7f; // scales the entire quick action menu

    private record ButtonFlash(FlashType Type, float DurationMs, float ElapsedMs = 0);

    private enum FlashType
    {
        Delay,
        Error,
    }

    private enum PromptPosition
    {
        Above,
        Below,
        Left,
        Right,
    }

    public float BackgroundOpacity { get; set; } = 1;
    public float SpriteOpacity { get; set; } = 1;

    public IReadOnlyDictionary<SButton, IRadialMenuItem> SlotItems { get; set; } =
        new Dictionary<SButton, IRadialMenuItem>();
    public IReadOnlyDictionary<SButton, IItemLookup> Slots { get; set; } =
        new Dictionary<SButton, IItemLookup>();

    public bool UnassignedButtonsVisible { get; set; } = true;

    private const int IMAGE_SIZE = 64;
    private const int SLOT_PADDING = 20;
    private const int SLOT_SIZE = (int)((IMAGE_SIZE + SLOT_PADDING * 2) * MENU_SCALE * 1f);
    private const int SLOT_DISTANCE = (int)((SLOT_SIZE - 12) * MENU_SCALE * 2.6f);
    private const int MARGIN_OUTER = 32;
    private const int MARGIN_HORIZONTAL = 120;
    private const int MARGIN_VERTICAL = 16;
    private const int PROMPT_OFFSET = SLOT_SIZE / 2;
    private const int PROMPT_SIZE = 26;
    private const int BACKGROUND_RADIUS = (int)(
        (SLOT_DISTANCE + SLOT_SIZE / 2 + MARGIN_OUTER) * 0.7f
    );

    private static readonly Color OuterBackgroundColor = new(16, 16, 16, 210);

    private readonly Dictionary<SButton, ButtonFlash> flashes = [];
    private readonly HashSet<SButton> enabledSlots = [];
    private readonly Dictionary<SButton, Sprite> slotSprites = [];
    private readonly Texture2D uiTexture = Game1.content.Load<Texture2D>(Sprites.UI_TEXTURE_PATH);
    private readonly GraphicsDevice graphicsDevice = graphicsDevice;

    private Color disabledBackgroundColor = Color.Transparent;
    private Color innerBackgroundColor = Color.Transparent;
    private bool isDirty = true;
    private float quickSlotScale = 1f;
    private Texture2D outerBackground = null!;
    private Texture2D slotBackground = null!;

    public void Draw(SpriteBatch b, Rectangle viewport)
    {
        UpdateScale();
        if (isDirty)
        {
            innerBackgroundColor = (Color)config.Style.OuterBackgroundColor * 0.6f;
            disabledBackgroundColor = LumaGray(innerBackgroundColor, 0.75f);
            RefreshSlots();
        }

        var leftOrigin = new Point(
            viewport.Left
                + Scale(MARGIN_HORIZONTAL * MENU_SCALE)
                + Scale(MARGIN_OUTER * MENU_SCALE)
                + Scale(SLOT_SIZE / 2f),
            viewport.Bottom
                - Scale(MARGIN_VERTICAL * MENU_SCALE)
                - Scale(MARGIN_OUTER * MENU_SCALE)
                - Scale(SLOT_SIZE)
                - Scale(SLOT_SIZE / 2f)
        );
        var leftShoulderPosition = leftOrigin.AddX(Scale(SLOT_DISTANCE * MENU_SCALE));
        if (
            UnassignedButtonsVisible
            || HasSlotSprite(SButton.DPadLeft)
            || HasSlotSprite(SButton.DPadUp)
            || HasSlotSprite(SButton.DPadRight)
            || HasSlotSprite(SButton.DPadDown)
        )
        {
            var leftBackgroundRect = GetCircleRect(
                leftOrigin.AddX(Scale(SLOT_DISTANCE * MENU_SCALE)),
                Scale(BACKGROUND_RADIUS)
            );
            b.Draw(outerBackground, leftBackgroundRect, OuterBackgroundColor * BackgroundOpacity);
            DrawSlot(b, leftOrigin, SButton.DPadLeft, PromptPosition.Left);
            DrawSlot(
                b,
                leftOrigin.Add(
                    Scale(SLOT_DISTANCE * MENU_SCALE),
                    -Scale(SLOT_DISTANCE * MENU_SCALE)
                ),
                SButton.DPadUp,
                PromptPosition.Above
            );
            DrawSlot(
                b,
                leftOrigin.Add(
                    Scale(SLOT_DISTANCE * MENU_SCALE),
                    Scale(SLOT_DISTANCE * MENU_SCALE)
                ),
                SButton.DPadDown,
                PromptPosition.Below
            );
            DrawSlot(
                b,
                leftOrigin.AddX(Scale(SLOT_DISTANCE * MENU_SCALE * 2f)),
                SButton.DPadRight,
                PromptPosition.Right
            );
            leftShoulderPosition.Y -= Scale(SLOT_DISTANCE * 2.5f * MENU_SCALE);
        }
        if (enabledSlots.Contains(SButton.LeftShoulder))
        {
            DrawSlot(
                b,
                leftShoulderPosition,
                SButton.LeftShoulder,
                PromptPosition.Above,
                darken: true
            );
        }

        var rightOrigin = new Point(
            viewport.Right
                - Scale(MARGIN_HORIZONTAL * MENU_SCALE)
                - Scale(MARGIN_OUTER * MENU_SCALE)
                - Scale(SLOT_SIZE / 2f),
            leftOrigin.Y
        );
        var rightShoulderPosition = rightOrigin.AddX(-Scale(SLOT_DISTANCE * MENU_SCALE));
        if (
            UnassignedButtonsVisible
            || HasSlotSprite(SButton.ControllerX)
            || HasSlotSprite(SButton.ControllerY)
            || HasSlotSprite(SButton.ControllerA)
            || HasSlotSprite(SButton.ControllerB)
        )
        {
            var rightBackgroundRect = GetCircleRect(
                rightOrigin.AddX(-Scale(SLOT_DISTANCE * MENU_SCALE)),
                Scale(BACKGROUND_RADIUS)
            );
            b.Draw(outerBackground, rightBackgroundRect, OuterBackgroundColor * BackgroundOpacity);
            DrawSlot(b, rightOrigin, SButton.ControllerB, PromptPosition.Right);
            DrawSlot(
                b,
                rightOrigin.Add(
                    -Scale(SLOT_DISTANCE * MENU_SCALE),
                    -Scale(SLOT_DISTANCE * MENU_SCALE)
                ),
                SButton.ControllerY,
                PromptPosition.Above
            );
            DrawSlot(
                b,
                rightOrigin.Add(
                    -Scale(SLOT_DISTANCE * MENU_SCALE),
                    Scale(SLOT_DISTANCE * MENU_SCALE)
                ),
                SButton.ControllerA,
                PromptPosition.Below
            );
            DrawSlot(
                b,
                rightOrigin.AddX(-Scale(SLOT_DISTANCE * MENU_SCALE * 2f)),
                SButton.ControllerX,
                PromptPosition.Left
            );
            rightShoulderPosition.Y -= Scale(SLOT_DISTANCE * 2.5f * MENU_SCALE);
        }
        if (enabledSlots.Contains(SButton.RightShoulder))
        {
            DrawSlot(
                b,
                rightShoulderPosition,
                SButton.RightShoulder,
                PromptPosition.Above,
                darken: true
            );
        }
    }

    private void UpdateScale(bool force = false)
    {
        var desiredScale = Math.Clamp(config.Style.QuickActionScale, 0.5f, 1.5f);
        if (
            !force
            && MathF.Abs(desiredScale - quickSlotScale) < 0.001f
            && outerBackground is not null
        )
        {
            return;
        }
        quickSlotScale = desiredScale;
        outerBackground?.Dispose();
        slotBackground?.Dispose();
        outerBackground = ShapeTexture.CreateCircle(
            Scale((SLOT_SIZE + SLOT_SIZE / 2f + MARGIN_OUTER) * MENU_SCALE),
            filled: true,
            graphicsDevice: graphicsDevice
        );
        slotBackground = ShapeTexture.CreateCircle(
            Scale(SLOT_SIZE / 2f),
            filled: true,
            graphicsDevice: graphicsDevice
        );
        isDirty = true;
    }

    private int Scale(float value)
    {
        return (int)MathF.Round(value * quickSlotScale);
    }

    public void FlashDelay(SButton button)
    {
        flashes[button] = new(FlashType.Delay, config.Input.ActivationDelayMs);
    }

    public void FlashError(SButton button)
    {
        flashes[button] = new(FlashType.Error, Animation.ERROR_FLASH_DURATION_MS);
    }

    public bool HasSlotSprite(SButton button)
    {
        return slotSprites.ContainsKey(button);
    }

    public void Invalidate()
    {
        isDirty = true;
    }

    public void Update(TimeSpan elapsed)
    {
        foreach (var (button, flash) in flashes)
        {
            var flashElapsedMs = flash.ElapsedMs + (float)elapsed.TotalMilliseconds;
            if (flashElapsedMs >= flash.DurationMs)
            {
                flashes.Remove(button);
                continue;
            }
            flashes[button] = flash with { ElapsedMs = flashElapsedMs };
        }
    }

    private void DrawSlot(
        SpriteBatch b,
        Point origin,
        SButton button,
        PromptPosition promptPosition,
        bool darken = false
    )
    {
        var isAssigned = slotSprites.TryGetValue(button, out var sprite);
        if (!isAssigned && !UnassignedButtonsVisible)
        {
            return;
        }

        var isEnabled = enabledSlots.Contains(button);
        var backgroundRect = GetCircleRect(origin, Scale(SLOT_SIZE / 2f));
        if (darken)
        {
            var darkenRect = backgroundRect;
            darkenRect.Inflate(4, 4);
            b.Draw(slotBackground, darkenRect, Color.Black * BackgroundOpacity);
        }
        var backgroundColor = GetBackgroundColor(button, isAssigned && isEnabled);
        b.Draw(slotBackground, backgroundRect, backgroundColor * BackgroundOpacity);

        var slotOpacity = isEnabled ? 1f : 0.5f;

        if (isAssigned)
        {
            var spriteRect = GetCircleRect(origin, Scale(IMAGE_SIZE * 1f * MENU_SCALE / 2f));
            if (SlotItems.TryGetValue(button, out var item) && item.Texture is not null)
            {
                ItemRenderer.Draw(
                    b,
                    item,
                    spriteRect,
                    config.Style,
                    opacity: slotOpacity * SpriteOpacity
                );
            }
            else
            {
                b.Draw(
                    sprite!.Texture,
                    spriteRect,
                    sprite.SourceRect,
                    Color.White * slotOpacity * SpriteOpacity
                );
            }
        }

        if (GetPromptSprite(button) is { } promptSprite)
        {
            var promptOffset = Scale(PROMPT_OFFSET);
            var promptOrigin = promptPosition switch
            {
                PromptPosition.Above => origin.AddY(-promptOffset),
                PromptPosition.Below => origin.AddY(promptOffset),
                PromptPosition.Left => origin.AddX(-promptOffset),
                PromptPosition.Right => origin.AddX(promptOffset),
                _ => throw new ArgumentException(
                    $"Invalid prompt position: {promptPosition}",
                    nameof(promptPosition)
                ),
            };
            var promptRect = GetCircleRect(promptOrigin, Scale(PROMPT_SIZE / 2f));
            b.Draw(
                promptSprite.Texture,
                promptRect,
                promptSprite.SourceRect,
                Color.White * slotOpacity * SpriteOpacity
            );
        }
    }

    private Color GetBackgroundColor(SButton button, bool enabled)
    {
        var baseColor = enabled ? innerBackgroundColor : disabledBackgroundColor;
        if (!flashes.TryGetValue(button, out var flash))
        {
            return baseColor;
        }
        var (flashColor, position) = flash.Type switch
        {
            FlashType.Delay => (
                config.Style.HighlightColor,
                Animation.GetDelayFlashPosition(flash.ElapsedMs)
            ),
            FlashType.Error => (Color.Red, Animation.GetErrorFlashPosition(flash.ElapsedMs)),
            _ => (Color.White, 0),
        };
        return Color.Lerp(baseColor, flashColor, position);
    }

    private static Rectangle GetCircleRect(Point center, int radius)
    {
        int length = radius * 2;
        return new(center.X - radius, center.Y - radius, length, length);
    }

    private static Sprite GetIconSprite(IconConfig icon)
    {
        return !string.IsNullOrEmpty(icon.ItemId)
            ? Sprite.ForItemId(icon.ItemId)
            : Sprite.TryLoad(icon.TextureAssetPath, icon.SourceRect)
                ?? Sprite.ForItemId("Error_Invalid");
    }

    private Sprite? GetModItemSprite(string id)
    {
        var itemConfig = config
            .Items.ModMenuPages.SelectMany(items => items)
            .FirstOrDefault(item => item.Id == id);
        return itemConfig?.Icon is { } icon ? GetIconSprite(icon) : null;
    }

    private Sprite? GetPromptSprite(SButton button)
    {
        var (rowIndex, columnIndex) = button switch
        {
            SButton.DPadUp => (1, 0),
            SButton.DPadRight => (1, 1),
            SButton.DPadDown => (1, 2),
            SButton.DPadLeft => (1, 3),
            SButton.ControllerA => (1, 4),
            SButton.ControllerB => (1, 5),
            SButton.ControllerX => (1, 6),
            SButton.ControllerY => (1, 7),
            SButton.LeftTrigger => (2, 0),
            SButton.RightTrigger => (2, 1),
            SButton.LeftShoulder => (2, 2),
            SButton.RightShoulder => (2, 3),
            SButton.ControllerBack => (2, 4),
            SButton.ControllerStart => (2, 5),
            SButton.LeftStick => (2, 6),
            SButton.RightStick => (2, 7),
            _ => (-1, -1),
        };
        if (columnIndex == -1)
        {
            return null;
        }
        return new(uiTexture, new(columnIndex * 16, rowIndex * 16, 16, 16));
    }

    private Sprite? GetSlotSprite(IItemLookup itemLookup)
    {
        if (string.IsNullOrWhiteSpace(itemLookup.Id))
        {
            return null;
        }
        return itemLookup.IdType switch
        {
            ItemIdType.GameItem => Sprite.ForItemId(itemLookup.Id),
            ItemIdType.ModItem => GetModItemSprite(itemLookup.Id),
            _ => null,
        };
    }

    private static Color LumaGray(Color color, float lightness)
    {
        var v = (int)((color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f) * lightness);
        return new(v, v, v);
    }

    private void RefreshSlots()
    {
        Logger.Log(LogCategory.QuickSlots, "Starting refresh of quick slot renderer data.");
        enabledSlots.Clear();
        slotSprites.Clear();
        foreach (var (button, slotConfig) in Slots)
        {
            Logger.Log(LogCategory.QuickSlots, $"Checking slot for {button}...");
            Sprite? sprite = null;
            if (SlotItems.TryGetValue(button, out var item))
            {
                if (item.Texture is not null)
                {
                    Logger.Log(
                        LogCategory.QuickSlots,
                        $"Using configured item sprite for {item.Title} in {button} slot."
                    );
                    sprite = new(item.Texture, item.SourceRectangle ?? item.Texture.Bounds);
                }
                else
                {
                    Logger.Log(
                        LogCategory.QuickSlots,
                        $"Item {item.Title} in {button} slot has no texture; using default sprite."
                    );
                }
                enabledSlots.Add(button);
                Logger.Log(
                    LogCategory.QuickSlots,
                    $"Enabled {button} slot with '{item.Title}'.",
                    LogLevel.Info
                );
            }
            else
            {
                Logger.Log(
                    LogCategory.QuickSlots,
                    $"Disabled unassigned {button} slot.",
                    LogLevel.Info
                );
            }
            sprite ??= GetSlotSprite(slotConfig);
            if (sprite is not null)
            {
                slotSprites.Add(button, sprite);
            }
        }
        isDirty = false;
    }
}

file static class PointExtensions
{
    public static Point Add(this Point point, int x, int y)
    {
        return new(point.X + x, point.Y + y);
    }

    public static Point AddX(this Point point, int x)
    {
        return new(point.X + x, point.Y);
    }

    public static Point AddY(this Point point, int y)
    {
        return new(point.X, point.Y + y);
    }
}
