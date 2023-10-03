using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldLink.Common;
using static System.Net.Mime.MediaTypeNames;

namespace WorldLink
{
    public class WorldLink : Mod
    {
        public LocalizedText NoWorldText;
        public LocalizedText PlayWorldText;
        public LocalizedText LinkText;
        public LocalizedText UnlinkText;
        public Asset<Texture2D> ChainButtonTexture;
        public Asset<Texture2D> NoChainButtonTexture;
        public Asset<Texture2D> PlayChainButtonTexture;

        public override void Load()
        {
            NoWorldText = Language.GetText("Mods.WorldLink.UI.CharacterListItem.NoWorld");
            PlayWorldText = Language.GetText("Mods.WorldLink.UI.CharacterListItem.PlayWorld");
            LinkText = Language.GetText("Mods.WorldLink.UI.WorldListItem.Link");
            UnlinkText = Language.GetText("Mods.WorldLink.UI.WorldListItem.Unlink");

            ChainButtonTexture = Assets.Request<Texture2D>("Assets/ChainButton");
            NoChainButtonTexture = Assets.Request<Texture2D>("Assets/NoChainButton");
            PlayChainButtonTexture = Assets.Request<Texture2D>("Assets/PlayChainButton");

            IL_UICharacterListItem.ctor += AddCharacterLinkButton;
            IL_UIWorldListItem.ctor += AddWorldLinkButton;
        }

        public override void Unload()
        {
            NoWorldText = null;
            PlayWorldText = null;
            LinkText = null;
            UnlinkText = null;

            ChainButtonTexture = null;
            NoChainButtonTexture = null;
            PlayChainButtonTexture = null;

            IL_UICharacterListItem.ctor -= AddCharacterLinkButton;
            IL_UIWorldListItem.ctor -= AddWorldLinkButton;
        }

        private static WorldFileData GetWorldDataById(string worldId)
        {
            if (!Main.WorldList.Any()) Main.LoadWorlds();
            foreach (WorldFileData worldData in Main.WorldList)
            {
                if (worldData.UniqueId.ToString() == worldId) return worldData;
            }
            return null;
        }

