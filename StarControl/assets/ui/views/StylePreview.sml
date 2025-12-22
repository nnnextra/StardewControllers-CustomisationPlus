<frame layout="520px 560px"
       padding="16"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane orientation="vertical" horizontal-content-alignment="middle">
        <label margin="0, 8, 0, 12"
               font="dialogue"
               color="#d93"
               shadow-alpha="0.6"
               shadow-color="#666"
               shadow-offset="-3, 3"
               text={#Config.Preview.Heading} />
        <image layout="480px" sprite={:Sprite} />
    </lane>
</frame>
