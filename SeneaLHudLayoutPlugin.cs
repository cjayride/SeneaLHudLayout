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
    [BepInProcess("valheim.exe")]
    public class SeneaLHudLayoutPlugin : BaseUnityPlugin
    {
        public const string NAME = "SeneaL HUD Layout";
        public const string GUID = "cjayride.SeneaLHudLayout";
        public const string VERSION = "1.1.16";

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
        public static ConfigEntry<bool> HideSenealPower;
        public static ConfigEntry<float> PowersGap;
        public static ConfigEntry<float> PowersScale;
        public static ConfigEntry<float> PowersOffsetX;
        public static ConfigEntry<float> PowersOffsetY;
        public static ConfigEntry<bool> CollapseDrawerPreview;
        public static ConfigEntry<bool> ShowWorldHealthNumbers;
        public static ConfigEntry<bool> AlwaysShowStamina;
        public static ConfigEntry<bool> AlwaysShowEitr;
        public static ConfigEntry<bool> AlwaysShowAdrenaline;
        public static ConfigEntry<float> SelectionGlow;
        public static ConfigEntry<bool> ShowEquipCue;
        public static ConfigEntry<bool> HideServerRules;

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
            CenterMessageGap = Config.Bind("CenterMessage", "Gap", 18f,
                new ConfigDescription("Pixels between the slain / biome banner and whatever is above it: the compass, or the boss bar when one is showing.",
                    new AcceptableValueRange<float>(0f, 400f)));

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
            ShowWorldHealthNumbers = Config.Bind("WorldHud", "ShowHealthNumbers", true,
                "Show current/max health just above creature and player health bars.");
            AlwaysShowStamina = Config.Bind("Vitals", "AlwaysShowStamina", false,
                "Keep the stamina bar visible when it is full. Off leaves SeneaL UI's normal fade.");
            AlwaysShowEitr = Config.Bind("Vitals", "AlwaysShowEitr", false,
                "Keep the eitr bar visible when it is full. Off leaves SeneaL UI's normal fade.");
            AlwaysShowAdrenaline = Config.Bind("Vitals", "AlwaysShowAdrenaline", false,
                "Keep the adrenaline bar visible when it is empty. Off leaves SeneaL UI's normal fade.");
            SelectionGlow = Config.Bind("Slots", "SelectionGlow", 1.4f,
                new ConfigDescription("Extra brightness on the frame around the selected hotbar item and equipped gear. 0 leaves SeneaL's soft glow.",
                    new AcceptableValueRange<float>(0f, 3f)));
            ShowEquipCue = Config.Bind("Slots", "ShowEquipCue", true,
                "Show a white seconds countdown on the icon while an item is equipping, unequipping, or reloading.");
            HideServerRules = Config.Bind("Notices", "HideServerRules", true,
                "Never show SeneaL UI's server-rules window when you enter a world.");

            Harmony harmony = new Harmony(GUID);
            CompassPin.Apply(harmony);
            DrawerPreview.Apply(harmony);
            WorldHealthText.Apply(harmony);
            SlotCue.Apply(harmony);
            RulesNotice.Apply(harmony);
            Logger.LogInfo("SeneaL HUD Layout loaded. Offsets apply once you are in a world with SeneaL UI.");
        }

        void LateUpdate()
        {
            ResourceBars.Apply();
            SlotCue.Tick();
            if (Enabled == null || !Enabled.Value)
            {
                HudLayout.Restore();
                return;
            }

            HudLayout.Apply();
        }
    }
}
