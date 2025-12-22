using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarControl.Config;
using StarControl.Graphics;
using StarControl.Menus;
using StardewValley.ItemTypeDefinitions;

namespace StarControl.UI;

internal class RadialMenuPreview : IDisposable
{
    public Texture2D? Texture => !renderTarget.IsDisposed ? renderTarget : null;
    public Sprite? Sprite =>
        Texture is not null ? new(Texture, new(0, 0, Texture.Width, Texture.Height)) : null;

    private readonly StyleConfigurationViewModel context;
    private readonly RadialMenuPainter painter;
    private readonly SpriteBatch previewSpriteBatch;
    private readonly PreviewItem[] previewItems =
    [
        new("(T)GoldAxe"),
        new("(T)CopperHoe"),
        new("(T)SteelWateringCan"),
        new("(T)IridiumPickaxe"),
        new("(W)47"),
        new("(O)24", 6),
        new("(O)388", 55),
        new("(O)390", 141),
        new("(W)10"),
    ];
    private readonly RenderTarget2D renderTarget;
    private readonly Styles styles = new();
    private const float PreviewScale = 1.0f;

    public RadialMenuPreview(StyleConfigurationViewModel context, int width, int height)
    {
        this.context = context;
        context.PropertyChanged += Context_PropertyChanged;
        previewSpriteBatch = new(Game1.graphics.GraphicsDevice);
        renderTarget = new(
            Game1.graphics.GraphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );
        painter = new(Game1.graphics.GraphicsDevice, styles)
        {
            Items = previewItems,
            RenderTarget = renderTarget,
            Scale = PreviewScale,
            UseStyleOffsets = false,
        };
        Draw();
    }

    public void Dispose()
    {
        painter.RenderTarget = null;
        renderTarget.Dispose();
        previewSpriteBatch.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Refresh()
    {
        Draw();
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        context.Save(styles);
        painter.Invalidate();
        Draw();
    }

    private void Draw()
    {
        UpdatePreviewLayout();
        painter.Paint(
            previewSpriteBatch,
            selectedIndex: 1,
            focusedIndex: 3,
            selectionAngle: MathHelper.ToRadians(120),
            selectionBlend: 1f
        );
    }

    private void UpdatePreviewLayout()
    {
        painter.VerticalOffset = 0f;
        painter.HorizontalOffset = 0f;
    }

    private class PreviewItem(string itemId, int? stackSize = null) : IRadialMenuItem
    {
        public string Description => data.Description;
        public Rectangle? SourceRectangle => data.GetSourceRect();
        public int? StackSize => stackSize;
        public string Title => data.DisplayName;
        public Texture2D Texture => data.GetTexture();

        private readonly ParsedItemData data = ItemRegistry.GetDataOrErrorItem(itemId);

        public ItemActivationResult Activate(
            Farmer who,
            DelayedActions delayedActions,
            ItemActivationType activationType
        )
        {
            return ItemActivationResult.Ignored;
        }
    }
}
