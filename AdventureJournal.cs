using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Character-panel button that lists active EpicLoot bounties and treasure maps.
    /// </summary>
    static class AdventureJournal
    {
        const string ButtonName = "SeneaLHudLayout_Quests";
        const string WindowName = "SeneaLHudLayout_Adventure";

        static Transform _tabs;
        static Button _button;
        static GameObject _window;
        static RectTransform _list;
        static TextMeshProUGUI _bountyTab;
        static TextMeshProUGUI _counts;
        static TextMeshProUGUI _treasureTab;
        static bool _bounties = true;
        static bool _lookedUp;
        static MethodInfo _saveData;
        static MethodInfo _inProgress;
        static MethodInfo _claimable;
        static MethodInfo _unfound;
        static MethodInfo _bountyName;
        static MethodInfo _monsterName;
        static MethodInfo _config;
        static FieldInfo _treasureConfig;
        static FieldInfo _biomeInfo;

        static bool _returnToJournal;
        static bool _reopen;
        static bool _allowMap;
        static MethodInfo _adventureEnabled;

        public static void Apply(Harmony harmony)
        {
            MethodInfo show = AccessTools.Method(typeof(Minimap), nameof(Minimap.ShowPointOnMap), new[] { typeof(Vector3) });
            MethodInfo mode = AccessTools.Method(typeof(Minimap), nameof(Minimap.SetMapMode));
            harmony.Patch(show, prefix: new HarmonyMethod(typeof(AdventureJournal), nameof(SkipMerchantMap)));
            harmony.Patch(mode, postfix: new HarmonyMethod(typeof(AdventureJournal), nameof(AfterMapMode)));
        }

        static bool SkipMerchantMap()
        {
            if (_allowMap)
            {
                _allowMap = false;
                _returnToJournal = true;
                return true;
            }

            if (SeneaLHudLayoutPlugin.KeepMapClosedOnAccept != null
                && SeneaLHudLayoutPlugin.KeepMapClosedOnAccept.Value
                && (FromMerchant() || TraderOpen()))
            {
                return false;
            }

            return true;
        }

        static bool FromMerchant()
        {
            var trace = new StackTrace();
            for (int i = 0; i < trace.FrameCount; i++)
            {
                Type type = trace.GetFrame(i).GetMethod()?.DeclaringType;
                while (type != null)
                {
                    string name = type.FullName ?? type.Name;
                    if (name.Contains("BountiesAdventureFeature") || name.Contains("TreasureMapsAdventureFeature"))
                    {
                        return true;
                    }

                    type = type.DeclaringType;
                }
            }

            return false;
        }

        static bool TraderOpen()
        {
            StoreGui store = StoreGui.instance;
            if (store == null)
            {
                return false;
            }

            var root = AccessTools.Field(typeof(StoreGui), "m_root")?.GetValue(store) as GameObject;
            return root != null && root.activeInHierarchy;
        }

        static void AfterMapMode(Minimap.MapMode mode)
        {
            if (!_returnToJournal || mode == Minimap.MapMode.Large)
            {
                return;
            }

            _returnToJournal = false;
            _reopen = true;
        }

        public static void Tick()
        {
            if (_reopen && InventoryGui.instance != null)
            {
                _reopen = false;
                InventoryGui.instance.Show(null, 1);
                EnsureWindow();
                _window.SetActive(true);
                _window.transform.SetAsLastSibling();
            }

            if (!EnsureButton())
            {
                return;
            }

            PlaceButton();
            bool inventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool show = inventoryOpen && _tabs.gameObject.activeInHierarchy && AdventureOn();
            if (_button.gameObject.activeSelf != show)
            {
                _button.gameObject.SetActive(show);
            }

            if (_window != null && _window.activeSelf && !inventoryOpen && !_returnToJournal)
            {
                _window.SetActive(false);
            }
        }

        static bool _adventureChecked;

        static bool AdventureOn()
        {
            if (!_adventureChecked)
            {
                _adventureChecked = true;
                Type epic = AccessTools.TypeByName("EpicLoot.EpicLoot");
                _adventureEnabled = AccessTools.Method(epic, "IsAdventureModeEnabled");
            }

            if (_adventureEnabled == null)
            {
                return false;
            }

            try
            {
                return (bool)_adventureEnabled.Invoke(null, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool EnsureButton()
        {
            if (_button != null)
            {
                return true;
            }

            if (_tabs == null)
            {
                InventoryGui gui = InventoryGui.instance;
                if (gui == null)
                {
                    return false;
                }

                Transform[] transforms = gui.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == "tabs" && transforms[i].Find("tab0") != null)
                    {
                        _tabs = transforms[i];
                        break;
                    }
                }
            }

            if (_tabs == null || !(_tabs.Find("gear") is RectTransform gear))
            {
                return false;
            }

            Transform existing = _tabs.Find(ButtonName);
            if (existing != null)
            {
                _button = existing.GetComponent<Button>();
                return _button != null;
            }

            var go = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_tabs, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            float width = 88f;
            float height = gear.sizeDelta.y > 8f ? gear.sizeDelta.y : 28f;
            rt.sizeDelta = new Vector2(width, height);

            var image = go.GetComponent<Image>();
            image.sprite = CharacterExtras.Plate();
            image.raycastTarget = true;
            CharacterExtras.Frame(image);
            _button = go.GetComponent<Button>();
            _button.targetGraphic = image;
            _button.onClick.AddListener(Toggle);

            var labelGo = new GameObject("t", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "Quests";
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Normal;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.93f, 0.82f, 0.52f, 1f);
            label.raycastTarget = false;
            CopyFont(gear, label);
            PlaceButton();
            return true;
        }

        static void PlaceButton()
        {
            if (_button == null || !(_tabs.Find("gear") is RectTransform gear))
            {
                return;
            }

            RectTransform anchor = _tabs.Find("SeneaLHudLayout_Talents") as RectTransform;
            if (anchor == null || !anchor.gameObject.activeInHierarchy)
            {
                anchor = gear;
            }

            var rt = (RectTransform)_button.transform;
            rt.anchoredPosition = new Vector2(anchor.anchoredPosition.x - anchor.sizeDelta.x - 8f, anchor.anchoredPosition.y);
        }

        static void Toggle()
        {
            if (_window != null && _window.activeSelf)
            {
                _window.SetActive(false);
                return;
            }

            EnsureWindow();
            _bounties = true;
            _window.SetActive(true);
            _window.transform.SetAsLastSibling();
            Rebuild();
        }

        static void EnsureWindow()
        {
            if (_window != null)
            {
                return;
            }

            Transform parent = InventoryGui.instance.transform;
            var go = new GameObject(WindowName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 460f);
            go.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.05f, 0.96f);
            _window = go;

            AddLabel(rt, "Bounties and treasure maps", 18f, new Vector2(0f, -16f), 20f, FontStyles.Bold);
            AddTab(rt, "Bounties", new Vector2(16f, -48f), true);
            AddTab(rt, "Treasure", new Vector2(150f, -48f), false);
            _counts = AddLabel(rt, "", 0f, new Vector2(-16f, -48f), 15f, FontStyles.Normal);
            _counts.alignment = TextAlignmentOptions.MidlineRight;
            _counts.rectTransform.anchorMin = new Vector2(1f, 1f);
            _counts.rectTransform.anchorMax = new Vector2(1f, 1f);
            _counts.rectTransform.pivot = new Vector2(1f, 1f);
            _counts.rectTransform.sizeDelta = new Vector2(260f, 28f);
            AddClose(rt);

            var viewportGo = new GameObject("view", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(rt, false);
            var viewport = (RectTransform)viewportGo.transform;
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(12f, 12f);
            viewport.offsetMax = new Vector2(-12f, -88f);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = true;

            var contentGo = new GameObject("list", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport, false);
            _list = (RectTransform)contentGo.transform;
            _list.anchorMin = new Vector2(0f, 1f);
            _list.anchorMax = new Vector2(1f, 1f);
            _list.pivot = new Vector2(0.5f, 1f);
            _list.anchoredPosition = Vector2.zero;
            _list.sizeDelta = new Vector2(0f, 0f);
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            var fit = contentGo.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            go.SetActive(false);
        }

        static void AddTab(RectTransform window, string text, Vector2 pos, bool bounties)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(window, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(124f, 28f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.17f, 0.1f, 1f);
            var label = AddLabel(rt, text, 0f, Vector2.zero, 15f, FontStyles.Normal);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            if (bounties)
            {
                _bountyTab = label;
            }
            else
            {
                _treasureTab = label;
            }

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                _bounties = bounties;
                Rebuild();
            });
        }

        static void AddClose(RectTransform window)
        {
            var go = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(window, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12f, -12f);
            rt.sizeDelta = new Vector2(28f, 28f);
            go.GetComponent<Image>().color = new Color(0.28f, 0.14f, 0.1f, 1f);
            AddLabel(rt, "X", 0f, Vector2.zero, 16f, FontStyles.Bold).rectTransform.anchorMin = Vector2.zero;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            go.GetComponent<Button>().onClick.AddListener(() => _window.SetActive(false));
        }

        static void Rebuild()
        {
            PaintTabs();
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_list.GetChild(i).gameObject);
            }

            if (!Lookup())
            {
                Row("EpicLoot is not loaded.", "", null);
                return;
            }

            object save = _saveData.Invoke(null, new object[] { Player.m_localPlayer });
            if (save == null)
            {
                Row("No adventure data on this character.", "", null);
                return;
            }

            IList bounties = Join(AsList(Invoke(_inProgress, save)), AsList(Invoke(_claimable, save)));
            IList maps = AsList(Invoke(_unfound, save));
            if (_counts != null)
            {
                _counts.text = "Bounties: " + Count(bounties) + "/" + BountyCap()
                    + "    Treasure: " + Count(maps);
            }

            IList items = _bounties ? bounties : maps;
            if (items == null || items.Count == 0)
            {
                Row(_bounties ? "No active bounties." : "No active treasure maps.", "", null);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                object item = items[i];
                if (_bounties)
                {
                    Row(BountyText(item), "Map", () => ShowMap(ReadVector(Field(item, "Position"))));
                }
                else
                {
                    Row(TreasureText(item), "Map", () => ShowMap(ReadVector(Field(item, "Position"))));
                }
            }
        }

        static object Invoke(MethodInfo method, object target)
        {
            try
            {
                return method == null ? null : method.Invoke(target, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static IList AsList(object value)
        {
            if (value is IList list)
            {
                return list;
            }

            if (value is IEnumerable sequence)
            {
                var gathered = new ArrayList();
                foreach (object item in sequence)
                {
                    gathered.Add(item);
                }

                return gathered;
            }

            return null;
        }

        static int Count(IList items)
        {
            return items == null ? 0 : items.Count;
        }

        static int BountyCap()
        {
            if (!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out var plugin) || plugin.Instance == null)
            {
                return 0;
            }

            ConfigFile config = plugin.Instance.Config;
            string[] sections = { "5 - Adventure", "Bounty Management", "Adventure" };
            for (int i = 0; i < sections.Length; i++)
            {
                if (config.TryGetEntry(sections[i], "Max Bounties Per Player", out ConfigEntry<int> entry))
                {
                    return entry.Value;
                }
            }

            return 0;
        }

        static void PaintTabs()
        {
            if (_bountyTab != null)
            {
                _bountyTab.color = _bounties ? new Color(1f, 0.86f, 0.45f) : new Color(0.75f, 0.7f, 0.6f);
            }

            if (_treasureTab != null)
            {
                _treasureTab.color = _bounties ? new Color(0.75f, 0.7f, 0.6f) : new Color(1f, 0.86f, 0.45f);
            }
        }

        static string BountyText(object bounty)
        {
            string name = _bountyName != null ? _bountyName.Invoke(null, new object[] { bounty }) as string : null;
            if (string.IsNullOrEmpty(name))
            {
                name = Field(bounty, "TargetName") as string;
            }

            object target = Field(bounty, "Target");
            int stars = target == null ? 0 : Convert.ToInt32(Field(target, "Level") ?? 0);
            string monster = target == null ? "" : Field(target, "MonsterID") as string;
            var text = new StringBuilder();
            text.Append(Localize(name));
            if (!string.IsNullOrEmpty(monster))
            {
                text.Append("  (").Append(Monster(monster)).Append(')');
            }

            text.Append("\nZone: ").Append(Zone(Field(bounty, "Biome")));
            text.Append("\nStars: ").Append(stars);
            text.Append("\nLoot: ").Append(Rewards(bounty));
            object adds = Field(bounty, "Adds");
            if (adds is IList list && list.Count > 0)
            {
                text.Append("\nAdds: ");
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }

                    object add = list[i];
                    text.Append(Monster(Field(add, "MonsterID") as string));
                    text.Append(" x").Append(Convert.ToInt32(Field(add, "Count") ?? 1));
                    text.Append(" (").Append(Convert.ToInt32(Field(add, "Level") ?? 0)).Append(" stars)");
                }
            }

            return text.ToString();
        }

        static string TreasureText(object map)
        {
            object biome = Field(map, "Biome");
            var text = new StringBuilder();
            text.Append("Treasure map");
            text.Append("\nZone: ").Append(Zone(biome));
            text.Append("\nLoot: ").Append(TreasureLoot(biome));
            return text.ToString();
        }

        static string Rewards(object bounty)
        {
            var parts = new List<string>();
            AddCount(parts, "Iron token", Field(bounty, "RewardIron"));
            AddCount(parts, "Gold token", Field(bounty, "RewardGold"));
            AddCount(parts, "Coins", Field(bounty, "RewardCoins"));
            return parts.Count == 0 ? "none listed" : string.Join(", ", parts);
        }

        static string TreasureLoot(object biome)
        {
            if (_config == null || _treasureConfig == null || _biomeInfo == null)
            {
                return "see the merchant";
            }

            object cfg = _config.Invoke(null, null);
            object treasure = cfg == null ? null : _treasureConfig.GetValue(cfg);
            object list = treasure == null ? null : _biomeInfo.GetValue(treasure);
            if (list is not IList biomes)
            {
                return "see the merchant";
            }

            string wanted = biome == null ? "" : biome.ToString();
            for (int i = 0; i < biomes.Count; i++)
            {
                object info = biomes[i];
                if (!string.Equals(Field(info, "Biome") as string, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = new List<string>();
                AddCount(parts, "Forest token", Field(info, "ForestTokens"));
                AddCount(parts, "Iron token", Field(info, "IronTokens"));
                AddCount(parts, "Gold token", Field(info, "GoldTokens"));
                AddCount(parts, "Coins", Field(info, "Coins"));
                return parts.Count == 0 ? "none listed" : string.Join(", ", parts);
            }

            return "see the merchant";
        }

        static void AddCount(List<string> parts, string label, object value)
        {
            int count = Convert.ToInt32(value ?? 0);
            if (count > 0)
            {
                parts.Add(label + " x" + count);
            }
        }

        static string Monster(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "unknown";
            }

            if (_monsterName != null)
            {
                string named = _monsterName.Invoke(null, new object[] { id }) as string;
                if (!string.IsNullOrEmpty(named))
                {
                    return Localize(named);
                }
            }

            return Localize("$enemy_" + id.ToLowerInvariant());
        }

        static string Zone(object biome)
        {
            if (biome == null)
            {
                return "unknown";
            }

            string name = biome.ToString();
            string localized = Localize("$biome_" + name.ToLowerInvariant());
            return localized.StartsWith("$") ? name : localized;
        }

        static string Localize(string text)
        {
            if (string.IsNullOrEmpty(text) || Localization.instance == null)
            {
                return text ?? "";
            }

            return Localization.instance.Localize(text);
        }

        static void ShowMap(Vector3 position)
        {
            if (position == Vector3.zero || Minimap.instance == null)
            {
                return;
            }

            _window.SetActive(false);
            _allowMap = true;
            _returnToJournal = true;
            if (InventoryGui.instance != null && InventoryGui.IsVisible())
            {
                InventoryGui.instance.Hide();
            }

            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            Minimap.instance.ShowPointOnMap(position);
        }

        static Vector3 ReadVector(object value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            if (value is Vector3 vector)
            {
                return vector;
            }

            MethodInfo convert = AccessTools.Method(value.GetType(), "ToVector3");
            if (convert != null && convert.ReturnType == typeof(Vector3))
            {
                return (Vector3)convert.Invoke(value, null);
            }

            float x = Convert.ToSingle(Field(value, "x") ?? Field(value, "X") ?? 0f);
            float y = Convert.ToSingle(Field(value, "y") ?? Field(value, "Y") ?? 0f);
            float z = Convert.ToSingle(Field(value, "z") ?? Field(value, "Z") ?? 0f);
            return new Vector3(x, y, z);
        }

        static IList Join(IList first, IList second)
        {
            var list = new ArrayList();
            if (first != null)
            {
                foreach (object item in first)
                {
                    list.Add(item);
                }
            }

            if (second != null)
            {
                foreach (object item in second)
                {
                    if (!list.Contains(item))
                    {
                        list.Add(item);
                    }
                }
            }

            return list;
        }

        static bool Lookup()
        {
            if (_lookedUp)
            {
                return _saveData != null;
            }

            _lookedUp = true;
            Type players = AccessTools.TypeByName("EpicLoot.Adventure.PlayerExtensions_Adventure");
            Type save = AccessTools.TypeByName("EpicLoot.Adventure.AdventureSaveData");
            Type data = AccessTools.TypeByName("EpicLoot.Adventure.AdventureDataManager");
            Type config = AccessTools.TypeByName("EpicLoot.Adventure.AdventureDataConfig");
            Type treasure = AccessTools.TypeByName("EpicLoot.Adventure.TreasureMapConfig");
            _saveData = AccessTools.Method(players, "GetAdventureSaveData", new[] { typeof(Player) });
            _inProgress = AccessTools.Method(save, "GetInProgressBounties");
            _claimable = AccessTools.Method(save, "GetClaimableBounties");
            _unfound = AccessTools.Method(save, "GetUnfoundTreasureChests");
            _bountyName = AccessTools.Method(data, "GetBountyName");
            _monsterName = AccessTools.Method(data, "GetMonsterName");
            _config = AccessTools.Method(data, "GetCFG");
            _treasureConfig = AccessTools.Field(config, "TreasureMap");
            _biomeInfo = AccessTools.Field(treasure, "BiomeInfo");
            return _saveData != null && _inProgress != null && _unfound != null;
        }

        static object Field(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            FieldInfo field = AccessTools.Field(source.GetType(), name);
            return field == null ? null : field.GetValue(source);
        }

        static void Row(string body, string button, Action click)
        {
            var go = new GameObject("row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(_list, false);
            go.GetComponent<Image>().color = new Color(0.14f, 0.12f, 0.09f, 0.9f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            go.GetComponent<LayoutElement>().minHeight = 72f;

            var textGo = new GameObject("body", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = body;
            text.fontSize = 15f;
            text.color = new Color(0.93f, 0.88f, 0.76f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            var textLayout = textGo.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.minHeight = 60f;
            CopyFont(_tabs.Find("gear") as RectTransform, text);

            if (click == null)
            {
                return;
            }

            var buttonGo = new GameObject("map", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(go.transform, false);
            buttonGo.GetComponent<LayoutElement>().preferredWidth = 72f;
            buttonGo.GetComponent<LayoutElement>().preferredHeight = 28f;
            buttonGo.GetComponent<Image>().color = new Color(0.28f, 0.22f, 0.12f, 1f);
            buttonGo.GetComponent<Button>().onClick.AddListener(() => click());
            var label = AddLabel((RectTransform)buttonGo.transform, button, 0f, Vector2.zero, 14f, FontStyles.Normal);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI AddLabel(RectTransform parent, string text, float y, Vector2 pos, float size, FontStyles style)
        {
            var go = new GameObject("label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos == Vector2.zero && y != 0f ? new Vector2(0f, -y) : pos;
            rt.sizeDelta = new Vector2(-24f, size + 8f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.93f, 0.84f, 0.62f);
            label.raycastTarget = false;
            CopyFont(_tabs != null ? _tabs.Find("gear") as RectTransform : null, label);
            return label;
        }

        static void CopyFont(RectTransform source, TextMeshProUGUI label)
        {
            TMP_Text found = source != null ? source.GetComponentInChildren<TMP_Text>(true) : null;
            if (found != null && found.font != null)
            {
                label.font = found.font;
                if (found.fontSharedMaterial != null)
                {
                    label.fontSharedMaterial = found.fontSharedMaterial;
                }

                return;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }
        }
    }
}