        private static void PlayWorld(WorldFileData worldData)
        {
            UIWorldListItem worldUi = new(worldData, 0, true);
            UIImageButton playButton = (UIImageButton)((List<UIElement>)worldUi.GetType().GetField("Elements", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(worldUi))[1];
            UIMouseEvent evt = new(playButton, new Vector2(Main.mouseX, Main.mouseY));
            worldUi.GetType().GetMethod("PlayGame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(worldUi, new object[] { evt, playButton });
        }

        private static UIText GetButtonLabel(UICharacterListItem listItem)
        {
            if (listItem.GetType() == typeof(UICharacterListItem))
                return (UIText)listItem.GetType()
                    .GetField("_buttonLabel", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(listItem);
            else
                return (UIText)((List<UIElement>)listItem.GetType()
                    .GetField("Elements", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(listItem)).First((el) => el is UIText && el.Left.Pixels > 0 && el.Top.Pixels < 0);
        }

        private static UIText GetButtonLabel(UIWorldListItem listItem)
        {
            if (listItem.GetType() == typeof(UIWorldListItem))
                return (UIText)listItem.GetType()
                    .GetField("_buttonLabel", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(listItem);
            else
                return (UIText)((List<UIElement>)listItem.GetType()
                    .GetField("Elements", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(listItem)).First((el) => el is UIText && el.Left.Pixels > 0 && el.Top.Pixels < 0);
        }

        private void AddCharacterLinkButton(ILContext il)
        {
            try
            {
                ILCursor c = new(il)
                {
                    Index = 140
                };

                c.EmitLdarg0();
                c.EmitLdarg1();
                c.EmitLdloca(0);
                c.EmitLdarg2();
                c.EmitDelegate((UICharacterListItem listItem, PlayerFileData playerData, ref float shift, int order) =>
                {
                    LinkPlayer modPlayer = playerData.Player.GetModPlayer<LinkPlayer>();
                    WorldFileData worldData = GetWorldDataById(modPlayer.LinkedWorldId);

                    if (modPlayer.LinkedWorldId is not null && (worldData is null || !worldData.IsValid)) 
                        modPlayer.LinkedWorldId = null;
                    if ((modPlayer.LinkedWorldId is null && !LinkConfig.Instance.ShowNoWorldLinkedPlayButton) ||
                        (Main.menuMultiplayer && !Main.menuServer))
                        return;

                    UIImageButton linkButton = new(NoChainButtonTexture)
                    {
                        VAlign = 1f,
                        Left = StyleDimension.FromPixelsAndPercent(shift, 0f)
                    };
                    if (modPlayer.LinkedWorldId is not null) linkButton.SetImage(PlayChainButtonTexture);

                    linkButton.OnLeftClick += (evt, el) =>
                    {
                        if (modPlayer.LinkedWorldId is null) return;
                        playerData.SetAsActive();
                        PlayWorld(worldData);
                    };
                    linkButton.OnMouseOver += (evt, el) =>
                    {
                        string text = modPlayer.LinkedWorldId is null ? NoWorldText.Value : PlayWorldText.Format(worldData.Name);
                        GetButtonLabel(listItem)?.SetText(text);
                    };
                    MethodInfo defaultOut = listItem.GetType().GetMethod("ButtonMouseOut", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (defaultOut is not null)
                        linkButton.OnMouseOut += defaultOut.CreateDelegate<UIElement.MouseEvent>(listItem);
                    else
                        linkButton.OnMouseOut += (evt, el) => GetButtonLabel(listItem)?.SetText("");
                    linkButton.SetSnapPoint("Link", order, null, null);
                    listItem.Append(linkButton);

                    shift += 24f;
                });
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<WorldLink>(), il);
            }
        }

        private void UpdateWorldListItemText(UIWorldListItem listItem, WorldFileData worldData)
        {
            LinkPlayer modPlayer = Main.ActivePlayerFileData.Player.GetModPlayer<LinkPlayer>();
            if (modPlayer.LinkedWorldId == worldData.UniqueId.ToString()) GetButtonLabel(listItem)?.SetText(UnlinkText.Format(modPlayer.Player.name));
            else GetButtonLabel(listItem)?.SetText(LinkText.Format(modPlayer.Player.name));
        }

        private void AddWorldLinkButton(ILContext il)
        {
            try
            {
                ILCursor c = new(il)
                {
                    Index = 305
                };
                c.EmitLdarg0();
                c.EmitLdarg1();
                c.EmitLdloca(0);
                c.EmitLdarg2();
                c.EmitLdarg3();
                c.EmitDelegate((UIWorldListItem listItem, WorldFileData worldData, ref float shift, int order, bool canBePlayed) =>
                {
                    if (!canBePlayed) return;
                    LinkPlayer modPlayer = Main.ActivePlayerFileData.Player.GetModPlayer<LinkPlayer>();

                    UIImageButton linkButton = new(ChainButtonTexture)
                    {
                        VAlign = 1f,
                        Left = StyleDimension.FromPixelsAndPercent(shift, 0f)
                    };
                    if (modPlayer.LinkedWorldId == worldData.UniqueId.ToString()) linkButton.SetImage(NoChainButtonTexture);

                    linkButton.OnLeftClick += (evt, el) =>
                    {
                        if (modPlayer.LinkedWorldId == worldData.UniqueId.ToString()) modPlayer.LinkedWorldId = null;
                        else Main.ActivePlayerFileData.Player.GetModPlayer<LinkPlayer>().LinkedWorldId = worldData.UniqueId.ToString();

                        bool prevMapEnabled = Main.mapEnabled;
                        Main.mapEnabled = false;
                        Player.SavePlayer(Main.ActivePlayerFileData, true);
                        Main.mapEnabled = prevMapEnabled;

                        if (linkButton.IsMouseHovering) UpdateWorldListItemText(listItem, worldData);
                    };
                    linkButton.OnUpdate += (el) =>
                    {
                        if (modPlayer.LinkedWorldId == worldData.UniqueId.ToString()) linkButton.SetImage(NoChainButtonTexture);
                        else linkButton.SetImage(ChainButtonTexture);
                    };
                    linkButton.OnMouseOver += (evt, el) => UpdateWorldListItemText(listItem, worldData);
                    MethodInfo defaultOut = listItem.GetType().GetMethod("ButtonMouseOut", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (defaultOut is not null)
                        linkButton.OnMouseOut += defaultOut.CreateDelegate<UIElement.MouseEvent>(listItem);
                    else
                        linkButton.OnMouseOut += (evt, el) => GetButtonLabel(listItem)?.SetText("");
                    linkButton.SetSnapPoint("Link", order, null, null);
                    listItem.Append(linkButton);

                    shift += 24f;
                });
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<WorldLink>(), il);
            }
        }
    }
}