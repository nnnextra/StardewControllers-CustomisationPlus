<panel layout={:MenuLayout}>
<lane layout="stretch stretch"
      orientation="vertical"
      horizontal-content-alignment="middle"
      vertical-content-alignment="start">
    <frame padding="12"
           background={@Mods/StardewUI/Sprites/ControlBorderUncolored}
           background-tint="#8cd">
        <lane>
            <remap-slot *repeat={:Slots} />
        </lane>
    </frame>
    <frame layout="stretch stretch"
           margin="0, 4"
           padding="12"
           background={@Mods/StardewUI/Sprites/ControlBorder}>
        <scrollable peeking="64"
                    scrollbar-margin="-32, 0, 0, 0"
                    scrollbar-visibility="Visible">
            <panel layout="stretch content"
                   margin="0, 0, 32, 0">
                <lane layout="stretch content" margin="0, 0, 0, -16" orientation="vertical">
                    <item-group *repeat={:ItemGroups} />
                </lane>
            </panel>
        </scrollable>
    </frame>
</lane>
<panel *float="below; 0, -12"
       layout="stretch content"
       horizontal-content-alignment="middle">
    <frame *if={ShowAssignTip}
           padding="16, 12"
           background={@Mods/StardewUI/Sprites/ControlBorderUncolored}
           background-tint="#8cd">
        <lane vertical-content-alignment="middle">
            <label margin="0, 0, 16, 0" text={#Remapping.Assign.IntroText} />
            <label bold="true" text={#Remapping.Assign.TextBeforeButtons} />
            <button-prompt layout="32px" margin="8, 0" button="LeftTrigger" iconSet={:ButtonIconSet} />
            <label text={#Remapping.Assign.TextBetweenButtons} />
            <button-prompt layout="32px" margin="8, 0" button="RightTrigger" iconSet={:ButtonIconSet} />
            <label text={#Remapping.Assign.TextAfterButtons} />
        </lane>
    </frame>
    <frame *if={ShowUnassignTip}
           padding="16, 12"
           background={@Mods/StardewUI/Sprites/ControlBorderUncolored}
           background-tint="#8cd">
        <lane vertical-content-alignment="middle">
            <label text={#Remapping.Unassign.TextBeforeButton} />
            <button-prompt layout="32px" margin="8, 0" button="ControllerX" iconSet={:ButtonIconSet} />
            <label text={#Remapping.Unassign.TextAfterButton} />
        </lane>
    </frame>
</panel>
</panel>

<template name="remap-slot">
    <lane margin="4, 0"
          orientation="vertical"
          horizontal-content-alignment="middle"
          focusable="true"
          pointer-enter=|^SetSlotHovered(this)|
          pointer-leave=|^SetSlotHovered("null")|
          right-click=|^UnassignSlot(this)|>
        <frame background={@Mods/StardewUI/Sprites/MenuBackgroundUncolored}
               background-tint="#8bd"
               border={@Mods/StardewUI/Sprites/MenuSlotTransparentUncolored}
               border-thickness="4"
               border-tint="#6de"
               focusable="true"
               tooltip={Tooltip}>
            <lane margin="4, 0"
                  orientation="vertical"
                  horizontal-content-alignment="middle">
                <slotted-item icon={Sprite} enabled={IsItemEnabled} />
            </lane>
        </frame>
        <button-prompt layout="32px" button={:Button} iconSet={^ButtonIconSet} />
    </lane>
</template>

<template name="item-group">
    <lane layout="stretch content" margin="0, 0, 0, 16" orientation="vertical">
        <banner text={Name} />
        <grid layout="stretch content"
              item-layout="count: 12"
              item-spacing="0, 8"
              horizontal-item-alignment="middle"
              vertical-item-alignment="middle">
            <frame *repeat={Items}
                   padding="4"
                   background={@Mods/StardewUI/Sprites/MenuSlotTransparent}
                   focusable="true"
                   tooltip={Tooltip}
                   button-press=|^^AssignToSlot($Button, this)|
                   pointer-enter=|^^SetItemHovered(this)|
                   pointer-leave=|^^SetItemHovered("null")|>
                <panel horizontal-content-alignment="middle" vertical-content-alignment="end">
                    <image *if={Hovered}
                           layout="stretch"
                           fit="stretch"
                           sprite={@Mods/StardewUI/Sprites/White}
                           tint="#4c4"
                           opacity="0"
                           +state:assignable={^^CanReassign}
                           +state:assignable:opacity="1"
                           +transition:opacity="250ms EaseOutSine" />
                    <slotted-item icon={Sprite} enabled="true" />
                    <frame margin="0, 0, 0, -12" pointer-events-enabled="false">
                        <button-prompt layout="24px" button={AssignedButton} iconSet={^^ButtonIconSet} />
                    </frame>
                </panel>
            </frame>
        </grid>
    </lane>
</template>

<template name="slotted-item">
    <panel layout="64px"
           margin="4"
           vertical-content-alignment="end"
           pointer-events-enabled="false">
        <image layout="stretch"
               horizontal-alignment="middle"
               vertical-alignment="middle"
               sprite={&icon}
               opacity="0.5"
               +state:enabled={&enabled}
               +state:enabled:opacity="1" />
        <lane layout="stretch content" vertical-content-alignment="end">
            <frame *switch={Quality} layout="24px" margin="-3, 0, 0, -3">
                <image *case="1"
                       layout="stretch"
                       sprite={@Mods/focustense.StarControl/Sprites/Cursors:QualityStarSilver} />
                <image *case="2"
                       layout="stretch"
                       sprite={@Mods/focustense.StarControl/Sprites/Cursors:QualityStarGold} />
                <image *case="4"
                       layout="stretch"
                       sprite={@Mods/focustense.StarControl/Sprites/Cursors:QualityStarIridium} />
            </frame>
            <spacer layout="stretch 0px" />
            <digits *if={IsCountVisible}
                    margin="0, 0, -6, -4"
                    number={Count}
                    scale="3" />
        </lane>
    </panel>
</template>

<template name="button-prompt">
    <panel *switch={&iconSet} layout={&layout} margin="0, -8, 0, 0">
        <panel *case="PlayStation" *switch={&button}>
            <image *case="DPadLeft" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadLeft} />
            <image *case="DPadUp" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadUp} />
            <image *case="DPadRight" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadRight} />
            <image *case="DPadDown" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadDown} />
            <image *case="ControllerA" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadA} />
            <image *case="ControllerB" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadB} />
            <image *case="ControllerX" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadX} />
            <image *case="ControllerY" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadY} />
            <image *case="LeftTrigger" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadLeftTrigger} />
            <image *case="LeftShoulder" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadLeftButton} />
            <image *case="RightTrigger" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadRightTrigger} />
            <image *case="RightShoulder" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI.PlayStation:GamepadRightButton} />
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
            <image *case="LeftTrigger" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadLeftTrigger} />
            <image *case="LeftShoulder" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadLeftButton} />
            <image *case="RightTrigger" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadRightTrigger} />
            <image *case="RightShoulder" layout="stretch" sprite={@Mods/focustense.StarControl/Sprites/UI:GamepadRightButton} />
        </panel>
    </panel>
</template>
