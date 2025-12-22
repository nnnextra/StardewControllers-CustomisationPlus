using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarControl.Config;
using StarControl.Graphics;

namespace StarControl.Menus;

public class RadialMenuPainter(GraphicsDevice graphicsDevice, Styles styles)
{
    private record SelectionState(int ItemCount, int SelectedIndex, int FocusedIndex);

    private const float CIRCLE_MAX_ERROR = 0.1f;
    private const float EQUILATERAL_ANGLE = MathF.PI * 2 / 3;
    private const float MENU_SPRITE_MAX_WIDTH_RATIO = 0.8f;
    private const float TWO_PI = MathF.PI * 2;

    private static readonly float ROOT_3 = MathF.Sqrt(3);

    private static readonly Sprite UnknownSprite = new(
        Game1.mouseCursors, // Question Mark
        new(176, 425, 9, 12)
    );

    public IReadOnlyList<IRadialMenuItem?> Items { get; set; } = [];
    public RenderTarget2D? RenderTarget { get; set; }
    public float Scale { get; set; } = 1f;
    public float VerticalOffset { get; set; } = 0.3f;
    public float HorizontalOffset { get; set; } = 0f;
    public bool UseStyleOffsets { get; set; } = true;

    private readonly BasicEffect effect = new(graphicsDevice)
    {
        World = Matrix.Identity,
        View = Matrix.CreateLookAt(Vector3.Forward, Vector3.Zero, Vector3.Down),
        VertexColorEnabled = true,
    };

    private VertexPositionColor[] innerVertices = [];
    private VertexPositionColor[] outerVertices = [];
    private RenderTarget2D? textRenderTarget;
    private float previousScale = 1f;
    private float selectionBlend = 1.0f;
    private SelectionState selectionState = new(ItemCount: 0, SelectedIndex: 0, FocusedIndex: 0);

    public void Invalidate()
    {
        innerVertices = [];
        outerVertices = [];
    }

