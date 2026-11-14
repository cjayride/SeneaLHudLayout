using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    public enum StarAlign
    {
        Left,
        Center,
        Right
    }

    public enum VitalsBarOrder
    {
        Stamina_Eitr_Adrenaline,   // current order you like
        Stamina_Adrenaline_Eitr,
        Eitr_Stamina_Adrenaline,
        Eitr_Adrenaline_Stamina,
        Adrenaline_Stamina_Eitr,
        Adrenaline_Eitr_Stamina
    }

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("seneaL.valheim.ui")]
    [BepInDependency("_shudnal.ConfigurationManager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.passivepowers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.grillspett.itemdrawers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("M2Valheim.SkillsReworked", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("M2Valheim.TalentTree", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.tenacity", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.maxsch.valheim.vnei", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.creaturelevelcontrol", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("cjayride.CompactStatusSquares", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("valheim.exe")]
    public class SeneaLHudLayoutPlugin : BaseUnityPlugin
    {
        public const string NAME = "SeneaL HUD Layout";
        public const string GUID = "cjayride.SeneaLHudLayout";
        public const string VERSION = "1.5.65";

        /// <summary>
        /// 1.5.60: the mod ships THREE XP displays - the action-slot card ([Hotbar] ShowXpBar),
        /// the plate under the food timers (HealthFood.ShowLevelFoodBar), and the character
        /// window's bar (Character Window.ShowLevelXpBar). Enabling more than one at once made
        /// the controls fight: three cards, one set of knobs. Exactly ONE is live at a time,
        /// by this priority: Hotbar card > food-timers plate > character-window bar. The
        /// highest-priority enabled one wins and the others hide themselves.
        /// </summary>
        /// <summary>
        /// Which XP display wins when [Hotbar] XpBarOneAtATime is on. 1.5.62: the food-timers
        /// plate ([HealthFood] ShowLevelFoodBar) is NOT part of this - it shows whenever its own
        /// toggle is on, OneAtATime never hides it (the user asked for exactly that). Only the
        /// action-slot card and the character-window bar take turns.
        /// </summary>
        public enum XpBarOwner
        {
            None,
            Hotbar,
            FoodBar,
            CharacterWindow
        }

        static XpBarOwner _xpOwner = XpBarOwner.None;

        public static ConfigEntry<bool> ShowSlotXpBarOneAtATime;

        /// <summary>The single XP display that is live right now.</summary>
        public static XpBarOwner ActiveXpBar()
        {
            if (ShowSlotXpBar != null && ShowSlotXpBar.Value)
            {
                return XpBarOwner.Hotbar;
            }

            if (ShowLevelXpBar != null && ShowLevelXpBar.Value)
            {
                return XpBarOwner.CharacterWindow;
            }

            return XpBarOwner.None;
        }

        /// <summary>
        /// True when THIS feature owns the XP display. Called once per tick per feature; the
        /// owner is logged once per change (and only when more than one bar is switched on),
        /// so "my bar vanished" explains itself in LogOutput.txt. The food-timers plate never
        /// calls this (1.5.62: it is independent of the one-at-a-time rule).
        /// </summary>
        public static bool XpBarOwned(XpBarOwner who)
        {
            if (ShowSlotXpBarOneAtATime == null || !ShowSlotXpBarOneAtATime.Value)
            {
                return true;
            }

            XpBarOwner owner = ActiveXpBar();
            if (owner != _xpOwner)
            {
                int enabled = (ShowSlotXpBar != null && ShowSlotXpBar.Value ? 1 : 0)
                    + (ShowLevelXpBar != null && ShowLevelXpBar.Value ? 1 : 0);
                if (enabled > 1)
                {
                    Debug.Log(
                        "[SeneaL HUD Layout] " + enabled +
                        " XP bars are enabled - the action-slot card and the character-window bar" +
                        " take turns (the food-timers plate always follows its own toggle)." +
                        " Showing: " + owner);
                }

                _xpOwner = owner;
            }

            return owner == who;
        }
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> HotbarBottomLeft;
        public static ConfigEntry<float> HotbarBottomMargin;
        public static ConfigEntry<float> HotbarOffsetX;
        public static ConfigEntry<float> HotbarOffsetY;
        public static ConfigEntry<bool> LiftHealthAboveHotbar;
        public static ConfigEntry<float> HealthFoodOffsetY;
        public static ConfigEntry<float> HealthFoodOffsetX;
        public static ConfigEntry<float> HealthBarMaxHeight;
        public static ConfigEntry<float> MinimapOffsetX;
        public static ConfigEntry<float> MinimapOffsetY;
        public static ConfigEntry<float> BossOffsetY;
        public static ConfigEntry<bool> HideBossStars;
        public static ConfigEntry<float> CompassTop;
        public static ConfigEntry<bool> ShipsOnCompass;
        public static ConfigEntry<bool> CartsOnCompass;
        public static ConfigEntry<bool> PortalsOnCompass;
        public static ConfigEntry<bool> DeathPinOnCompass;
        public static ConfigEntry<float> CenterMessageGap;
        public static ConfigEntry<float> CenterMessageScale;
        public static ConfigEntry<bool> CenterMessageToNotices;
        public static ConfigEntry<bool> HideSenealPower;
        public static ConfigEntry<float> PowersGap;
        public static ConfigEntry<float> PowersScale;
        public static ConfigEntry<float> PowersOffsetX;
        public static ConfigEntry<float> PowersOffsetY;
        public static ConfigEntry<bool> CollapseDrawerPreview;
        public static ConfigEntry<bool> CreatureHud;
        public static ConfigEntry<bool> ShowHealthNumbers;
        public static ConfigEntry<bool> ShowThreatIcons;
        public static ConfigEntry<bool> ShowNameplateDiamonds;
        public static ConfigEntry<bool> ShowCreatureStars;
        public static ConfigEntry<float> ThreatIconSize;
        public static ConfigEntry<float> ThreatIconLift;
        public static ConfigEntry<float> ThreatIconOpacity;
        public static ConfigEntry<float> NameTextScale;
        public static ConfigEntry<float> HealthTextScale;
        public static ConfigEntry<float> BarWidth;
        public static ConfigEntry<float> StarSize;
        public static ConfigEntry<StarAlign> StarAlignment;
        public static ConfigEntry<bool> ShowEnemyLevel;
        public static ConfigEntry<float> LevelSize;
        public static ConfigEntry<bool> HideNameplateExtras;
        public static ConfigEntry<bool> AlwaysShowStamina;
        public static ConfigEntry<bool> AlwaysShowEitr;
        public static ConfigEntry<bool> AlwaysShowAdrenaline;
        public static ConfigEntry<float> VitalsNumberBold;
        public static ConfigEntry<bool> SyncVitalsSize;
        public static ConfigEntry<float> VitalsWidth;
        public static ConfigEntry<float> VitalsHeight;
        public static ConfigEntry<Vector2> VitalsOffset;
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
        public static ConfigEntry<bool> ShowEquipQueue;
        public static ConfigEntry<bool> HideServerRules;
        public static ConfigEntry<bool> ShowKillXp;
        public static ConfigEntry<bool> HideChat;
        public static ConfigEntry<bool> ChatSettingsEnabled;
        public static ConfigEntry<bool> DefaultToShout;
        public static ConfigEntry<bool> RememberLastUsed;
        public static ConfigEntry<bool> ShoutKeepsCapitalization;
        public static ConfigEntry<bool> HideShoutOverHead;
        public static ConfigEntry<bool> ChatAutoScrollOnNewMessage;
        public static ConfigEntry<float> HideChatPanelDelay;
        public static ConfigEntry<bool> ShowLevelCharacterWindow;
        public static ConfigEntry<bool> ShowTalentsButton;
        public static ConfigEntry<bool> KeepMapClosedOnAccept;
        public static ConfigEntry<Color> QuestBackground;
        public static ConfigEntry<float> QuestBackgroundAlpha;
        public static ConfigEntry<bool> ShowLevelXpBar;
        public static ConfigEntry<bool> FixSkillRespendAfterDeath;
        public static ConfigEntry<bool> ShowLevelFoodBar;
        public static ConfigEntry<bool> ShowSlotXpBar;
        public static ConfigEntry<float> ShowHotbarSlotsOffsetX;
        public static ConfigEntry<float> ShowHotbarSlotsOffsetY;
        public static ConfigEntry<float> ShowSlotXpBarScale;
        public static ConfigEntry<float> ShowSlotXpBarTrackOpacity;
        public static ConfigEntry<float> ShowSlotXpBarOpacity;
        public static ConfigEntry<bool> ShowSlotXpBarBorder;
        public static ConfigEntry<float> ShowSlotXpBarBorderWidth;
        public static ConfigEntry<float> ShowSlotXpBarBorderOpacity;
        public static ConfigEntry<bool> ShowSlotXpBarWhiteNumbers;
        public static ConfigEntry<XpBarAnchor> ShowSlotXpBarAnchor;
        public static ConfigEntry<float> ShowSlotXpBarPaddingX;
        public static ConfigEntry<float> ShowSlotXpBarPaddingY;
        public static ConfigEntry<bool> ShowSlotXpBarCompact;
        public static ConfigEntry<float> ShowSlotXpBarCompactBarWidth;
        public static ConfigEntry<float> ShowSlotXpBarCompactBottomPad;
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
        public static ConfigEntry<bool> WishboneInKeySlot;
        public static ConfigEntry<float> BuildPanelScale;
        public static ConfigEntry<bool> StatsTab;
        public static ConfigEntry<KeyboardShortcut> StatsHotkey;
        public static ConfigEntry<bool> StatsPortrait;
        public static ConfigEntry<bool> StatsGuardianPowers;
        public static ConfigEntry<bool> StatValuesBesideLabels;
        public static ConfigEntry<ItemTipLeftOverflow> ItemTipOverflow;
        public static ConfigEntry<bool> ShowTooltipItemIcon;
        public static ConfigEntry<bool> ShowPieceHealth;
        public static ConfigEntry<float> ShowPieceHealthSize;
        public static ConfigEntry<bool> ShowPieceHealthPercent;
        public static ConfigEntry<float> ShowPieceHealthPercentSize;
        public static ConfigEntry<bool> ShowShipHealth;
        public static ConfigEntry<bool> ShowShipHealthNumbers;
        public static ConfigEntry<VitalsBarOrder> VitalsOrder;
        public static ConfigEntry<bool> HideStaminaNumber;
        public static ConfigEntry<bool> HideEitrNumber;
        public static ConfigEntry<bool> HideAdrenalineNumber;
        public static ConfigEntry<float> EpicLootAbilitiesOffsetX;
        public static ConfigEntry<float> EpicLootAbilitiesOffsetY;
        public static ConfigEntry<bool> ShardSocketWindow;
        public static ConfigEntry<KeyboardShortcut> ShardSocketHotkey;
        public static ConfigEntry<bool> ShardWindowRequireConfirmClose;
        public static ConfigEntry<float> ShardWindowPanelScale;
        public static ConfigEntry<bool> ChatFontSettingOn;
        public static ConfigEntry<string> ChatFontAsset;
        public static ConfigEntry<float> ChatFontSize;
        public static ConfigEntry<bool> ChatWindowRecommendedLayout;
        public static ConfigEntry<int> ChatWindowPosX;
        public static ConfigEntry<int> ChatWindowPosY;
        public static ConfigEntry<int> ChatWindowWidth;
        public static ConfigEntry<int> ChatWindowHeight;
        public static ConfigEntry<float> ChatBackgroundOpacity;
        public static ConfigEntry<bool> ShoutWhite;
        public static ConfigEntry<bool> SayLightGrey;
        public static ConfigEntry<string> ShoutColorHex;
        public static ConfigEntry<string> SayColorHex;
        public static ConfigEntry<bool> ChatAlwaysVisible;
        public static ConfigEntry<bool> AllChatShout;
        public static ConfigEntry<bool> PotionCooldowns;
        public static ConfigEntry<float> PotionCooldownTextOpacity;
        public static ConfigEntry<float> PotionCooldownDimOpacity;
        public static ConfigEntry<float> PotionCooldownInset;
        public static ConfigEntry<float> PotionCooldownTextOffsetY;

        public const int ChatWindowRecommendedPosX = 84;
        public const int ChatWindowRecommendedPosY = -88;
        public const int ChatWindowRecommendedWidth = -88;
        public const int ChatWindowRecommendedHeight = -9;

        public static bool ChatSettingsOn()
        {
            return ChatSettingsEnabled != null && ChatSettingsEnabled.Value;
        }

        void Awake()
        {
            // 1.5.60: THREE XP displays, ONE at a time. The user enabled all three and the knobs
            // fought ("we have 3 independently controlled xp bars, i don't want that"), so
            // [Hotbar] XpBarOneAtATime (ON default) lets only the highest-priority ENABLED display
            // draw: action-slot card > food-timers plate > character-window bar. The other two
            // hide themselves. The switches keep their long-standing keys ([Hotbar] ShowXpBar,
            // [HealthFood] ShowLevelFoodBar, [Character Window] ShowLevelXpBar) - a 1.5.60 draft
            // moved them into a new [XpBar] section, which made ConfigHousekeeping DELETE the
            // players' tuned [Hotbar] ShowXpBar line, so that rename is reverted and removed.
            ShowSlotXpBar = Config.Bind("Hotbar", "ShowXpBar",
                ConfigDefault(Config, "Hotbar", "ShowXpBar", "Character", "ShowXpBar", false),
                "Show your SkillsReworked level and XP bar on the HUD as a gold card (Hotbar.XpBarAnchor picks WHERE: RightOfSlots, TopLeft, or UnderFoodBar). All its knobs are the [Hotbar] XpBar* settings: anchor, padding, scale, compact, colours. OFF by default; the CJAYCRAFT pack ships it ON with XpBarAnchor = UnderFoodBar. With [Hotbar] XpBarOneAtATime on, this display wins over the food-timers plate and the character-window bar. Client-only.");
            ShowSlotXpBarOneAtATime = Config.Bind("Hotbar", "XpBarOneAtATime", true,
                "Show only ONE of the mod's three XP displays at a time (1.5.60, ON default): the action-slot card ([Hotbar] ShowXpBar), the level plate under the food timers ([HealthFood] ShowLevelFoodBar), and the character window's bar ([Character Window] ShowLevelXpBar). Switching on more than one used to stack them, three cards sharing one set of knobs. ON: the highest-priority enabled one draws - action-slot card, then food-timers plate, then character-window bar - and the rest hide. OFF: all three show at once, the pre-1.5.60 behaviour. Applies live, no restart. Client-only.");

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
            HotbarOffsetY = Config.Bind("Hotbar", "OffsetY", 14.5f,
                new ConfigDescription("Extra vertical nudge in pixels, after BottomLeft placement. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            ShowHotbarSlotsOffsetX = Config.Bind("Hotbar", "SlotsOffsetX", 0f,
                new ConfigDescription("Move the ACTION SLOTS and the QUIVER frame (arrows) right, on top of the layout everything else uses. Positive moves right, negative left. 1.5.60: the user asked to clear the slots' old spot so the XP card can sit there - slide the slots right with this, then park the card in the vacated place with Hotbar.XpBarPaddingX (the card anchors to the slots, so it rides this nudge and measures from the slots' NEW edge). The item bar and the food quick slots stay put. Applies on the next world load / F1, no restart. Client-only.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            ShowHotbarSlotsOffsetY = Config.Bind("Hotbar", "SlotsOffsetY", 0f,
                new ConfigDescription("Vertical partner of Hotbar.SlotsOffsetX, for the same two groups. 0 keeps their shipped height. Positive moves up. Applies on the next world load / F1, no restart. Client-only.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            // 1.5.60: the ON switch moved to [Hotbar] ShowXpBar (the section at the top of
            // the file the user asked for); every XpBar* knob stays here and names it.
            ShowSlotXpBarScale = Config.Bind("Hotbar", "XpBarScale", 1f,
                new ConfigDescription(
                    "Size multiplier for that action-slot XP bar ([Hotbar] ShowXpBar): the level line, bar, percent, and XP numbers with their plates. "
                    + "1 is the character window's own size, 2 doubles it, 0.5 halves it. 0.5-3. Client-only.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            ShowSlotXpBarTrackOpacity = Config.Bind("Hotbar", "XpBarTrackOpacity", 0.75f,
                new ConfigDescription(
                    "Black opacity of the action-slot XP bar's track ([Hotbar] ShowXpBar): 0.75 (the default) is a dark groove with the gold progress bar standing out; 0.25 shows the scenery through it, 1 is solid black. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ShowSlotXpBarOpacity = Config.Bind("Hotbar", "XpBarOpacity", 0.75f,
                new ConfigDescription(
                    "Black opacity of the action-slot XP bar's BACKDROP plates ([Hotbar] ShowXpBar): 0.75 (the default) is a dark card the scenery shows faintly through; 0.5 is see-through (the 1.5.45 default - washed out to invisible over bright scenery), 1 is solid black. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ShowSlotXpBarBorder = Config.Bind("Hotbar", "XpBarBorder", true,
                "Draw the gold BORDER line around the action-slot XP bar card ([Hotbar] ShowXpBar). The border is four solid line quads (top/bottom/left/right), never a filled rectangle. Off removes it entirely; applies live, no restart. Client-only.");
            ShowSlotXpBarBorderWidth = Config.Bind("Hotbar", "XpBarBorderWidth", 0.5f,
                new ConfigDescription(
                    "Thickness of the action-slot XP bar card's border LINE in HUD pixels ([Hotbar] ShowXpBar): 0.5 (the default) is a hairline, 1 is a pixel, 2 is chunky. Scales with Hotbar.XpBarScale; applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(0.1f, 4f)));
            ShowSlotXpBarBorderOpacity = Config.Bind("Hotbar", "XpBarBorderOpacity", 0.15f,
                new ConfigDescription(
                    "Gold opacity of the action-slot XP bar card's border ([Hotbar] ShowXpBar): 0.15 (the default) is a faint gold edge; 1 is a solid gold line. The gold TEXT keeps full alpha. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ShowSlotXpBarWhiteNumbers = Config.Bind("Hotbar", "XpBarWhiteNumbers", false,
                "Draw the action-slot XP bar's NUMBERS in white ([Hotbar] ShowXpBar, off by default): the level number, the percentage and the `## / ##` values go white while the words and symbols around them - `Level `, the ` / ` between the values and the `%` - keep the gold text colour. Off keeps everything gold. Applies live, no restart. Client-only.");
            ShowSlotXpBarAnchor = Config.Bind("Hotbar", "XpBarAnchor", XpBarAnchor.RightOfSlots,
                "Where the action-slot XP bar hangs ([Hotbar] ShowXpBar). Starter layouts: RightOfSlots = beside the action/quiver HUD slots (XpBarPaddingX default 222 clears three quiver cells); TopLeft = top-left of the screen (PaddingX/Y 12); UnderFoodBar = under the food timers (PaddingX/Y 12, 0.75x). Existing configs keep their own values. Applies live, no restart. Client-only.");
            ShowSlotXpBarPaddingX = Config.Bind("Hotbar", "XpBarPaddingX", 222f,
                new ConfigDescription(
                    "Horizontal padding in HUD pixels for the action-slot XP bar ([Hotbar] ShowXpBar). RightOfSlots: gap after the last action slot - 222 is the new starter (three 70px quiver cells + 12) so the card does not sit on the arrows; 12 was the old gap. TopLeft: inset from the screen LEFT. UnderFoodBar: inset from the food cluster LEFT. Your saved value is never overwritten. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(-400f, 1600f)));
            ShowSlotXpBarPaddingY = Config.Bind("Hotbar", "XpBarPaddingY", 12f,
                new ConfigDescription(
                    "Vertical padding in HUD pixels for the XP bar ([Hotbar] ShowXpBar). Starters: TopLeft 12 from the screen top; UnderFoodBar 12 below the food timers; RightOfSlots 12 centres on the action slots (above 12 moves up). Saved configs keep their value.",
                    new AcceptableValueRange<float>(-1600f, 1600f)));
            ShowSlotXpBarCompact = Config.Bind("Hotbar", "XpBarCompact", false,
                "Condense the action-slot XP bar to ONE row ([Hotbar] ShowXpBar): `Lv 42  bar  37%  1234/3333` on a single plate - shorter level word, no spaces around the slash, a narrower bar (Hotbar.XpBarCompactBarWidth) and tighter gaps - so the card fits in tight HUD space. Off keeps the shipped two-row card (`Level 42 · bar · 37%` over the XP numbers). Works on EVERY anchor, UnderFoodBar included (1.5.61: that anchor used to force this ON, which is why the toggle looked dead). Applies live, no restart. Client-only.");
            ShowSlotXpBarCompactBarWidth = Config.Bind("Hotbar", "XpBarCompactBarWidth", 70f,
                new ConfigDescription(
                    "Width in HUD pixels of the CONDENSED XP bar's track (Hotbar.XpBarCompact): 70 is the default, 100 matches the full card's bar. Scales with Hotbar.XpBarScale. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(30f, 400f)));
            ShowSlotXpBarCompactBottomPad = Config.Bind("Hotbar", "XpBarCompactBottomPad", 4f,
                new ConfigDescription(
                    "Empty space in HUD pixels BELOW the text row inside the CONDENSED XP bar card (Hotbar.XpBarCompact): 4 is the 1.5.61 default (your report: 'add 4 pixels to the padding bottom'), 0 makes the card's bottom border hug the text, 8 is roomy. The condensed card also trims 3 pixels off its TOP (the 'remove 3 pixels from the padding top' half) at a fixed rate this knob does not touch. The pad grows the card DOWNWARD only - the text row and the food timers above never move. Scales with Hotbar.XpBarScale. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(0f, 40f)));

            LiftHealthAboveHotbar = Config.Bind("HealthFood", "LiftAboveHotbar", true,
                "Raise the health, food, and guardian-power cluster so it sits above the relocated hotbar.");
            HealthFoodOffsetY = Config.Bind("HealthFood", "OffsetY", -75f,
                new ConfigDescription("Extra pixels to raise the health and food cluster. Positive moves up. Applied on top of LiftAboveHotbar.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            HealthFoodOffsetX = Config.Bind("HealthFood", "OffsetX", 54f,
                new ConfigDescription("Extra horizontal spacing for the food bar relative to the health bar. Positive moves right.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            HealthBarMaxHeight = Config.Bind("HealthFood", "HealthBarMaxHeight", 250f,
                new ConfigDescription("Max pixel height of the SeneaL player health bar after HealthCapacity sizing. The bar still grows with max HP up to this cap, then stays this tall. Fill is always current / max of that height (updates with HP). SeneaL ticks, keep-space hatch, numbers, shapes, and rune shield are unchanged. HUD editor Fixed length is also capped. 80â€“800.",
                    new AcceptableValueRange<float>(80f, 800f)));
            bool showLevelFoodBarDefault = ConfigDefault(
                Config, "HealthFood", "ShowLevelFoodBar", "Character", "ShowLevelFoodBar", false);
            ShowLevelFoodBar = Config.Bind("HealthFood", "ShowLevelFoodBar", showLevelFoodBarDefault,
                "Show your SkillsReworked level directly under the food timers. Off by default. This plate is INDEPENDENT of [Hotbar] XpBarOneAtATime (1.5.62): turn it on and it shows, even when the action-slot XP card is also on - it does not take turns with the other XP bars.");
            float showLevelFoodBarOffsetXDefault = ConfigDefault(
                Config, "HealthFood", "ShowLevelFoodBarOffsetX", "Character", "ShowLevelFoodBarOffsetX", 12f);
            ShowLevelFoodBarOffsetX = Config.Bind("HealthFood", "ShowLevelFoodBarOffsetX", showLevelFoodBarOffsetXDefault,
                new ConfigDescription("Horizontal nudge for the food-timer XP plate. Starter is 12 (slightly right of the food slots). Positive moves right. Saved configs keep their value.",
                    new AcceptableValueRange<float>(-200f, 200f)));
            float showLevelFoodBarOffsetYDefault = ConfigDefault(
                Config, "HealthFood", "ShowLevelFoodBarOffsetY", "Character", "ShowLevelFoodBarOffsetY", -11f);
            ShowLevelFoodBarOffsetY = Config.Bind("HealthFood", "ShowLevelFoodBarOffsetY", showLevelFoodBarOffsetYDefault,
                new ConfigDescription("Vertical nudge for the food-timer XP plate. Starter is -11 (just under the timers). Positive moves up. Saved configs keep their value.",
                    new AcceptableValueRange<float>(-200f, 200f)));

            MinimapOffsetX = Config.Bind("Minimap", "OffsetX", -36f,
                new ConfigDescription("Pixels to move the minimap, wind pill, and time pill. Negative moves left.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            MinimapOffsetY = Config.Bind("Minimap", "OffsetY", 0f,
                new ConfigDescription("Pixels to move the minimap cluster. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            BossOffsetY = Config.Bind("BossBar", "OffsetY", 40f,
                new ConfigDescription("Pixels to move boss health bars. Positive moves up. Regular enemy bars stay where SeneaL UI put them.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            HideBossStars = Config.Bind("BossBar", "HideStars", true,
                "Remove the star icons on the boss health card. Creature nameplate stars are a separate setting.");

            CompassTop = Config.Bind("Compass", "Top", 8f,
                new ConfigDescription("Pixels between the top of the screen and the compass. The hotbar and action slots never move it.",
                    new AcceptableValueRange<float>(0f, 800f)));
            ShipsOnCompass = Config.Bind("Compass", "ShowShips", false,
                "Show the ship markers SeneaL UI's [Map] LiveShips adds on the compass. Off keeps them on the map and minimap only.");
            CartsOnCompass = Config.Bind("Compass", "ShowCarts", false,
                "Show the cart markers SeneaL UI's [Map] LiveCarts adds on the compass. Off keeps them on the map and minimap only.");
            PortalsOnCompass = Config.Bind("Compass", "ShowPortals", false,
                "Show the portal markers SeneaL UI's [Map] LivePortals adds on the compass. Off keeps them on the map and minimap only.");
            DeathPinOnCompass = Config.Bind("Compass", "DeathPinOnCompass", true,
                "When you die, mark the grave pin for the SeneaL compass automatically. Helps when SeneaL UI [Map] CompassPins is Marked (the game adds death pins unchecked by default).");
            CenterMessageGap = Config.Bind("CenterMessage", "Gap", 110f,
                new ConfigDescription("Pixels between the slain / biome banner and whatever is above it: the compass, or the boss bar when one is showing.",
                    new AcceptableValueRange<float>(0f, 400f)));
            CenterMessageScale = Config.Bind("CenterMessage", "Scale", 0.5f,
                new ConfigDescription("Size of the large middle-of-screen message. 1 is SeneaL's size.",
                    new AcceptableValueRange<float>(0.4f, 2.5f)));
            CenterMessageToNotices = Config.Bind("CenterMessage", "SendToNotices", false,
                "Send that large middle message into SeneaL's notification feed or capsules instead of the banner. Uses whatever [Notifications] Style is set to in SeneaL UI.");

            HideSenealPower = Config.Bind("Powers", "HideSenealSlot", true,
                "Hide SeneaL UI's guardian-power circle and its F key. Works with or without Passive Powers. "
                + "When Passive Powers is installed, only the unused SeneaL circle is hidden; Passive Powers icons stay.");
            PowersGap = Config.Bind("Powers", "Gap", 12f,
                new ConfigDescription("Pixels between the food column and the first Passive Powers icon, and between the two icons.",
                    new AcceptableValueRange<float>(0f, 200f)));
            PowersScale = Config.Bind("Powers", "Scale", 1f,
                new ConfigDescription("Size of the Passive Powers icons, their key labels, and the shared cooldown. 1 is the current size. Smaller than 1 shrinks them.",
                    new AcceptableValueRange<float>(0.5f, 1.5f)));
            PowersOffsetX = Config.Bind("Powers", "OffsetX", -30f,
                new ConfigDescription("Extra nudge for the Passive Powers icons. Positive moves right.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            PowersOffsetY = Config.Bind("Powers", "OffsetY", -78f,
                new ConfigDescription("Extra nudge for the Passive Powers icons. Positive moves up.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));

            CollapseDrawerPreview = Config.Bind("Hover", "CollapseItemDrawerStacks", true,
                "When hovering a Grillspett Item Drawer, show one preview slot for the stored item instead of every stack.");
            ShowPieceHealth = Config.Bind("Hover", "ShowPieceHealth", false,
                "With the hammer out, show the piece's current/max health beside SeneaL's crosshair piece health bar (PieceHealthIndicator). "
                + "Requires that bar to stay visible: on SeneaL UI 1.2.8+, turn off TooltipExtra and TooltipPlates (or use an equivalent layout). "
                + "Does not add the piece name.");
            ShowPieceHealthSize = Config.Bind("Hover", "ShowPieceHealthSize", 14f,
                new ConfigDescription("Text size of the current/max piece health number.",
                    new AcceptableValueRange<float>(8f, 32f)));
            ShowPieceHealthPercent = Config.Bind("Hover", "ShowPieceHealthPercent", false,
                "With the hammer out, show the piece's health percent beside SeneaL's crosshair piece health bar (PieceHealthIndicator). "
                + "Requires that bar to stay visible: on SeneaL UI 1.2.8+, turn off TooltipExtra and TooltipPlates (or use an equivalent layout). "
                + "Does not add the piece name.");
            ShowPieceHealthPercentSize = Config.Bind("Hover", "ShowPieceHealthPercentSize", 14f,
                new ConfigDescription("Text size of the piece health percent.",
                    new AcceptableValueRange<float>(8f, 32f)));
            ShowShipHealth = Config.Bind("Ship", "ShowHealth", false,
                "While you are at the helm, show a hull health bar under the sailing wind disc (SeneaL's wind widget, or vanilla if that is off). Off by default. Client-only.");
            ShowShipHealthNumbers = Config.Bind("Ship", "ShowNumbers", true,
                "Show current/max hull health under that ship health bar. Only used while Ship.ShowHealth is on. Client-only.");
            CreatureHud = Config.Bind("Creature/Player HUD", "Enabled", true,
                "Apply this mod's creature and player nameplate changes: health numbers, star size, name size, bar width, threat icons. Off leaves those nameplates entirely to SeneaL UI.");
            ShowHealthNumbers = Config.Bind("Creature/Player HUD", "ShowHealthNumbers", true,
                "Show current/max health on creature and player nameplates. Order is name, health numbers, health bar, then stars.");
            ShowThreatIcons = Config.Bind("Creature/Player HUD", "ShowThreatIcons", true,
                "Show the Alerted/Aware threat icons on creature nameplates. Turn off to hide them.");
            ShowNameplateDiamonds = Config.Bind("Creature/Player HUD", "ShowNameplateDiamonds", false,
                "Show SeneaL UI's gold diamonds and lines on either side of the creature name. Off hides them.");
            ShowCreatureStars = Config.Bind("Creature/Player HUD", "ShowCreatureStars", true,
                "Show the level stars under creature health bars. Off hides them.");
            ThreatIconSize = Config.Bind("Creature/Player HUD", "ThreatIconSize", 5f,
                new ConfigDescription("Size of the Alerted/Aware icons in pixels. SeneaL's own size is about 26.",
                    new AcceptableValueRange<float>(2f, 32f)));
            ThreatIconLift = Config.Bind("Creature/Player HUD", "ThreatIconLift", 10f,
                new ConfigDescription("Raise the Alerted/Aware icons above the creature name, in pixels.",
                    new AcceptableValueRange<float>(0f, 24f)));
            ThreatIconOpacity = Config.Bind("Creature/Player HUD", "ThreatIconOpacity", 0.71f,
                new ConfigDescription("Opacity of the Alerted/Aware icons. 1 is fully visible, 0 is invisible.",
                    new AcceptableValueRange<float>(0f, 1f)));
            NameTextScale = Config.Bind("Creature/Player HUD", "NameTextScale", 0.75f,
                new ConfigDescription("Size of the creature or player name above the health bar. 1 is SeneaL's size.",
                    new AcceptableValueRange<float>(0.5f, 2.5f)));
            HealthTextScale = Config.Bind("Creature/Player HUD", "HealthTextScale", 0.94f,
                new ConfigDescription("Size of the current/max health numbers. 1 is SeneaL's size.",
                    new AcceptableValueRange<float>(0.5f, 2.5f)));
            BarWidth = Config.Bind("Creature/Player HUD", "BarWidth", 1f,
                new ConfigDescription("Width of creature and player health bars. 1 is SeneaL's width. Boss bars stay the same. The health fill uses this width too.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            StarSize = Config.Bind("Creature/Player HUD", "StarSize", 10f,
                new ConfigDescription("Size of each creature level star, in pixels. Every star uses this size, and it stays matched to the health bar when you move closer or farther.",
                    new AcceptableValueRange<float>(4f, 24f)));
            StarAlignment = Config.Bind("Creature/Player HUD", "StarAlign", StarAlign.Left,
                "Where the level stars sit under the health bar. SeneaL UI 1.1.9 centers its own stars; this one is ours.");
            ShowEnemyLevel = Config.Bind("Creature/Player HUD", "ShowEnemyLevel", false,
                "Show the [Lvl:N] label under the health bar. If Creature Level and Loot Control already adds one, this toggles that label instead of drawing a second.");
            LevelSize = Config.Bind("Creature/Player HUD", "LevelSize", 14f,
                new ConfigDescription("Font size of our own level label when CLLC is not providing one.",
                    new AcceptableValueRange<float>(8f, 32f)));
            HideNameplateExtras = Config.Bind("Creature/Player HUD", "HideNameplateExtras", false,
                "Hide extra nameplate lines above the creature name (CLLC affixes like Aggressive, EliteCreatures mutation lines, and empty caption boxes on some modded creatures). Keeps the main Name and health/level text.");
            AlwaysShowStamina = Config.Bind("Vitals", "AlwaysShowStamina", false,
                "Keep the stamina bar visible when it is full. Off leaves SeneaL UI's normal fade.");
            AlwaysShowEitr = Config.Bind("Vitals", "AlwaysShowEitr", false,
                "Keep the eitr bar visible when it is full. Off leaves SeneaL UI's normal fade.");
            AlwaysShowAdrenaline = Config.Bind("Vitals", "AlwaysShowAdrenaline", false,
                "Keep the adrenaline bar visible when it is empty. Off leaves SeneaL UI's normal fade.");
            VitalsNumberBold = Config.Bind("Vitals", "NumberBold", 0.8f,
                new ConfigDescription("How heavy the stamina, eitr, and adrenaline numbers look. 0 is SeneaL's own weight. 1 thickens the digits (face dilate + outline). Change it in F1 and you should see it immediately.",
                    new AcceptableValueRange<float>(0f, 1f)));
            SyncVitalsSize = Config.Bind("Vitals", "SyncSize", true,
                "Make the adrenaline and eitr bars match the stamina bar's size. Fixes misalignment in SeneaL UI v1.2.1.");
            VitalsWidth = Config.Bind("Vitals", "Width", 300f,
                new ConfigDescription("Target width for all vital bars when SyncSize is on, pixels.",
                    new AcceptableValueRange<float>(50f, 600f)));
            VitalsHeight = Config.Bind("Vitals", "Height", 24f,
                new ConfigDescription("Target height for all vital bars when SyncSize is on, pixels.",
                    new AcceptableValueRange<float>(10f, 60f)));
            VitalsOffset = Config.Bind("Vitals", "Offset", Vector2.zero,
                new ConfigDescription("Extra nudge for the vitals cluster after syncing. Positive X moves right, positive Y moves up."));
            VitalsOrder = Config.Bind("Vitals", "BarOrder", VitalsBarOrder.Stamina_Eitr_Adrenaline,
                new ConfigDescription("Order of the three resource bars from top to bottom."));
            HideStaminaNumber = Config.Bind("Vitals", "HideStaminaNumber", false,
                new ConfigDescription("Hide the stamina value number and its background square."));
            HideEitrNumber = Config.Bind("Vitals", "HideEitrNumber", false,
                new ConfigDescription("Hide the eitr value number and its background square."));
            HideAdrenalineNumber = Config.Bind("Vitals", "HideAdrenalineNumber", false,
                new ConfigDescription("Hide the adrenaline value number and its background square."));           
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
            RarityFill = Config.Bind("Slots", "RarityFill", false,
                "Fill the inside of a magic item's slot with its EpicLoot color (green, blue, purple, orange) and hide SeneaL's rarity border. The gold selected and equipped glow stays. Off restores SeneaL's border.");
            RarityFillStrength = Config.Bind("Slots", "RarityFillStrength", 0.10f,
                new ConfigDescription("How strong that interior color is. 0 is invisible. 1 is a solid plate under the icon.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ShowEquipCue = Config.Bind("Slots", "ShowEquipCue", false,
                "Show a white seconds countdown on an item in the inventory window while it is being equipped or unequipped.");
            ShowGearBarCue = Config.Bind("Slots", "ShowGearBarCue", false,
                "Also show that countdown on the hotbar or action bar, but only while armor or other worn gear is being equipped or unequipped. Switching weapons or tools does not show it.");
            ShowEquipQueue = Config.Bind("Slots", "ShowEquipQueue", false,
                "Show SeneaL UI's number in the top left of an item icon while it is waiting to equip. That number is the queue position, 1 then 2, not a timer. Off hides it.");
            HideServerRules = Config.Bind("Notices", "HideServerRules", true,
                "Never show SeneaL UI's server-rules window when you enter a world.");
            ShowKillXp = Config.Bind("Notices", "ShowKillXp", true,
                "When a creature dies, add the SkillsReworked Level XP from that kill to the damage-share message (or show both together if no share line appears). Off leaves the damage percent as-is.");
            HideChat = Config.Bind("Chat", "HideChat", false,
                "Hide SeneaL UI's chat window. Chat messages still exist for another chat mod, such as Chatter.");
            ChatSettingsEnabled = Config.Bind("Chat", "ChatSettingsEnabled", false,
                "Master on/off for every Chat setting except HideChat, HideShoutOverHead, ChatAutoScrollOnNewMessage, ChatWindowRecommendedLayout, ChatAlwaysVisible, ChatBackgroundOpacity, ShoutWhiteText, ShoutTextColor, SayLightGreyText, SayTextColor, AllChatShout, and the ChatWindow Pos/Width/Height keys. Off leaves Valheim/SeneaL chat as-is for "
                + "DefaultToShout, RememberLastUsed, ShoutKeepsCapitalization, HideChatPanelDelay, and the chat font keys.");
            DefaultToShout = Config.Bind("Chat", "DefaultToShout", false,
                "Off keeps Valheim's chat exactly as it is. On, the chat box opens with /s already typed in it, so your first Enter "
                + "shouts (the long-range message above your character) and you never have to type the prefix. Delete the /s and the "
                + "message is a normal close-range say, and the box stops prefilled until you type /s again.");
            RememberLastUsed = Config.Bind("Chat", "RememberLastUsed", false,
                "With DefaultToShout on, the box prefills /s only while shout is the channel in use: after a message sent with /s, "
                + "and not after one sent as plain text. Kept in the game's own prefs file, so it is still remembered after you quit. "
                + "Off always prefills /s.");
            ShoutKeepsCapitalization = Config.Bind("Chat", "ShoutKeepsCapitalization", false,
                "Valheim prints shouted chat in ALL CAPS, in the chat log and above the character. Off (default) does NOT keep shout "
                + "capitalization: the shout shows the letters exactly the way the player typed them. On keeps Valheim's ALL CAPS shout. "
                + "Whispers stay lower case, as Valheim makes them.");
            HideShoutOverHead = Config.Bind("Chat", "HideShoutOverHead", true,
                "Hide shouted chat (yell) above player heads. The shout still appears in the chat log. Independent of ChatSettingsEnabled. Client-only.");
            ChatAutoScrollOnNewMessage = Config.Bind("Chat", "ChatAutoScrollOnNewMessage", true,
                "When the chat input is closed and a new message arrives, jump the log to the latest lines. Stops you missing chat after scrolling up and clicking away. While the input is open, scroll position is kept. Independent of ChatSettingsEnabled. Client-only.");
            HideChatPanelDelay = Config.Bind("Chat", "HideChatPanelDelay", 12f,
                new ConfigDescription(
                    "Seconds the chat panel stays visible after the last chat activity, written to the game's own chat hide timer. "
                    + "The game ships 10; this mod ships 12. Client-only.",
                    new AcceptableValueRange<float>(1f, 600f)));
            ChatAlwaysVisible = Config.Bind("Chat", "ChatAlwaysVisible", false,
                "Leave the chat window always on: it never fades out after the last activity, so the message log is visible on screen "
                + "all the time. Off restores the normal fade after HideChatPanelDelay. HideChat wins when both are on. Independent of "
                + "ChatSettingsEnabled. Client-only.");
            ChatBackgroundOpacity = Config.Bind("Chat", "ChatBackgroundOpacity", 0.25f,
                new ConfigDescription(
                    "Opacity of the chat window's dark background (the panel behind the messages and the input box). 1 is SeneaL UI's own "
                    + "solid glass, 0.25 is a quarter as opaque (the default), 0 hides the fill. The chat text is not faded. Client-only.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ShoutWhite = Config.Bind("Chat", "ShoutWhiteText", true,
                "Print shouted chat in the shout colour below (white by default) in the chat log. Valheim prints shouts in yellow. Off leaves shouts yellow. Independent of ChatSettingsEnabled. Client-only.");
            ShoutColorHex = Config.Bind("Chat", "ShoutTextColor", "FFFFFF",
                "Hex RGB colour of shouted chat text when ShoutWhiteText is on, with or without # (for example FFFFFF white, FFD700 Valheim yellow). Empty restores vanilla yellow. Independent of ChatSettingsEnabled. Client-only.");
            SayLightGrey = Config.Bind("Chat", "SayLightGreyText", true,
                "Print say / proximity chat in the say colour below (light grey by default) in the chat log, so shouts stand out from it. Valheim prints it white. Off leaves it white. "
                + "Independent of ChatSettingsEnabled. Client-only.");
            SayColorHex = Config.Bind("Chat", "SayTextColor", "C7C7C7",
                "Hex RGB colour of say / proximity chat text when SayLightGreyText is on, with or without # (for example C7C7C7 light grey, FFFFFF white). Empty restores vanilla white. Independent of ChatSettingsEnabled. Client-only.");
            AllChatShout = Config.Bind("Chat", "AllChatShout", true,
                "Every chat message is a shout, with no prefixes to type at all. The box opens empty (no /s prefilled, RememberLastUsed is not consulted) and plain text you type is sent as /s. "
                + "A typed /say (or /w) sends that one message close-range. Console commands are untouched. Needs ChatSettingsEnabled on. Client-only.");
            PotionCooldowns = Config.Bind("Items", "PotionCooldowns", true,
                "Show a cooldown countdown on consumables (PotionsPlus potions and other Valheim food/potions) sitting in the SeneaL action "
                + "slots: while the item's consume cooldown is running, the slot is dimmed and shows the whole seconds left. Off hides it. Client-only.");
            PotionCooldownTextOpacity = Config.Bind("Items", "PotionCooldownTextOpacity", 0.65f,
                new ConfigDescription(
                    "Opacity of the potion cooldown countdown number drawn over the action slot. 1 is solid white (the number hides the potion icon), "
                    + "0.65 (the default) lets the icon read through it, 0 hides the number. The slot dim below is not faded. Client-only.",
                    new AcceptableValueRange<float>(0f, 1f)));
            PotionCooldownDimOpacity = Config.Bind("Items", "PotionCooldownDimOpacity", 0.55f,
                new ConfigDescription(
                    "Opacity of the dark tint laid over an action slot while its potion is on cooldown. 0 leaves the slot undimmed and only the number shows. Client-only.",
                    new AcceptableValueRange<float>(0f, 1f)));
            PotionCooldownInset = Config.Bind("Items", "PotionCooldownInset", 10f,
                new ConfigDescription(
                    "Pixels shaved off EVERY side of the potion cooldown square so it matches the drawn quick-action button instead of reaching past it. 10 (the default) sits inside the button's border art; 0 covers the slot's full face. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(0f, 40f)));
            PotionCooldownTextOffsetY = Config.Bind("Items", "PotionCooldownTextOffsetY", 5f,
                new ConfigDescription(
                    "Pixels to move the cooldown countdown NUMBER DOWN from the slot's centre (negative moves it up). 5 is the default. Applies live, no restart. Client-only.",
                    new AcceptableValueRange<float>(-40f, 40f)));
            ChatFontSettingOn = Config.Bind("Chat", "chatMessageFontEnabled", false,
                "Apply the chat font and size below to the chat log text. Off leaves the font the chat UI (SeneaL UI, Chatter) chose.");
            ChatFontAsset = Config.Bind("Chat", "chatMessageTextFontAsset", "Valheim-AveriaSansLibre",
                new ConfigDescription(
                    "FontAsset (TMP) to use for chat message text. Any TMP font asset loaded in the game, matched by name; "
                    + "the game ships Valheim-AveriaSansLibre, Valheim-AveriaSerifLibre, Valheim-Norse, Valheim-Norsebold, Valheim-Prstartk, "
                    + "AveriaSansLibre-Bold SDF, LiberationSans SDF, the Fallback-Noto* set, the NotoSans/NotoSerif language sets, NotoEmoji, and Opensans - Fallback. "
                    + "Empty keeps the font the chat UI chose. Client-only. Chatter's own ChatMessage.Text.Font keys still drive Chatter's panel.",
                    new AcceptableValueList<string>(ChatFont.FontNames)));
            ChatFontSize = Config.Bind("Chat", "chatMessageTextFontSize", 16f,
                new ConfigDescription("The font size to use for chat messages.",
                    new AcceptableValueRange<float>(ChatFont.MinFontSize, ChatFont.MaxFontSize)));
            ChatWindowRecommendedLayout = Config.Bind("Chat", "ChatWindowRecommendedLayout", false,
                "Built-in recommended chat layout: PosX 84, PosY -88, Width -88, Height -9. "
                + "While on, ChatWindowPosX/Y/Width/Height are ignored (your values stay in the cfg for when you turn this off). Client-only.");
            ChatWindowPosX = Config.Bind("Chat", "ChatWindowPosX", 0,
                new ConfigDescription(
                    "Pixels to move the whole chat from SeneaL's original window. 0 is that original. Positive is right. Any integer (for example -76). Client-only.",
                    new AcceptableValueRange<int>(-400, 400)));
            ChatWindowPosY = Config.Bind("Chat", "ChatWindowPosY", 0,
                new ConfigDescription(
                    "Pixels to move the whole chat from SeneaL's original window. 0 is that original. Positive is up. Any integer (for example -76). Client-only.",
                    new AcceptableValueRange<int>(-400, 400)));
            ChatWindowWidth = Config.Bind("Chat", "ChatWindowWidth", 0,
                new ConfigDescription(
                    "Added width in pixels vs SeneaL's original chat. 0 is original. Negative makes it narrower. Any integer works (for example -10). Client-only.",
                    new AcceptableValueRange<int>(-400, 400)));
            ChatWindowHeight = Config.Bind("Chat", "ChatWindowHeight", 0,
                new ConfigDescription(
                    "Added height in pixels vs SeneaL's original chat. 0 is original. Negative makes it shorter. Any integer works (for example -10). Client-only.",
                    new AcceptableValueRange<int>(-400, 400)));
            bool showLevelCharacterWindowDefault = ConfigDefault(
                Config, "Character Window", "ShowLevelCharacterWindow", "Character", "ShowLevelCharacterWindow", true);
            ShowLevelCharacterWindow = Config.Bind("Character Window", "ShowLevelCharacterWindow", showLevelCharacterWindowDefault,
                "Show your SkillsReworked level in the top left of the character window (Skills, Texts, Trophies, and PvP).");
            bool showTalentsButtonDefault = ConfigDefault(
                Config, "Character Window", "ShowTalentsButton", "Character", "ShowTalentsButton", true);
            ShowTalentsButton = Config.Bind("Character Window", "ShowTalentsButton", showTalentsButtonDefault,
                "Show a Talents button at the top right of the character window, left of the settings gear. It opens TalentTree. Needs TalentTree installed.");
            KeepMapClosedOnAccept = Config.Bind("Quests", "KeepMapClosedOnAccept", true,
                "Buying a bounty or treasure map from the trader does not open the world map.");
            QuestBackground = Config.Bind("Quests", "Background", new Color(0.08f, 0.07f, 0.05f, 1f),
                "Solid fill behind the Bounties and Treasure list.");
            QuestBackgroundAlpha = Config.Bind("Quests", "BackgroundAlpha", 1f,
                new ConfigDescription("How opaque that fill is. 1 is solid. 0 is invisible.",
                    new AcceptableValueRange<float>(0f, 1f)));
            QuestBackground.SettingChanged += (_, __) => AdventureJournal.PaintBackground();
            QuestBackgroundAlpha.SettingChanged += (_, __) => AdventureJournal.PaintBackground();
            bool showLevelXpBarDefault = ConfigDefault(
                Config, "Character Window", "ShowLevelXpBar", "Character", "ShowLevelXpBar", false);
            ShowLevelXpBar = Config.Bind("Character Window", "ShowLevelXpBar", showLevelXpBarDefault,
                "Show a 100 pixel SkillsReworked XP bar under the character-window level line (starter: immediately to the right of the level text). OFF by default; the pack may ship it ON. With [Hotbar] XpBarOneAtATime on, this bar yields to the action-slot card. Client-only.");
            bool fixSkillRespendDefault = ConfigDefault(
                Config, "Character Window", "FixSkillRespendAfterDeath", "Character", "FixSkillRespendAfterDeath", true);
            FixSkillRespendAfterDeath = Config.Bind("Character Window", "FixSkillRespendAfterDeath", fixSkillRespendDefault,
                "Let you spend points again in a skill that lost a partial level on death, such as Tenacity going from 30 to 29.1. Fractional levels are rounded down when the Skills window opens. Only works when both Smoothbrain Tenacity and SkillsReworked are installed. Otherwise it does nothing.");
            HideMinimapStats = Config.Bind("Minimap", "HideWindAndServerDay", true,
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
            StatsTab = Config.Bind("Stats", "StatsTab", true,
                "Add a Stats tab on the character panel (person icon) with a live vitals and defense sheet.");
            StatsHotkey = Config.Bind("Stats", "StatsHotkey", new KeyboardShortcut(KeyCode.None),
                "Open or close the Stats sheet while the inventory is open. Unbound by default.");
            StatsPortrait = Config.Bind("Stats", "StatsPortrait", true,
                "Show a square Steam portrait next to your name on the Stats sheet.");
            StatsGuardianPowers = Config.Bind("Stats", "GuardianPowers", true,
                "Show a Guardian powers section on the Stats sheet: one row per equipped boss power with its passive state (Active with buff time, Depleted with lockout time, Passive at rest) and the passive effect lines from the Passive Powers config. Needs the Passive Powers mod; without it the section does not appear.");
            bool valuesBesideDefault = ConfigDefault(
                Config,
                "ItemTooltips",
                "ValuesBesideLabels",
                "Stats",
                "ValuesBesideLabels",
                true);
            StatValuesBesideLabels = Config.Bind("ItemTooltips", "ValuesBesideLabels", valuesBesideDefault,
                "Place stat numbers directly beside their names, such as pierce 8. Covers item tooltips and the item window. Off keeps SeneaL's right-aligned numbers.");
            ItemTipLeftOverflow overflowDefault = ConfigDefault(
                Config,
                "ItemTooltips",
                "LeftOverflow",
                "Stats",
                "LeftOverflow",
                ItemTipLeftOverflow.SetThenMagic);
            ItemTipOverflow = Config.Bind("ItemTooltips", "LeftOverflow", overflowDefault,
                "Balance two-column item tooltips by moving whole sections (set, magic, stats) between columns for similar height. Combat always stays on the left. Set = set may move; SetThenMagic = set and magic may move. Off keeps SeneaL's layout.");
            ShowTooltipItemIcon = Config.Bind("ItemTooltips", "ShowItemIcon", true,
                "Show the item's own sprite on the type line (for example [hoe] Tool). Small, same row height. Not SeneaL's large TooltipIcon box.");
            SplitWalletForChest = Config.Bind("Wallet", "SplitForChest", true,
                "Keep the coin purse out of chest deposits. Deposit All, Deposit Similar, and Stack All never take coins from the purse. Move coins into your inventory first if you want them in a chest. Turn this off to let those buttons take the purse.");
            WishboneInKeySlot = Config.Bind("Inventory", "WishboneInKeySlot", false,
                "Put the wishbone in a SeneaL dungeon-key slot instead of the charm / utility cell. Off (default) keeps SeneaL's normal wishbone slot. Needs key slots enabled in SeneaL UI. The wishbone is stored there, not worn; this mod keeps its finder ping effect running while it sits in a key slot, as long as it is not equipped. Turn off to return it to the charm cell.");
            InventoryRowMerge.MergeSlotRows = Config.Bind("Inventory", "MergeSlotRows", false,
                "EXPERIMENTAL (1.5.62): FOOD, ACTION and QUIVER ARROW cells on ONE row of the SeneaL inventory panel (where the food slots sit), labels above the action and arrow groups, empty action-row space reclaimed. Display only - items keep their real grid cells. OFF (default) is stock SeneaL. Client-only; world re-load / F1.");
            QuiverMeshToggle.ShowQuiverMesh = Config.Bind("Inventory", "ShowQuiverMesh", true,
                "Show the equipped quiver model on your character. Off hides only the 3D quiver; ammo slots and the equipped item stay. The small button to the right of the inventory QUIVER label flips this.");
            BuildPanelScale = Config.Bind("BuildPanel", "Scale", 0.8f,
                new ConfigDescription(
                    "Size of SeneaL's selected-piece BuildPanel (the hammer piece card). 1 is SeneaL's current size. 0.8 is this mod's default. SeneaL's own [Sizes] BuildPanel stays at 1 unless you change that too.",
                    new AcceptableValueRange<float>(0.5f, 1.5f)));

            EpicLootAbilitiesOffsetX = Config.Bind("EpicLootAbilities", "OffsetX", 12f,
                new ConfigDescription(
                    "Horizontal gap between Epic Loot Legendary Set abilities and Guardian Powers, or food bars when Guardian Powers are unavailable. Positive values increase the gap.",
                    new AcceptableValueRange<float>(-200f, 400f)));

            EpicLootAbilitiesOffsetY = Config.Bind("EpicLootAbilities", "OffsetY", 0f,
                new ConfigDescription(
            "Vertical adjustment for Epic Loot Legendary Set ability icons. Positive moves up.",
            new AcceptableValueRange<float>(-400f, 400f)));

            ShardSocketWindow = Config.Bind("EpicLoot", "ShardSocketWindow", true,
                "Client-only, not synced. When on and Epic Loot is loaded, open shard sockets in a dedicated window so add/remove works with SeneaL CustomUI on. Off leaves Epic Loot's own overlay shortcut unchanged and adds no extra hotkey.");
            ShardSocketHotkey = Config.Bind("EpicLoot", "ShardSocketHotkey", new KeyboardShortcut(KeyCode.E),
                "Opens the shard socket window for the inventory item under the mouse. Inventory must be open and the item must have Epic Loot shard sockets. Client-only. Default is E (same as Epic Loot's overlay shortcut; this mod then hosts the socket row in the window).");
            ShardWindowRequireConfirmClose = Config.Bind("EpicLoot", "ShardWindowRequireConfirmClose", false,
                "If on, do not close the shard window while an inventory item is being dragged. Client-only. Off (default) closes with Esc, the X button, or closing inventory.");
            ShardWindowPanelScale = Config.Bind("EpicLoot", "ShardWindowPanelScale", 1f,
                new ConfigDescription(
                    "Extra scale for the shard socket window, on top of the automatic size that fits the item's socket count. 1 is the compact auto-fit size; lower makes it smaller. Client-only.",
                    new AcceptableValueRange<float>(0.5f, 1.5f)));

            Harmony harmony = new Harmony(GUID);
            ConfigHousekeeping.PruneLegacyEntries(Config, Logger);

            // 1.5.62 lesson (your UI wipe-out): one feature's Harmony patch threw during Awake
            // ("Parameter __inv does not contain a valid index" in the inventory row merge),
            // the exception left Awake, BepInEx marked the plugin FAILED, and the whole mod
            // stopped running - hotbar placement, XP bar, notices, everything. Every feature is
            // now registered through Safe(): a feature that fails logs its name and the rest of
            // the mod keeps working exactly as configured.
            Safe("HealthBarCap", () => HealthBarCap.Apply(harmony));
            Safe("ShudnalConfigBridge", () => ShudnalConfigBridge.Apply(harmony));
            Safe("CompassPin", () => CompassPin.Apply(harmony));
            Safe("CompassLivePins", () => CompassLivePins.Apply(harmony));
            Safe("DeathPinCompass", () => DeathPinCompass.Apply(harmony));
            Safe("StatsWindow", () => StatsWindow.Apply(harmony));
            Safe("DrawerPreview", () => DrawerPreview.Apply(harmony));
            Safe("PieceHealthLabel", () => PieceHealthLabel.Apply(harmony));
            Safe("WorldHealthText", () => WorldHealthText.Apply(harmony));
            Safe("SlotCue", () => SlotCue.Apply(harmony));
            Safe("CharacterExtras", () => CharacterExtras.Apply(harmony));
            Safe("AdventureJournal", () => AdventureJournal.Apply(harmony));
            Safe("RulesNotice", () => RulesNotice.Apply(harmony));
            Safe("ChatHide", () => ChatHide.Apply(harmony));
            Safe("ChatWindowLayout", () => ChatWindowLayout.Apply(harmony));
            Safe("ChatPrefix", () => ChatPrefix.Apply(harmony));
            Safe("ChatShoutText", () => ChatShoutText.Apply(harmony));
            Safe("CenterMessageRoute", () => CenterMessageRoute.Apply(harmony));
            Safe("WalletStacks", () => WalletStacks.Apply(harmony));
            Safe("RepairTipPlace", () => RepairTipPlace.Apply(harmony));
            Safe("BuildSearchMemory", () => BuildSearchMemory.Apply(harmony));
            Safe("StatValues", () => StatValues.Apply(harmony));
            Safe("TipItemIcon", () => TipItemIcon.Apply(harmony));
            Safe("TipColumnOverflow", () => TipColumnOverflow.Apply(harmony));
            Safe("CraftMaterials", () => CraftMaterials.Apply(harmony));
            Safe("StatusEffectsPlace", () => StatusEffectsPlace.Apply(harmony));
            Safe("SkillRespendFix", () => SkillRespendFix.Apply(harmony));
            Safe("VanillaSkillsWindow", () => VanillaSkillsWindow.Apply(harmony));
            Safe("RebirthNotice", () => RebirthNotice.Apply(harmony));
            Safe("KillXpNotice", () => KillXpNotice.Apply(harmony));
            Safe("EquipQueueMark", () => EquipQueueMark.Apply(harmony));
            Safe("WishboneKeySlot", () => WishboneKeySlot.Apply(harmony));
            Safe("InventoryRowMerge", () => InventoryRowMerge.Apply(harmony));
            Safe("QuiverMeshToggle", () => QuiverMeshToggle.Apply(harmony));
            Safe("EpicLootShardWindow", () => EpicLootShardWindow.Apply(harmony));
            Logger.LogInfo("SeneaL HUD Layout loaded. Offsets apply once you are in a world with SeneaL UI.");
        }

        /// <summary>
        /// Run one feature's registration. A feature that throws (bad patch signature, a mod's
        /// method moved, ...) is reported by name and SKIPPED - it can never take the whole
        /// plugin down. Every other feature and the per-frame placement keep running.
        /// </summary>
        static void Safe(string feature, Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] feature '" + feature + "' failed to start and is " +
                    "skipped; every other feature of the mod keeps running. " + e);
            }
        }

        void LateUpdate()
        {
            ResourceBars.Apply();
            ShipHealthMeter.Tick();
            SlotCue.Tick();
            PotionCooldown.Tick();
            CharacterExtras.Tick();
            KillXpNotice.Tick();
            AdventureJournal.Tick();
            StatsWindow.Tick();
            EpicLootShardWindow.Tick();
            VneiSearch.Tick();
            CraftMaterials.Tick();
            StatValues.Tick();
            ChatHide.Tick();
            ChatWindowLayout.Tick();
            ChatPrefix.Tick();
            ChatBackground.Tick();
            ChatFont.Tick();
            BossStars.Tick();
            WishboneKeySlot.Tick();
            QuiverMeshToggle.Tick();
            if (Enabled == null || !Enabled.Value)
            {
                HudLayout.Restore();
                return;
            }

            HudLayout.Apply();
            ActionSlotXpBar.Tick();
        }

        static T ConfigDefault<T>(
            ConfigFile config,
            string section,
            string key,
            string legacySection,
            string legacyKey,
            T fallback)
        {
            if (config.TryGetEntry(section, key, out ConfigEntry<T> current))
            {
                return current.Value;
            }

            if (config.TryGetEntry(legacySection, legacyKey, out ConfigEntry<T> legacy))
            {
                return legacy.Value;
            }

            return fallback;
        }
    }
}
