using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldLink
{
    public class LinkConfig : ModConfig
    {
        public static LinkConfig Instance;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => Instance = this;

        [DefaultValue(true)]
        public bool ShowNoWorldLinkedPlayButton;
    }
}
