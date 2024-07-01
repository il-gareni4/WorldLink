using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldLink;

public class LinkConfig : ModConfig
{
    public static LinkConfig Instance;

    [DefaultValue(true)] public bool ShowNoWorldLinkedPlayButton;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnLoaded() => Instance = this;
}