using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Enchantments;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StarControl.Menus;

/// <summary>
/// A menu item that corresponds to an item in the player's inventory.
/// </summary>
internal class InventoryMenuItem : IRadialMenuItem
{
    public string Id => Item.QualifiedItemId;

    /// <summary>
    /// The underlying inventory item.
    /// </summary>
    public Item Item { get; }

    public string Title { get; }

    public string Description { get; }

    public int? StackSize => Item.maximumStackSize() > 1 ? Item.Stack : null;

    public int? Quality => Item.Quality;

    public Texture2D? Texture { get; }

    public Rectangle? SourceRectangle { get; }

    public Rectangle? TintRectangle { get; }

    public Color? TintColor { get; }

    private static readonly ConditionalWeakTable<Item, Texture2D?> DerivedTextures = [];

    public InventoryMenuItem(Item item)
    {
        Item = item;
        Title = item.DisplayName;
        Description = UnparseText(item.getDescription());

        var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
        // Workaround for some mods creating items without registering item data; these mods
        // typically only implement drawInMenu. This is not a good thing to do, and tends to cause
        // other problems, but we can still attempt to show an icon.
        if (
            data.IsErrorItem
            // Exclude vanilla item classes; hacked items only apply to mods, and if we found an
            // instance with a vanilla type, it means the item truly is missing (e.g. source mod is
            // no longer installed, or the item is simply invalid) and we'd only waste time trying
            // to derive a texture.
            && item.GetType().Assembly != typeof(SObject).Assembly
            && GetDerivedTexture(item) is { } texture
        )
        {
            Texture = texture;
            SourceRectangle = texture.Bounds;
        }
        else
        {
            var textureData = GetTextureRedirect(item);
            Texture = textureData?.GetTexture() ?? data.GetTexture();
            SourceRectangle = textureData?.GetSourceRect() ?? data.GetSourceRect();
            (TintRectangle, TintColor) = GetTinting(item, textureData ?? data);
        }
    }

    public ItemActivationResult Activate(
        Farmer who,
        DelayedActions delayedActions,
        ItemActivationType activationType
    )
    {
        // Item Bags: open bag UI from Primary (wheel) and Instant actions.
        if (
            Item is Tool bagTool
            && (
                activationType == ItemActivationType.Primary
                || activationType == ItemActivationType.Instant
            )
            && TryOpenItemBagsMenu(bagTool)
        )
        {
            return ItemActivationResult.Used;
        }

        if (activationType == ItemActivationType.Instant)
        {
            if (Item is Tool tool)
            {
                var isActivating = IsActivating();
                if (!isActivating && (!Context.CanPlayerMove || who.canOnlyWalk || who.UsingTool))
                {
                    return ItemActivationResult.Ignored;
                }
                if (who.CurrentTool != tool)
                {
                    var toolIndex = who.Items.IndexOf(tool);
                    if (toolIndex < 0)
                    {
                        return ItemActivationResult.Ignored;
                    }
                    who.CurrentToolIndex = toolIndex;
                }
                if (tool is FishingRod rod && rod.fishCaught)
                {
                    rod.doneHoldingFish(who);
                    return ItemActivationResult.Used;
                }
                else if (tool is not MeleeWeapon)
                {
                    who.FireTool();
                }
                Game1.pressUseToolButton();
                return isActivating
                    ? ItemActivationResult.Used
                    : ItemActivationResult.ToolUseStarted;
            }
            else if (Item is SObject obj && obj.isPlaceable())
            {
                var previousToolIndex = who.CurrentToolIndex;
                var grabTile = Game1.GetPlacementGrabTile();
                var placementPosition = Utility.GetNearbyValidPlacementPosition(
                    who,
                    who.currentLocation,
                    obj,
                    (int)grabTile.X * 64,
                    (int)grabTile.Y * 64
                );
                try
                {
                    who.CurrentToolIndex = who.Items.IndexOf(obj);
                    return Utility.tryToPlaceItem(
                        who.currentLocation,
                        obj,
                        (int)placementPosition.X,
                        (int)placementPosition.Y
                    )
                        ? ItemActivationResult.Used
                        : ItemActivationResult.Ignored;
                }
                finally
                {
                    who.CurrentToolIndex = previousToolIndex;
                }
            }
        }

        return FuzzyActivation.ConsumeOrSelect(
            who,
            Item,
            delayedActions,
            activationType == ItemActivationType.Secondary
                ? InventoryAction.Select
                : InventoryAction.Use
        );
    }

    public void ContinueActivation()
    {
        var who = Game1.player;
        if (Item is not Tool tool || who.CurrentTool != tool)
        {
            return;
        }
        if (!who.canReleaseTool || who.Stamina < 1 || tool is FishingRod)
        {
            return;
        }
        var maxPowerModifier = tool.hasEnchantmentOfType<ReachingToolEnchantment>() ? 1 : 0;
        var maxPower = tool.UpgradeLevel + maxPowerModifier;
        if (who.toolPower.Value >= maxPower)
        {
            return;
        }
        if (who.toolHold.Value <= 0)
        {
            who.toolHold.Value = (int)(tool.AnimationSpeedModifier * 600);
        }
        else
        {
            who.toolHold.Value -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            if (who.toolHold.Value <= 0)
            {
                who.toolPowerIncrease();
            }
        }
    }

