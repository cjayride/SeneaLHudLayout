using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("seneaL.valheim.ui")]
    [BepInDependency("org.bepinex.plugins.passivepowers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.grillspett.itemdrawers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("M2Valheim.SkillsReworked", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.maxsch.valheim.vnei", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.creaturelevelcontrol", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("cjayride.CompactStatusSquares", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("valheim.exe")]
    public class SeneaLHudLayoutPlugin : BaseUnityPlugin
    {
        public const string NAME = "SeneaL HUD Layout";
        public const string GUID = "cjayride.SeneaLHudLayout";
        public const string VERSION = "1.1.64";

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> HotbarBottomLeft;
        public static ConfigEntry<float> HotbarBottomMargin;
        public static ConfigEntry<float> HotbarOffsetX;
        public static ConfigEntry<float> HotbarOffsetY;
        public static ConfigEntry<bool> LiftHealthAboveHotbar;
        public static ConfigEntry<float> HealthFoodOffsetY;
        public static ConfigEntry<float> MinimapOffsetX;
        public static ConfigEntry<float> MinimapOffsetY;
        public static ConfigEntry<float> BossOffsetY;
        public static ConfigEntry<float> CompassTop;
        public static ConfigEntry<float> CenterMessageGap;
        public static ConfigEntry<float> CenterMessageScale;
        public static ConfigEntry<bool> CenterMessageToNotices;
        public static ConfigEntry<bool> HideSenealPower;
        public static ConfigEntry<float> PowersGap;
        public static ConfigEntry<float> PowersScale;
        public static ConfigEntry<float> PowersOffsetX;
        public static ConfigEntry<float> PowersOffsetY;
        public static ConfigEntry<bool> CollapseDrawerPreview;
        public static ConfigEntry<bool> ShowHealthNumbers;
        public static ConfigEntry<float> NameTextScale;
        public static ConfigEntry<float> HealthTextScale;
        public static ConfigEntry<float> BarWidth;
        public static ConfigEntry<bool> ShowEnemyLevel;
        public static ConfigEntry<float> LevelSize;
        public static ConfigEntry<bool> AlwaysShowStamina;
        public static ConfigEntry<bool> AlwaysShowEitr;
        public static ConfigEntry<bool> AlwaysShowAdrenaline;
        public static ConfigEntry<bool> Highlight;
        public static ConfigEntry<Color> HighlightColor;
        public static ConfigEntry<float> HighlightSize;
        public static ConfigEntry<bool> CornerPip;
        public static ConfigEntry<float> CornerPipSize;
        public static ConfigEntry<Color> CheckColor;
        public static ConfigEntry<bool> RarityFill;
        public static ConfigEntry<float> RarityFillStrength;
        public static ConfigEntry<bool> ShowEquipCue;
        public static ConfigEntry<bool> ShowGearBarCue;
        public static ConfigEntry<bool> HideServerRules;
        public static ConfigEntry<bool> HideChat;
        public static ConfigEntry<bool> ShowLevelCharacterWindow;
        public static ConfigEntry<bool> ShowLevelFoodBar;
        public static ConfigEntry<float> ShowLevelFoodBarOffsetX;
        public static ConfigEntry<float> ShowLevelFoodBarOffsetY;
        public static ConfigEntry<bool> HideMinimapStats;
        public static ConfigEntry<bool> StatusMove;
        public static ConfigEntry<StatusCorner> StatusCornerSetting;
        public static ConfigEntry<float> StatusOffsetX;
        public static ConfigEntry<float> StatusOffsetY;
        public static ConfigEntry<bool> ShowItemSearch;
        public static ConfigEntry<bool> ClickMaterials;
        public static ConfigEntry<bool> SplitWalletForChest;
        public static ConfigEntry<bool> StatValuesBesideLabels;

        void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true,
                "Apply the layout offsets below. Turn this off to restore SeneaL UI's own placement.");

            HotbarBottomLeft = Config.Bind("Hotbar", "BottomLeft", true,
                "Move the hotbar, action slots, and food quick slots to the bottom left. They are drawn at the top left by SeneaL UI.");
            HotbarBottomMargin = Config.Bind("Hotbar", "BottomMargin", 16f,
                new ConfigDescription("Pixels to leave between the bottom of that cluster and the bottom of the screen.",
                    new AcceptableValueRange<float>(0f, 400f)));
            HotbarOffsetX = Config.Bind("Hotbar", "OffsetX", 0f,
                new ConfigDescription("Extra horizontal nudge in pixels. Positive moves right.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            HotbarOffsetY = Config.Bind("Hotbar", "OffsetY", 0f,
                new ConfigDescription("Extra vertical nudge in pixels, after BottomLeft placement. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            LiftHealthAboveHotbar = Config.Bind("HealthFood", "LiftAboveHotbar", true,
                "Raise the health, food, and guardian-power cluster so it sits above the relocated hotbar.");
            HealthFoodOffsetY = Config.Bind("HealthFood", "OffsetY", 28f,
                new ConfigDescription("Extra pixels to raise the health and food cluster. Positive moves up. Applied on top of LiftAboveHotbar.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            MinimapOffsetX = Config.Bind("Minimap", "OffsetX", -36f,
                new ConfigDescription("Pixels to move the minimap, wind pill, and time pill. Negative moves left.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            MinimapOffsetY = Config.Bind("Minimap", "OffsetY", 0f,
                new ConfigDescription("Pixels to move the minimap cluster. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            BossOffsetY = Config.Bind("BossBar", "OffsetY", 40f,
                new ConfigDescription("Pixels to move boss health bars. Positive moves up. Regular enemy bars stay where SeneaL UI put them.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            CompassTop = Config.Bind("Compass", "Top", 8f,
                new ConfigDescription("Pixels between the top of the screen and the compass. The hotbar and action slots never move it.",
                    new AcceptableValueRange<float>(0f, 800f)));
            CenterMessageGap = Config.Bind("CenterMessage", "Gap", 47f,
                new ConfigDescription("Pixels between the slain / biome banner and whatever is above it: the compass, or the boss bar when one is showing.",
                    new AcceptableValueRange<float>(0f, 400f)));
            CenterMessageScale = Config.Bind("CenterMessage", "Scale", 0.5f,
                new ConfigDescription("Size of the large middle-of-screen message. 1 is SeneaL's size.",
                    new AcceptableValueRange<float>(0.4f, 2.5f)));
            CenterMessageToNotices = Config.Bind("CenterMessage", "SendToNotices", false,
                "Send that large middle message into SeneaL's notification feed or capsules instead of the banner. Uses whatever [Notifications] Style is set to in SeneaL UI.");

            HideSenealPower = Config.Bind("Powers", "HideSenealSlot", true,
                "Hide SeneaL UI's guardian-power circle and its F key. That key does not activate Passive Powers.");
            PowersGap = Config.Bind("Powers", "Gap", 12f,
                new ConfigDescription("Pixels between the food column and the first Passive Powers icon, and between the two icons.",
                    new AcceptableValueRange<float>(0f, 200f)));
            PowersScale = Config.Bind("Powers", "Scale", 1f,
                new ConfigDescription("Size of the Passive Powers icons, their key labels, and the shared cooldown. 1 is the current size. Smaller than 1 shrinks them.",
                    new AcceptableValueRange<float>(0.5f, 1.5f)));
            PowersOffsetX = Config.Bind("Powers", "OffsetX", 0f,
                new ConfigDescription("Extra nudge for the Passive Powers icons. Positive moves right.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            PowersOffsetY = Config.Bind("Powers", "OffsetY", 0f,
                new ConfigDescription("Extra nudge for the Passive Powers icons. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            CollapseDrawerPreview = Config.Bind("Hover", "CollapseItemDrawerStacks", true,
                "When hovering a Grillspett Item Drawer, show one preview slot for the stored item instead of every stack.");
            ShowHealthNumbers = Config.Bind("Creature/Player HUD", "ShowHealthNumbers", true,
                "Show current/max health on creature and player nameplates. Order is name, health numbers, health bar, then stars.");
            NameTextScale = Config.Bind("Creature/Player HUD", "NameTextScale", 0.75f,
                new ConfigDescription("Size of the creature or player name above the health bar. 1 is SeneaL's size.",
                    new AcceptableValueRange<float>(0.5f, 2.5f)));
            HealthTextScale = Config.Bind("Creature/Player HUD", "HealthTextScale", 0.94f,
                new ConfigDescription("Size of the current/max health numbers. 1 is SeneaL's size.",
                    new AcceptableValueRange<float>(0.5f, 2.5f)));
            BarWidth = Config.Bind("Creature/Player HUD", "BarWidth", 1f,
                new ConfigDescription("Width of creature and player health bars. 1 is SeneaL's width. Boss bars stay the same. The health fill uses this width too.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            ShowEnemyLevel = Config.Bind("Creature/Player HUD", "ShowEnemyLevel", false,
                "Unused. The level label beside creature and boss names stays hidden.");
            LevelSize = Config.Bind("Creature/Player HUD", "LevelSize", 14f,
                new ConfigDescription("Font size of that level label.",
                    new AcceptableValueRange<float>(8f, 32f)));
            AlwaysShowStamina = Config.Bind("Vitals", "AlwaysShowStamina", false,
                "Keep the stamina bar visible when it is full. Off leaves SeneaL UI's normal fade.");
            AlwaysShowEitr = Config.Bind("Vitals", "AlwaysShowEitr", false,
                "Keep the eitr bar visible when it is full. Off leaves SeneaL UI's normal fade.");
            AlwaysShowAdrenaline = Config.Bind("Vitals", "AlwaysShowAdrenaline", false,
                "Keep the adrenaline bar visible when it is empty. Off leaves SeneaL UI's normal fade.");
            Highlight = Config.Bind("Slots", "Highlight", true,
                "Add a soft glow around the button's own edge on the selected hotbar item and on gear equipped in the inventory window.");
            HighlightColor = Config.Bind("Slots", "HighlightColor", new Color(1f, 0.843137f, 0f, 1f),
                "Color of that extra glow. Default is gold, FFD700.");
            HighlightSize = Config.Bind("Slots", "HighlightSize", 1.5f,
                new ConfigDescription("How far that glow spreads past the button's own edge, in pixels. 0 hides it. This does not change the check mark.",
                    new AcceptableValueRange<float>(0f, 12f)));
            CornerPip = Config.Bind("Slots", "CornerPip", true,
                "Show a check mark on the selected hotbar item and on gear equipped in the inventory window.");
            CornerPipSize = Config.Bind("Slots", "CornerPipSize", 18f,
                new ConfigDescription("Size of that check mark, in pixels.",
                    new AcceptableValueRange<float>(8f, 40f)));
            CheckColor = Config.Bind("Slots", "CheckColor", new Color(0.15f, 0.92f, 0.28f, 1f),
                "Color of that check mark. Default is a bright green.");
            RarityFill = Config.Bind("Slots", "RarityFill", true,
                "Fill the inside of a magic item's slot with its EpicLoot color (green, blue, purple, orange) and hide SeneaL's rarity border. The gold selected and equipped glow stays. Off restores SeneaL's border.");
            RarityFillStrength = Config.Bind("Slots", "RarityFillStrength", 0.10f,
                new ConfigDescription("How strong that interior color is. 0 is invisible. 1 is a solid plate under the icon.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ShowEquipCue = Config.Bind("Slots", "ShowEquipCue", true,
                "Show a white seconds countdown on an item in the inventory window while it is being equipped or unequipped.");
            ShowGearBarCue = Config.Bind("Slots", "ShowGearBarCue", true,
                "Also show that countdown on the hotbar or action bar, but only while armor or other worn gear is being equipped or unequipped. Switching weapons or tools does not show it.");
            HideServerRules = Config.Bind("Notices", "HideServerRules", true,
                "Never show SeneaL UI's server-rules window when you enter a world.");
            HideChat = Config.Bind("Chat", "HideChat", true,
                "Hide SeneaL UI's chat window. Chat messages still exist for another chat mod, such as Chatter.");
            ShowLevelCharacterWindow = Config.Bind("Character", "ShowLevelCharacterWindow", true,
                "Show your SkillsReworked level in the top left of the character window (Skills, Texts, Trophies, and PvP).");
            ShowLevelFoodBar = Config.Bind("Character", "ShowLevelFoodBar", true,
                "Show your SkillsReworked level directly under the food timers.");
            ShowLevelFoodBarOffsetX = Config.Bind("Character", "ShowLevelFoodBarOffsetX", -39f,
                new ConfigDescription("Horizontal nudge for that food-bar level, in pixels. Positive moves right. Longer levels grow to the right.",
                    new AcceptableValueRange<float>(-200f, 200f)));
            ShowLevelFoodBarOffsetY = Config.Bind("Character", "ShowLevelFoodBarOffsetY", -20.4f,
                new ConfigDescription("Vertical nudge for that food-bar level, in pixels. Positive moves up.",
                    new AcceptableValueRange<float>(-200f, 200f)));
            HideMinimapStats = Config.Bind("Minimap", "HideWindAndServerDay", false,
                "Hide the wind and day/time pills under the minimap. The biome name on the map stays.");
            StatusMove = Config.Bind("Status Effects", "MoveToCorner", false,
                "Move SeneaL UI's status effect list to a screen corner. Off leaves SeneaL's own placement. Compact Status Squares still replaces this list when that mod is enabled.");
            StatusCornerSetting = Config.Bind("Status Effects", "Corner", StatusCorner.TopLeft,
                "Screen corner for that list. The list grows away from the corner.");
            StatusOffsetX = Config.Bind("Status Effects", "OffsetX", 32f,
                new ConfigDescription("Horizontal distance from that corner, in pixels. Positive moves right.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            StatusOffsetY = Config.Bind("Status Effects", "OffsetY", -96f,
                new ConfigDescription("Vertical distance from that corner, in pixels. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            ShowItemSearch = Config.Bind("Crafting", "ShowItemSearch", true,
                "Show an Items button on the crafting panel. It opens VNEI's full item search. The same button then says Close.");
            ClickMaterials = Config.Bind("Crafting", "ClickMaterials", true,
                "In the crafting window, click a requirement that is crafted at this same station to open its recipe. Back returns to the recipe you came from.");
            StatValuesBesideLabels = Config.Bind("Stats", "ValuesBesideLabels", true,
                "Place stat numbers directly beside their names, such as pierce 8. Covers item tooltips and the item window. Off keeps SeneaL's right-aligned numbers.");
            SplitWalletForChest = Config.Bind("Wallet", "SplitForChest", true,
                "Keep the coin purse out of chest deposits. Deposit All, Deposit Similar, and Stack All never take coins from the purse. Move coins into your inventory first if you want them in a chest. Turn this off to let those buttons take the purse.");

            Harmony harmony = new Harmony(GUID);
            CompassPin.Apply(harmony);
            DrawerPreview.Apply(harmony);
            WorldHealthText.Apply(harmony);
            SlotCue.Apply(harmony);
            CharacterExtras.Apply(harmony);
            RulesNotice.Apply(harmony);
            ChatHide.Apply(harmony);
            CenterMessageRoute.Apply(harmony);
            WalletStacks.Apply(harmony);
            RepairTipPlace.Apply(harmony);
            BuildSearchMemory.Apply(harmony);
            StatValues.Apply(harmony);
            CraftMaterials.Apply(harmony);
            StatusEffectsPlace.Apply(harmony);
            Logger.LogInfo("SeneaL HUD Layout loaded. Offsets apply once you are in a world with SeneaL UI.");
        }

        void LateUpdate()
        {
            ResourceBars.Apply();
            SlotCue.Tick();
            CharacterExtras.Tick();
            VneiSearch.Tick();
            CraftMaterials.Tick();
            ChatHide.Tick();
            WorldHealthText.Tick();
            if (Enabled == null || !Enabled.Value)
            {
                HudLayout.Restore();
                return;
            }

            HudLayout.Apply();
        }
    }
}
