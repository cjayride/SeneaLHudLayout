using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("seneaL.valheim.ui")]
    [BepInDependency("org.bepinex.plugins.passivepowers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("valheim.exe")]
    public class SeneaLHudLayoutPlugin : BaseUnityPlugin
    {
        public const string NAME = "SeneaL HUD Layout";
        public const string GUID = "cjayride.SeneaLHudLayout";
        public const string VERSION = "1.1.3";

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
        public static ConfigEntry<float> PowersOffsetX;
        public static ConfigEntry<float> PowersOffsetY;

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
            PowersOffsetX = Config.Bind("Powers", "OffsetX", 0f,
                new ConfigDescription("Extra nudge for the Passive Powers icons. Positive moves right.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            PowersOffsetY = Config.Bind("Powers", "OffsetY", 0f,
                new ConfigDescription("Extra nudge for the Passive Powers icons. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            CompassPin.Apply(new Harmony(GUID));
            Logger.LogInfo("SeneaL HUD Layout loaded. Offsets apply once you are in a world with SeneaL UI.");
        }

        void LateUpdate()
        {
            if (Enabled == null || !Enabled.Value)
            {
                HudLayout.Restore();
                return;
            }

            HudLayout.Apply();
        }
    }
}