    public void Paint(
        SpriteBatch spriteBatch,
        int selectedIndex,
        int focusedIndex,
        float? selectionAngle = null,
        float selectionBlend = 1.0f,
        Rectangle? viewport = null
    )
    {
        if (UseStyleOffsets)
        {
            VerticalOffset = styles.MenuVerticalOffset;
            HorizontalOffset = styles.MenuHorizontalOffset;
        }
        var horizontalOffset = HorizontalOffset;
        if (Scale <= 0)
        {
            return;
        }
        if (Scale != previousScale)
        {
            Invalidate();
            previousScale = Scale;
        }
        var hasNewVertices = GenerateVertices();
        var selectionState = new SelectionState(Items.Count, selectedIndex, focusedIndex);
        if (
            hasNewVertices
            || selectionState != this.selectionState
            || selectionBlend != this.selectionBlend
        )
        {
            this.selectionState = selectionState;
            this.selectionBlend = selectionBlend;
            UpdateVertexColors();
        }
        viewport ??= RenderTarget?.Bounds ?? Viewports.DefaultViewport;
        var usesRenderTarget = RenderTarget is not null;
        RenderTargetBinding[]? previousTargets = null;
        if (usesRenderTarget)
        {
            previousTargets = graphicsDevice.GetRenderTargets();
            graphicsDevice.SetRenderTarget(RenderTarget);
            graphicsDevice.Clear(Color.Transparent);
        }
        else
        {
            spriteBatch.End();
        }
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            rasterizerState: new() { MultiSampleAntiAlias = false },
            samplerState: SamplerState.PointClamp
        );
        try
        {
            PaintBackgrounds(viewport.Value, selectionAngle, horizontalOffset);
            PaintItems(spriteBatch, viewport.Value, horizontalOffset);
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                rasterizerState: new() { MultiSampleAntiAlias = false },
                samplerState: SamplerState.PointClamp
            );
            PaintSelectionDetails(spriteBatch, viewport.Value, horizontalOffset);
        }
        finally
        {
            if (previousTargets is not null)
            {
                spriteBatch.End();
                graphicsDevice.SetRenderTargets(previousTargets);
            }
        }
    }

    private void PaintBackgrounds(Rectangle viewport, float? selectionAngle, float horizontalOffset)
    {
        effect.World = Matrix.CreateTranslation(
            viewport.X + viewport.Width * horizontalOffset,
            viewport.Y + viewport.Height * VerticalOffset,
            0
        );
        effect.Projection = Matrix.CreateOrthographic(viewport.Width, viewport.Height, 0, 1);
        // Cursor is just 1 triangle, so we can compute this on every frame.
        var cursorVertices =
            selectionAngle != null
                ? GenerateCursorVertices(
                    (styles.InnerRadius - styles.CursorDistance) * Scale,
                    selectionAngle.Value
                )
                : [];
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.TriangleList,
                innerVertices,
                0,
                innerVertices.Length / 3
            );
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.TriangleList,
                outerVertices,
                0,
                outerVertices.Length / 3
            );
            if (cursorVertices.Length > 0)
            {
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    cursorVertices,
                    0,
                    cursorVertices.Length / 3
                );
            }
        }
    }

    private void PaintItems(SpriteBatch spriteBatch, Rectangle viewport, float horizontalOffset)
    {
        var centerX = viewport.X + viewport.Width / 2.0f + viewport.Width * horizontalOffset;
        var centerY = viewport.Y + viewport.Height / 2.0f + viewport.Height * VerticalOffset;
        var itemRadius = (styles.InnerRadius + styles.GapWidth + styles.OuterRadius / 2.0f) * Scale;
        var angleBetweenItems = TWO_PI / Items.Count;
        var currentAngle = 0.0f;
        foreach (var item in Items)
        {
            if (item is null)
            {
                currentAngle += angleBetweenItems;
                continue;
            }
            var itemPoint = GetCirclePoint(itemRadius, currentAngle);
            var displaySize = GetScaledSize(item, styles.MenuSpriteHeight * Scale);
            // Aspect ratio is usually almost square, or has extra height (e.g. big craftables).
            // In case of a horizontal aspect ratio, shrink the size so that it still fits.
            var maxWidth = styles.OuterRadius * MENU_SPRITE_MAX_WIDTH_RATIO * Scale;
            if (displaySize.X > maxWidth)
            {
                var itemScale = maxWidth / displaySize.X;
                displaySize = new(
                    (int)MathF.Round(displaySize.X * itemScale),
                    (int)MathF.Round(displaySize.Y * itemScale)
                );
            }
            GetSpriteSize(item, out var isMonogram);
            var opacity = item.Enabled ? 1 : 0.5f;
            // Sprites draw from top left rather than center when using destination rectangle; we
            // have to adjust for it.
            var itemPoint2d = new Vector2(
                centerX + itemPoint.X - displaySize.X / 2.0f,
                centerY + itemPoint.Y - displaySize.Y / 2.0f
            );
            var destinationRect = new Rectangle(itemPoint2d.ToPoint(), displaySize);
            ItemRenderer.Draw(
                spriteBatch,
                item,
                destinationRect,
                styles,
                isMonogram,
                Scale,
                opacity
            );
            currentAngle += angleBetweenItems;
        }
    }

    private void PaintSelectionDetails(
        SpriteBatch spriteBatch,
        Rectangle viewport,
        float horizontalOffset
    )
    {
        if (selectionState.FocusedIndex < 0)
        {
            return;
        }
        var item =
            Items.Count > selectionState.FocusedIndex ? Items[selectionState.FocusedIndex] : null;
        if (item is null)
        {
            return;
        }

        var centerX = viewport.X + viewport.Width / 2.0f + viewport.Width * horizontalOffset;
        var centerY = viewport.Y + viewport.Height / 2.0f + viewport.Height * VerticalOffset;
        var opacity = item.Enabled ? 1 : 0.5f;
        if (styles.ShowSelectionIcon && item.Texture is not null)
        {
            // Make icon 50% larger (height * 1.5)
            var itemDrawSize = GetScaledSize(item, styles.SelectionSpriteHeight * Scale * 1.5f);
            var centeredX = MathF.Round(centerX);
            var itemPos = new Vector2(
                centeredX - itemDrawSize.X / 2f,
                centerY - itemDrawSize.Y - 12
            );
            var itemRect = new Rectangle(itemPos.ToPoint(), itemDrawSize);
            var baseColor = item.TintRectangle is null
                ? (item.TintColor ?? Color.White)
                : Color.White;
            spriteBatch.Draw(item.Texture, itemRect, item.SourceRectangle, baseColor * opacity);
            if (item.TintRectangle is Rectangle tintRect && item.TintColor is Color tintColor)
            {
                spriteBatch.Draw(item.Texture, itemRect, tintRect, tintColor * opacity);
            }
        }

        var centeredTextX = centerX + 3f;
        var titleY = centerY;
        var innerDiameter = styles.InnerRadius * Scale * 2f;
        if (styles.ShowSelectionTitle)
        {
            var titleScale = MathF.Max(0.5f, styles.SelectionTitleScale);
            var titleMaxLines = titleScale < 1f ? 3 : 2;
            var titleMaxWidth = (int)MathF.Max(1f, (innerDiameter - 48f) / titleScale);
            var wrappedTitle = Game1
                .parseText(item.Title, Game1.smallFont, titleMaxWidth)
                .Split(Environment.NewLine);
            if (wrappedTitle.Length > titleMaxLines)
            {
                wrappedTitle = wrappedTitle[..titleMaxLines];
            }
            titleY += DrawTextBlock(
                spriteBatch,
                Game1.smallFont,
                wrappedTitle,
                styles.SelectionTitleColor * opacity,
                centeredTextX,
                titleY,
                titleScale
            );
        }

        if (styles.ShowSelectionDescription)
        {
            var descriptionText = item.Description;
            var descriptionScale = MathF.Max(0.5f, styles.SelectionDescriptionScale);
            var descriptionY = titleY + 16.0f * descriptionScale;
            var descriptionMaxWidth = (int)MathF.Max(1f, (innerDiameter - 32f) / descriptionScale);
            var descriptionLines = Game1
                .parseText(descriptionText, Game1.smallFont, descriptionMaxWidth)
                .Split(Environment.NewLine);
            DrawTextBlock(
                spriteBatch,
                Game1.smallFont,
                descriptionLines,
                styles.SelectionDescriptionColor * opacity,
                centeredTextX,
                descriptionY,
                descriptionScale
            );
        }
    }

    private float DrawTextBlock(
        SpriteBatch spriteBatch,
        SpriteFont font,
        string[] lines,
        Color color,
        float centerX,
        float startY,
        float scale
    )
    {
        if (lines.Length == 0)
        {
            return 0f;
        }

        var maxLineWidth = lines.Max(line => font.MeasureString(line).X);
        var lineHeight = font.LineSpacing * scale;
        if (scale >= 1f)
        {
            foreach (var line in lines)
            {
                var lineWidth = font.MeasureString(line).X;
                var linePos = new Vector2(
                    MathF.Round(
                        centerX
                            - maxLineWidth * scale / 2f
                            + (maxLineWidth - lineWidth) * scale / 2f
                    ),
                    MathF.Round(startY)
                );
                spriteBatch.DrawString(
                    font,
                    line,
                    linePos,
                    color,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
                startY += lineHeight;
            }
            return lineHeight * lines.Length;
        }

        const int padding = 4;
        var textWidth = Math.Max(1, (int)MathF.Ceiling(maxLineWidth) + 4);
        var textHeight = Math.Max(1, (int)MathF.Ceiling(font.LineSpacing * lines.Length) + 4);
        var targetWidth = textWidth + padding * 2;
        var targetHeight = textHeight + padding * 2;
        var target = EnsureTextRenderTarget(targetWidth, targetHeight);

        spriteBatch.End();
        var previousViewport = graphicsDevice.Viewport;
        var previousTargets = graphicsDevice.GetRenderTargets();
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            rasterizerState: new() { MultiSampleAntiAlias = false },
            samplerState: SamplerState.PointClamp
        );
        var textY = (float)padding;
        foreach (var line in lines)
        {
            var lineWidth = font.MeasureString(line).X;
            var linePos = new Vector2(
                padding + MathF.Round((maxLineWidth - lineWidth) / 2f),
                MathF.Round(textY)
            );
            spriteBatch.DrawString(font, line, linePos, color);
            textY += font.LineSpacing;
        }
        spriteBatch.End();
        graphicsDevice.SetRenderTargets(previousTargets);
        graphicsDevice.Viewport = previousViewport;
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            rasterizerState: new() { MultiSampleAntiAlias = false },
            samplerState: SamplerState.PointClamp
        );

        var destWidth = MathF.Round(textWidth * scale);
        var destHeight = MathF.Round(textHeight * scale);
        var destPos = new Vector2(MathF.Round(centerX - destWidth / 2f), MathF.Round(startY));
        spriteBatch.Draw(
            target,
            new Rectangle(
                (int)destPos.X,
                (int)destPos.Y,
                Math.Max(1, (int)destWidth),
                Math.Max(1, (int)destHeight)
            ),
            new Rectangle(padding, padding, textWidth, textHeight),
            Color.White
        );
        return destHeight;
    }

    private RenderTarget2D EnsureTextRenderTarget(int width, int height)
    {
        if (
            textRenderTarget is not null
            && textRenderTarget.Width == width
            && textRenderTarget.Height == height
        )
        {
            return textRenderTarget;
        }
        textRenderTarget?.Dispose();
        textRenderTarget = new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );
        return textRenderTarget;
    }

    private bool GenerateVertices()
    {
        var wasGenerated = false;
        if (innerVertices.Length == 0)
        {
            innerVertices = GenerateCircleVertices(
                styles.InnerRadius * Scale,
                styles.InnerBackgroundColor
            );
            wasGenerated = true;
        }
        if (outerVertices.Length == 0)
        {
            outerVertices = GenerateDonutVertices(
                (styles.InnerRadius + styles.GapWidth) * Scale,
                styles.OuterRadius * Scale,
                styles.OuterBackgroundColor
            );
            wasGenerated = true;
        }
        return wasGenerated;
    }

    private static (float start, float end) GetSegmentRange(
        int focusedIndex,
        int itemCount,
        int segmentCount
    )
    {
        if (focusedIndex < 0)
        {
            return (-1.0f, -0.5f);
        }
        var sliceSize = (float)segmentCount / itemCount;
        var relativePosition = (float)focusedIndex / itemCount;
        var end = (relativePosition * segmentCount + sliceSize / 2) % segmentCount;
        var start = (end - sliceSize + segmentCount) % segmentCount;
        return (start, end);
    }

    private void UpdateVertexColors()
    {
        var (itemCount, selectedIndex, focusedIndex) = selectionState;
        const int outerChordSize = 6;
        var segmentCount = outerVertices.Length / outerChordSize;
        var (selectionHighlightStartSegment, selectionHighlightEndSegment) = GetSegmentRange(
            selectedIndex,
            itemCount,
            segmentCount
        );
        var (focusHighlightStartSegment, focusHighlightEndSegment) = GetSegmentRange(
            focusedIndex,
            itemCount,
            segmentCount
        );
        var isFocusedItemEnabled =
            focusedIndex >= 0 && focusedIndex < Items.Count && Items[focusedIndex]?.Enabled == true;
        var highlightColor = isFocusedItemEnabled
            ? styles.HighlightColor
            : AlphaBlend(styles.OuterBackgroundColor, styles.HighlightColor, 0.25f);
        for (var i = 0; i < segmentCount; i++)
        {
            var isFocusHighlight =
                focusHighlightStartSegment < focusHighlightEndSegment
                    ? (i >= focusHighlightStartSegment && i < focusHighlightEndSegment)
                    : (i >= focusHighlightStartSegment || i < focusHighlightEndSegment);
            var isSelectionHighlight =
                !isFocusHighlight
                && (
                    selectionHighlightStartSegment < selectionHighlightEndSegment
                        ? (i >= selectionHighlightStartSegment && i < selectionHighlightEndSegment)
                        : (i >= selectionHighlightStartSegment || i < selectionHighlightEndSegment)
                );
            var outerIndex = i * outerChordSize;
            var outerColor =
                isFocusHighlight
                    ? Color.Lerp(styles.OuterBackgroundColor, highlightColor, selectionBlend)
                : isSelectionHighlight ? styles.SelectionColor
                : styles.OuterBackgroundColor;
            for (var j = 0; j < outerChordSize; j++)
            {
                outerVertices[outerIndex + j].Color = outerColor;
            }
        }
    }

    private static Color AlphaBlend(Color color1, Color color2, float a)
    {
        var c1 = color1 * (1 - a);
        var c2 = color2 * a;
        return new(c1.R + c2.R, c1.G + c2.G, c1.B + c2.B, color1.A);
    }

    private static VertexPositionColor[] GenerateCircleVertices(float radius, Color color)
    {
        var vertexCount = GetOptimalVertexCount(radius);
        var step = TWO_PI / vertexCount;
        var t = 0.0f;
        var vertices = new VertexPositionColor[vertexCount * 3];
        var vertexIndex = 0;
        var prevPoint = GetCirclePoint(radius, 0);
        // Note: We loop using a fixed number of vertices, instead of a max angle, in case of
        // floating point rounding error.
        for (var i = 0; i < vertexCount; i++)
        {
            t += step;
            var nextPoint = GetCirclePoint(radius, t);
            vertices[vertexIndex++] = new(prevPoint, color);
            vertices[vertexIndex++] = new(nextPoint, color);
            vertices[vertexIndex++] = new(Vector3.Zero, color);
            prevPoint = nextPoint;
        }
        return vertices;
    }

    private static VertexPositionColor[] GenerateDonutVertices(
        float innerRadius,
        float thickness,
        Color color
    )
    {
        var outerRadius = innerRadius + thickness;
        var vertexCount = GetOptimalVertexCount(outerRadius);
        var step = TWO_PI / vertexCount;
        var t = 0.0f;
        var vertices = new VertexPositionColor[vertexCount * 6];
        var vertexIndex = 0;
        var prevInnerPoint = GetCirclePoint(innerRadius, 0);
        var prevOuterPoint = GetCirclePoint(outerRadius, 0);
        // Note: We loop using a fixed number of vertices, instead of a max angle, in case of
        // floating point rounding error.
        for (var i = 0; i < vertexCount; i++)
        {
            t += step;
            var nextInnerPoint = GetCirclePoint(innerRadius, t);
            var nextOuterPoint = GetCirclePoint(outerRadius, t);
            vertices[vertexIndex++] = new(prevOuterPoint, color);
            vertices[vertexIndex++] = new(nextOuterPoint, color);
            vertices[vertexIndex++] = new(nextInnerPoint, color);
            vertices[vertexIndex++] = new(nextInnerPoint, color);
            vertices[vertexIndex++] = new(prevInnerPoint, color);
            vertices[vertexIndex++] = new(prevOuterPoint, color);
            prevInnerPoint = nextInnerPoint;
            prevOuterPoint = nextOuterPoint;
        }
        return vertices;
    }

    private VertexPositionColor[] GenerateCursorVertices(float tipRadius, float angle)
    {
        var center = GetCirclePoint(tipRadius - styles.CursorSize / 2, angle);
        // Apply vertical offset to match menu shift
        center.Y += 0; // or a small tweak like 2-4 pixels
        // Compute the points for an origin-centered triangle, then offset.
        var radius = styles.CursorSize / ROOT_3;
        var p1 = center + radius * new Vector3(MathF.Sin(angle), -MathF.Cos(angle), 0);
        var angle2 = angle + EQUILATERAL_ANGLE;
        var p2 = center + radius * new Vector3(MathF.Sin(angle2), -MathF.Cos(angle2), 0);
        var angle3 = angle2 + EQUILATERAL_ANGLE;
        var p3 = center + radius * new Vector3(MathF.Sin(angle3), -MathF.Cos(angle3), 0);
        return
        [
            new(p1, styles.CursorColor),
            new(p2, styles.CursorColor),
            new(p3, styles.CursorColor),
        ];
    }

    private static Vector3 GetCirclePoint(float radius, float angle)
    {
        var x = radius * MathF.Sin(angle);
        var y = radius * -MathF.Cos(angle);
        return new Vector3(x, y, 0);
    }

    private static int GetOptimalVertexCount(float radius)
    {
        var optimalAngle = Math.Acos(1 - CIRCLE_MAX_ERROR / radius);
        return Math.Max((int)Math.Ceiling(TWO_PI / optimalAngle), 8);
    }

    private static Point GetScaledSize(IRadialMenuItem item, float height)
    {
        var sourceSize = GetSpriteSize(item, out _);
        var aspectRatio = (float)sourceSize.X / sourceSize.Y;
        var width = (int)MathF.Round(height * aspectRatio);
        return new(width, (int)MathF.Round(height));
    }

    private static Point GetSpriteSize(IRadialMenuItem item, out bool isMonogram)
    {
        if (item.Texture is null)
        {
            var monogramSize = Monogram.Measure(item.Title)?.ToPoint();
            isMonogram = monogramSize.HasValue;
            return monogramSize ?? UnknownSprite.SourceRect.Size;
        }
        isMonogram = false;
        return item.SourceRectangle?.Size ?? new Point(item.Texture.Width, item.Texture.Height);
    }
}