    public bool EndActivation()
    {
        var who = Game1.player;
        if (Item is Tool tool && who.CurrentTool == tool && who.UsingTool && who.canReleaseTool)
        {
            if (Item is not FishingRod)
            {
                who.EndUsingTool();
            }
            return true;
        }
        // This isn't equivalent to vanilla logic, but if we detect that the player is no longer
        // using ANY tool (which is what UsingTool tells us) then any button that the controller is
        // "holding" should be released anyway, so that it can be pressed again.
        if (!who.UsingTool)
        {
            return true;
        }
        return false;
    }

    public string? GetActivationSound(
        Farmer who,
        ItemActivationType activationType,
        string defaultSound
    )
    {
        return Item is Tool && activationType == ItemActivationType.Instant ? null : defaultSound;
    }

    private static Texture2D? GetDerivedTexture(Item item)
    {
        if (!DerivedTextures.TryGetValue(item, out var texture))
        {
            try
            {
                var graphicsDevice = Game1.graphics.GraphicsDevice;
                var spriteBatch = Game1.spriteBatch;
                var previousTargets = graphicsDevice.GetRenderTargets();
                var renderTarget = new RenderTarget2D(graphicsDevice, 64, 64);
                graphicsDevice.SetRenderTarget(renderTarget);
                graphicsDevice.Clear(Color.Transparent);
                spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    rasterizerState: new() { MultiSampleAntiAlias = false },
                    samplerState: SamplerState.PointClamp
                );
                try
                {
                    item.drawInMenu(spriteBatch, Vector2.Zero, 1f);
                    texture = renderTarget;
                    DerivedTextures.Add(item, texture);
                }
                catch
                {
                    renderTarget.Dispose();
                    throw;
                }
                finally
                {
                    spriteBatch.End();
                    graphicsDevice.SetRenderTargets(previousTargets);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(
                    $"Error deriving texture for item '{item.Name}' ({item.QualifiedItemId}): {ex}",
                    LogLevel.Error
                );
            }
        }
        return texture;
    }

    public bool IsActivating()
    {
        return Item is Tool tool && Game1.player.CurrentTool == tool && Game1.player.UsingTool;
    }

    private static ParsedItemData? GetTextureRedirect(Item item)
    {
        return item is SObject obj && item.ItemId == "SmokedFish"
            ? ItemRegistry.GetData(obj.preservedParentSheetIndex.Value)
            : null;
    }

    private static (Rectangle? tintRect, Color? tintColor) GetTinting(
        Item item,
        ParsedItemData data
    )
    {
        if (item is not ColoredObject coloredObject)
        {
            return default;
        }
        if (item.ItemId == "SmokedFish")
        {
            // Smoked fish implementation is unique (and private) in ColoredObject.
            // We don't care about the animation here, but should draw it darkened; the quirky
            // way this is implemented is to draw a tinted version of the original item sprite
            // (not an overlay) sprite over top of the original sprite.
            return (data.GetSourceRect(), new Color(80, 30, 10) * 0.6f);
        }
        return !coloredObject.ColorSameIndexAsParentSheetIndex
            ? (data.GetSourceRect(1), coloredObject.color.Value)
            : (null, coloredObject.color.Value);
    }

    // When we call Item.getDescription(), most implementations go through `Game1.parseText`
    // which splits the string itself onto multiple lines. This tries to remove that, so that we
    // can do our own wrapping using our own width.
    //
    // N.B. The reason we don't just use `ParsedItemData.Description` is that, at least in the
    // current version, it's often only a "base description" and includes format placeholders,
    // or is missing suffixes.
    private static string UnparseText(string text)
    {
        var sb = new StringBuilder();
        var isWhitespace = false;
        var newlineCount = 0;
        foreach (var c in text)
        {
            if (c == ' ' || c == '\r' || c == '\n')
            {
                if (!isWhitespace)
                {
                    sb.Append(' ');
                }
                isWhitespace = true;
                if (c == '\n')
                {
                    newlineCount++;
                }
            }
            else
            {
                // If the original text has a "paragraph", the formatted text will often look
                // strange if that is also collapsed into a space. So preserve _multiple_
                // newlines somewhat as a single "paragraph break".
                if (newlineCount > 1)
                {
                    // From implementation above, newlines are counted as whitespace so we know
                    // that the last character will always be a space when hitting here.
                    sb.Length--;
                    sb.Append("\r\n\r\n");
                }
                sb.Append(c);
                isWhitespace = false;
                newlineCount = 0;
            }
        }
        if (isWhitespace)
        {
            sb.Length--;
        }
        return sb.ToString();
    }

    // Cache reflection lookups (don’t re-scan assemblies every press)
    private static Type? _itemBagsBaseType;
    private static MethodInfo? _openContentsMethod;

    private static bool TryOpenItemBagsMenu(Tool tool)
    {
        try
        {
            _itemBagsBaseType ??= AppDomain
                .CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("ItemBags.Bags.ItemBag", throwOnError: false))
                .FirstOrDefault(t => t is not null);

            if (_itemBagsBaseType is null)
                return false;

            if (!_itemBagsBaseType.IsInstanceOfType(tool))
                return false;

            var player = Game1.player;
            if (player is null || player.CursorSlotItem is not null)
                return false;

            _openContentsMethod ??= _itemBagsBaseType.GetMethod(
                "OpenContents",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (_openContentsMethod is null)
                return false;

            var parameters = _openContentsMethod.GetParameters();

            if (parameters.Length == 0)
            {
                _openContentsMethod.Invoke(tool, null);
                return true;
            }

            if (parameters.Length == 3)
            {
                _openContentsMethod.Invoke(
                    tool,
                    new object?[] { player.Items, player.MaxItems, null }
                );
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
