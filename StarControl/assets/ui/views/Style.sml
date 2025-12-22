<lane *context={:~ConfigurationViewModel.Style} layout="stretch content" orientation="vertical">
    <form-heading title={#Config.Layout.Heading} />
    <form-row title={#Config.Layout.InnerRadius.Title} description={#Config.Layout.InnerRadius.Description}>
        <slider track-width="300"
                min="50"
                max="400"
                interval="10"
                value={<>InnerRadius}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title={#Config.Layout.InnerImageSize.Title} description={#Config.Layout.InnerImageSize.Description}>
        <slider track-width="300"
                min="32"
                max="256"
                interval="16"
                value={<>SelectionSpriteHeight}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title={#Config.Layout.OuterRadius.Title} description={#Config.Layout.OuterRadius.Description}>
        <slider track-width="300"
                min="80"
                max="240"
                interval="10"
                value={<>OuterRadius}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title={#Config.Layout.OuterImageSize.Title} description={#Config.Layout.OuterImageSize.Description}>
        <slider track-width="300"
                min="32"
                max="160"
                interval="16"
                value={<>MenuSpriteHeight}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title={#Config.Layout.OuterDistance.Title} description={#Config.Layout.OuterDistance.Description}>
        <slider track-width="300"
                min="0"
                max="20"
                interval="1"
                value={<>GapWidth}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title={#Config.Layout.CursorSize.Title} description={#Config.Layout.CursorSize.Description}>
        <slider track-width="300"
                min="8"
                max="64"
                interval="4"
                value={<>CursorSize}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title={#Config.Layout.CursorDistance.Title} description={#Config.Layout.CursorDistance.Description}>
        <slider track-width="300"
                min="0"
                max="20"
                interval="1"
                value={<>CursorDistance}
                value-format={:FormatPixels} />
    </form-row>
    <form-row title="Item Name Size"
              description="Scales the item name in the radial menu preview.">
        <slider track-width="300"
                min="0.5"
                max="3"
                interval="0.25"
                value={<>SelectionTitleScale}
                value-format={:FormatScale} />
    </form-row>
    <form-row title="Description Size"
              description="Scales the item description text in the radial menu preview.">
        <slider track-width="300"
                min="0.5"
                max="3"
                interval="0.25"
                value={<>SelectionDescriptionScale}
                value-format={:FormatScale} />
    </form-row>
    <form-row title="Show Item Icon"
              description="Shows the item icon in the radial menu preview.">
        <checkbox is-checked={<>ShowSelectionIcon} />
    </form-row>
    <form-row title="Show Item Name"
              description="Shows the item name in the radial menu preview.">
        <checkbox is-checked={<>ShowSelectionTitle} />
    </form-row>
    <form-row title="Show Item Description"
              description="Shows the item description in the radial menu preview.">
        <checkbox is-checked={<>ShowSelectionDescription} />
    </form-row>
    <form-row title="Vertical position"
              description="Moves the radial menu up or down on screen.">
        <slider track-width="300"
                min="-0.4"
                max="0.5"
                interval="0.02"
                value={<>MenuVerticalOffset}
                value-format={:FormatPercent} />
    </form-row>
    <form-row title="Horizontal position"
              description="Moves the radial menu left or right on screen.">
        <slider track-width="300"
                min="-0.5"
                max="0.5"
                interval="0.02"
                value={<>MenuHorizontalOffset}
                value-format={:FormatPercent} />
    </form-row>
    <form-row title="Show Quick Actions"
              description="Shows the quick action menus while a radial menu is open.">
        <checkbox is-checked={<>ShowQuickActions} />
    </form-row>
    <form-row title="Quick Actions Size"
              description="Scales the quick action menus.">
        <slider track-width="300"
                min="0.5"
                max="1.5"
                interval="0.1"
                value={<>QuickActionScale}
                value-format={:FormatScale} />
    </form-row>

    <form-heading title={#Config.Palette.Heading} />
    <color-row title={#Config.Palette.InnerBackground.Title} description={#Config.Palette.InnerBackground.Description}>
        <color-picker layout="300px content" color={<>InnerBackgroundColor} />
    </color-row>
    <color-row title={#Config.Palette.OuterBackground.Title} description={#Config.Palette.OuterBackground.Description}>
        <color-picker layout="300px content" color={<>OuterBackgroundColor} />
    </color-row>
    <color-row title={#Config.Palette.ActiveBackground.Title} description={#Config.Palette.ActiveBackground.Description}>
        <color-picker layout="300px content" color={<>SelectionColor} />
    </color-row>
    <color-row title={#Config.Palette.FocusedBackground.Title} description={#Config.Palette.FocusedBackground.Description}>
        <color-picker layout="300px content" color={<>HighlightColor} />
    </color-row>
    <color-row title={#Config.Palette.Cursor.Title} description={#Config.Palette.Cursor.Description}>
        <color-picker layout="300px content" color={<>CursorColor} />
    </color-row>
    <color-row title={#Config.Palette.ItemName.Title} description={#Config.Palette.ItemName.Description}>
        <color-picker layout="300px content" color={<>SelectionTitleColor} />
    </color-row>
    <color-row title={#Config.Palette.ItemDetails.Title} description={#Config.Palette.ItemDetails.Description}>
        <color-picker layout="300px content" color={<>SelectionDescriptionColor} />
    </color-row>
    <color-row title={#Config.Palette.ItemCount.Title} description={#Config.Palette.ItemCount.Description}>
        <color-picker layout="300px content" color={<>StackSizeColor} />
    </color-row>
</lane>

<template name="form-heading">
    <banner margin="0, 8, 0, 0" text={&title} />
</template>

<template name="form-row">
    <form-row-with-margin margin="16, 4, 0, 4" title={&title} description={&description}>
        <outlet />
    </form-row-with-margin>
</template>

<template name="color-row">
    <form-row-with-margin margin="16, 3, 0, 3" title={&title} description={&description}>
        <outlet />
    </form-row-with-margin>
</template>

<template name="form-row-with-margin">
    <lane margin={&margin} vertical-content-alignment="middle">
        <frame layout="350px content">
            <label text={&title} tooltip={&description} />
        </frame>
        <frame tooltip={&description}>
            <outlet />
        </frame>
    </lane>
</template>
