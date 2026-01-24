using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StarControl.Graphics;

/// <summary>
/// Reference to a sprite, consisting of the source texture and the included region.
/// </summary>
/// <param name="Texture">The texture containing the sprite.</param>
/// <param name="SourceRect">The region of the <paramref name="Texture"/> to display.</param>
public record Sprite(Texture2D Texture, Rectangle SourceRect)
{
    /// <summary>
    /// Loads the sprite for an in-game item.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>The sprite for the item with the specified <paramref name="id"/>.</returns>
    public static Sprite ForItemId(string id)
    {
        var data = ItemRegistry.GetDataOrErrorItem(id);
        return new(data.GetTexture(), data.GetSourceRect());
    }

    public static Sprite FromItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // 1) Prefer the official item registry sprite (works for all vanilla + most mod items that register properly)
        var qualifiedId = item.QualifiedItemId;
        if (!string.IsNullOrEmpty(qualifiedId))
        {
            try
            {
                var data = ItemRegistry.GetData(qualifiedId);
                if (data is not null)
                {
                    return new(data.GetTexture(), data.GetSourceRect());
                }
            }
            catch
            {
                // swallow and fall back below
            }
        }

        // 2) Fallback: render the item into a small texture (works even for "weird" items like Item Bags)
        var derivedTexture = TryRenderItemToTexture(item);
        if (derivedTexture is not null)
        {
            return new(derivedTexture, derivedTexture.Bounds);
        }

        // 3) Final fallback
        return Sprites.Error();
    }

    private static Texture2D? TryRenderItemToTexture(Item item)
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
                // drawInMenu is the most compatible "just draw whatever this item is" API
                item.drawInMenu(spriteBatch, Vector2.Zero, 1f);
            }
            finally
            {
                spriteBatch.End();
                graphicsDevice.SetRenderTargets(previousTargets);
            }

            return renderTarget;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to load a sprite from configuration data.
    /// </summary>
    /// <param name="assetPath">The name of the asset, e.g. <c>LooseSprites/Cursors</c>.</param>
    /// <param name="sourceRect">The <see cref="SourceRect"/> to use if successful.</param>
    /// <returns>A <see cref="Sprite"/> instance referencing the <see cref="Texture2D"/>
    /// corresponding to the <paramref name="assetPath"/> and having the specified
    /// <paramref name="sourceRect"/>, if the texture was successfully loaded; otherwise
    /// <c>null</c>.</returns>
    public static Sprite? TryLoad(string assetPath, Rectangle sourceRect)
    {
        return TryLoad(assetPath, sourceRect, out var sprite) ? sprite : null;
    }

    /// <summary>
    /// Attempts to load a sprite from configuration data.
    /// </summary>
    /// <param name="assetPath">The name of the asset, e.g. <c>LooseSprites/Cursors</c>.</param>
    /// <param name="sourceRect">The <see cref="SourceRect"/> to use if successful.</param>
    /// <param name="sprite">A <see cref="Sprite"/> instance referencing the <see cref="Texture2D"/>
    /// corresponding to the <paramref name="assetPath"/> and having the specified
    /// <paramref name="sourceRect"/>, if the texture was successfully loaded; otherwise
    /// <c>null</c>.</param>
    /// <returns><c>true</c> if the sprite was successfully loaded, otherwise <c>false</c>.</returns>
    public static bool TryLoad(
        string assetPath,
        Rectangle sourceRect,
        [MaybeNullWhen(false)] out Sprite sprite
    )
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            sprite = null;
            return false;
        }
        try
        {
            var texture = Game1.content.Load<Texture2D>(assetPath);
            sprite = new(texture, sourceRect);
            return true;
        }
        catch (ContentLoadException)
        {
            sprite = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse the string representation of a rectangular region.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="rect">The parsed <see cref="Rectangle"/>, if successful; otherwise a default
    /// value with all dimensions set to zero.</param>
    /// <returns><c>true</c> if the parsing was successful, otherwise <c>false</c>.</returns>
    public static bool TryParseRectangle(string value, out Rectangle rect)
    {
        rect = default;
        var coords = value.Split(',');
        if (coords.Length != 4)
        {
            return false;
        }
        if (
            int.TryParse(coords[0], out int x)
            && int.TryParse(coords[1], out int y)
            && int.TryParse(coords[2], out int width)
            && int.TryParse(coords[3], out int height)
        )
        {
            rect = new(x, y, width, height);
            return true;
        }
        return false;
    }
}
