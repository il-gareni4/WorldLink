using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WorldLink.Common
{
    public class LinkPlayer : ModPlayer
    {
        public string LinkedWorldId { get; set; }
        public override void SaveData(TagCompound tag)
        {
            tag["LinkedWorldPath"] = LinkedWorldId;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("LinkedWorldPath", out string path))
                LinkedWorldId = path;
        }
    }
}
