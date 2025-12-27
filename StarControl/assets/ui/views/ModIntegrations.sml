<lane *context={:~ConfigurationViewModel.Mods} layout="stretch content" orientation="vertical" clip-size="stretch">
    <form-heading title={#Config.ModPriorities.Heading} />
    <lane margin="16, 0, 8, 16"
          vertical-content-alignment="middle">
        <label color="#666" text={#Config.ModPriorities.HelpPrefix} />
        <image layout="32px" margin="0, -8, 0, 0" sprite={:ControllerXPromptSprite} />
        <label color="#666" text={#Config.ModPriorities.HelpSuffix} />
    </lane>
    <lane layout="stretch content"
          margin="16, 0"
          orientation="vertical"
          button-press=|HandleListButton($Button)|>
        <frame *repeat={Priorities}
               layout="stretch content"           
               padding="8, 0"
               vertical-content-alignment="middle"
               background={@Mods/StardewUI/Sprites/White}
               background-tint="#0000"
               border={@Mods/StardewUI/Sprites/MenuSlotTransparent}
               border-thickness="4"
               outer-size={>LayoutSize}
               +state:dragging={Dragging}
               +state:dragging:background-tint="#39d"
               +transition:background-tint="100ms EaseOutSine"
               button-press=|^HandleItemButton(this, $Button)|
               button-repeat=|^HandleItemButton(this, $Button)|>
            <lane vertical-content-alignment="middle">
                <checkbox margin="0, 4"
                          label-text={Name}
                          tooltip={Description}
                          is-checked={<>Enabled}
                          +state:disabled={:Required}
                          +state:disabled:opacity="0.5" />
                <spacer layout="stretch"
                        pointer-style="Hand"
                        draggable="true"
                        drag-start=|^BeginDrag(this)|
                        drag-end=|^EndDrag(this)|
                        drag=|^Drag(this, $Position)|/>
            </lane>
        </frame>
    </lane>
</lane>

<template name="form-heading">
    <banner margin="0, 8, 0, 8" text={&title} />
</template>

<template name="button-prompt">
    <panel *switch={&iconSet} layout="32px" margin="0, -8, 0, 0">
        <panel *case="PlayStation" *switch={&button}>
            <image *case="DPadLeft" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadLeft} />
            <image *case="DPadUp" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadUp} />
            <image *case="DPadRight" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadRight} />
            <image *case="DPadDown" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadDown} />
            <image *case="ControllerA" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadA} />
            <image *case="ControllerB" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadB} />
            <image *case="ControllerX" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadX} />
            <image *case="ControllerY" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadY} />
        </panel>
        <panel *case="Xbox" *switch={&button}>
            <image *case="DPadLeft" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadLeft} />
            <image *case="DPadUp" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadUp} />
            <image *case="DPadRight" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadRight} />
            <image *case="DPadDown" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadDown} />
            <image *case="ControllerA" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadA} />
            <image *case="ControllerB" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadB} />
            <image *case="ControllerX" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadX} />
            <image *case="ControllerY" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadY} />
        </panel>
    </panel>
</template>
