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

    private readonly BasicEffect effect = new(graphicsDevice)
    {
        World = Matrix.Identity,
        View = Matrix.CreateLookAt(Vector3.Forward, Vector3.Zero, Vector3.Down),
        VertexColorEnabled = true,
    };

    private VertexPositionColor[] innerVertices = [];
    private VertexPositionColor[] outerVertices = [];
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
        RenderTargetBinding[]? previousTargets = null;
        if (RenderTarget is not null)
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
            PaintBackgrounds(viewport.Value, selectionAngle);
            PaintItems(spriteBatch, viewport.Value);
            PaintSelectionDetails(spriteBatch, viewport.Value);
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

    private void PaintBackgrounds(Rectangle viewport, float? selectionAngle)
    {
        effect.World = Matrix.CreateTranslation(
            viewport.X,
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

    private void PaintItems(SpriteBatch spriteBatch, Rectangle viewport)
    {
        var centerX = viewport.X + viewport.Width / 2.0f;
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

    private void PaintSelectionDetails(SpriteBatch spriteBatch, Rectangle viewport)
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

        var centerX = viewport.X + viewport.Width / 2.0f;
        var centerY = viewport.Y + viewport.Height / 2.0f + viewport.Height * VerticalOffset;
        var opacity = item.Enabled ? 1 : 0.5f;
        if (item.Texture is not null)
        {
            // Make icon 50% larger (height * 1.5)
            var itemDrawSize = GetScaledSize(item, styles.SelectionSpriteHeight * Scale * 1.5f);
            var itemPos = new Vector2(centerX - itemDrawSize.X / 2, centerY - itemDrawSize.Y - 12); // nudge down 12 pixels
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

        var labelFont = Game1.dialogueFont;
        var titleScale = Scale * 0.6f;

        // Wrap title text to a max width
        var wrappedTitle = Game1.parseText(item.Title, labelFont, 260).Split(Environment.NewLine);

        // Limit to 2 lines max
        if (wrappedTitle.Length > 2)
        {
            wrappedTitle = wrappedTitle[..2];
        }

        var titleY = centerY;

        // Find the width of the longest line
        var maxLineWidth = wrappedTitle.Max(line => labelFont.MeasureString(line).X) * titleScale;

        foreach (var line in wrappedTitle)
        {
            var lineSize = labelFont.MeasureString(line) * titleScale;
            // Center relative to max line width
            var linePos = new Vector2(
                centerX - maxLineWidth / 2f + (maxLineWidth - lineSize.X) / 2f,
                titleY
            );

            spriteBatch.DrawString(
                labelFont,
                line,
                linePos,
                styles.SelectionTitleColor * opacity,
                0f,
                Vector2.Zero,
                titleScale,
                SpriteEffects.None,
                0f
            );

            titleY += labelFont.LineSpacing * titleScale;
        }

        var descriptionFont = Game1.smallFont;
        var descriptionText = item.Description;
        // Calculate descriptionY based on the last title line's Y position
        var descriptionY = titleY + 16.0f * Scale * 0.5f;
        var descriptionLines = Game1
            .parseText(descriptionText, descriptionFont, 400)
            .Split(Environment.NewLine);
        foreach (var descriptionLine in descriptionLines)
        {
            // Scale down description to 40%
            var descriptionSize = descriptionFont.MeasureString(descriptionLine) * Scale * 0.4f;
            var descriptionPos = new Vector2(centerX - descriptionSize.X / 2.0f, descriptionY);
            descriptionY += descriptionFont.LineSpacing * Scale * 0.4f;
            spriteBatch.DrawString(
                descriptionFont,
                descriptionLine,
                descriptionPos,
                styles.SelectionDescriptionColor * opacity,
                rotation: 0,
                origin: Vector2.Zero,
                scale: Scale * 0.4f,
                effects: SpriteEffects.None,
                layerDepth: 0
            );
        }
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
