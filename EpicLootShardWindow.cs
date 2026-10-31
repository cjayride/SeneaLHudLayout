using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Opens Epic Loot shard socketing in a dedicated window when SeneaL CustomUI hides
    /// the under-inventory socket row. Delegates add/remove to Epic Loot 0.15.4 ShardUI.
    /// </summary>
    static class EpicLootShardWindow
    {
        const string EpicGuid = "randyknapp.mods.epicloot";
        const string WindowName = "SeneaLHudLayout_ShardSockets";
        const string ButtonName = "SeneaLHudLayout_Sockets";
        const string HostName = "socketHost";
        const int FrontSort = 27000;
        const int DragSort = 28000;

        static bool _looked;
        static bool _available;
        static bool _loggedMissing;
        static bool _loggedNoApi;
        static bool _hosting;

        static Type _shardUi;
        static Type _panelType;
        static Type _overlayInput;
        static Type _itemExt;
        static MethodInfo _open;
        static MethodInfo _closeGui;
        static MethodInfo _close;
        static MethodInfo _isOpen;
        static MethodInfo _hasSockets;
        static MethodInfo _blocked;
        static MethodInfo _isSlotGrid;
        static MethodInfo _getMagic;
        static MethodInfo _displayName;
        static MethodInfo _getRarity;
        static MethodInfo _magicHasSockets;
        static MethodInfo _panelGet;
        static FieldInfo _panelRoot;
        static FieldInfo _guiHover;
        static FieldInfo _elItem;
        static MethodInfo _gatherCursor;
        static FieldInfo _seneaLPressItem;
        static MethodInfo _resetUse;

        static ItemDrop.ItemData _cachedHover;
        static Inventory _cachedInv;
        static bool _allowEpicClose;
        static bool _wantHost;
        static bool _openedThisFrame;
        static bool _socketsButtonRemoved;
        static Sprite _slotFrame;

        static GameObject _window;
        static Image _panelFill;
        static TextMeshProUGUI _title;
        static TextMeshProUGUI _hint;
        static RectTransform _host;
        static Transform _storedParent;
        static ItemDrop.ItemData _item;

        public static void Apply(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey(EpicGuid))
            {
                if (!_loggedMissing)
                {
                    _loggedMissing = true;
                    Debug.Log("[SeneaLHudLayout] Epic Loot is not loaded; shard socket window is inactive.");
                }

                return;
            }

            if (!Lookup())
            {
                if (!_loggedNoApi)
                {
                    _loggedNoApi = true;
                    Debug.Log("[SeneaLHudLayout] Epic Loot is loaded but ShardUI.Open was not found; shard socket window is inactive.");
                }

                return;
            }

            _available = true;
            Patch(harmony, AccessTools.Method(_panelType, "Place"), null, nameof(AfterPlace));
            Patch(harmony, AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.Hide)), nameof(BeforeHide), null);
            Patch(harmony, AccessTools.Method(_overlayInput, "TryGetPressTarget"), nameof(BeforeTryGetPress), null);
            Patch(harmony, AccessTools.Method(_shardUi, "Close", new[] { typeof(InventoryGui) }), nameof(BeforeEpicClose), null);
            Patch(harmony, AccessTools.Method(_shardUi, "Open", new[] { typeof(InventoryGui), typeof(ItemDrop.ItemData) }), nameof(BeforeEpicOpen), nameof(AfterEpicOpen));
            Patch(harmony, AccessTools.Method(typeof(InventoryGui), "OnEnterElement"), null, nameof(AfterEnterElement));
            Patch(harmony, AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UseItem)), nameof(BeforeUseItem), null);
        }

        static void Patch(Harmony harmony, MethodInfo original, string prefix, string postfix)
        {
            if (harmony == null || original == null)
            {
                return;
            }

            try
            {
                harmony.Patch(original,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(EpicLootShardWindow), prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(EpicLootShardWindow), postfix));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SeneaLHudLayout] Could not patch " + original.DeclaringType.Name + "." + original.Name + ": " + ex.Message);
            }
        }

        static bool Lookup()
        {
            if (_looked)
            {
                return _open != null;
            }

            _looked = true;
            _shardUi = AccessTools.TypeByName("EpicLoot.ShardStones.ShardUI");
            _panelType = AccessTools.TypeByName("EpicLoot.ShardStones.ShardSlotPanel");
            _overlayInput = AccessTools.TypeByName("EpicLoot.ShardStones.SocketOverlayInput");
            _itemExt = AccessTools.TypeByName("EpicLoot.ItemDataExtensions");
            Type magic = AccessTools.TypeByName("EpicLoot.MagicItem");
            _open = AccessTools.Method(_shardUi, "Open", new[] { typeof(InventoryGui), typeof(ItemDrop.ItemData) });
            _closeGui = AccessTools.Method(_shardUi, "Close", new[] { typeof(InventoryGui) });
            _close = AccessTools.Method(_shardUi, "Close", Type.EmptyTypes);
            _isOpen = AccessTools.Method(_shardUi, "get_IsOpen");
            _isSlotGrid = AccessTools.Method(_shardUi, "IsSlotGrid", new[] { typeof(InventoryGrid) });
            _hasSockets = AccessTools.Method(_overlayInput, "HasSockets", new[] { typeof(ItemDrop.ItemData) });
            _blocked = AccessTools.Method(_overlayInput, "IsBlockedFor", new[] { typeof(ItemDrop.ItemData), typeof(bool) });
            _getMagic = AccessTools.Method(_itemExt, "GetMagicItem", new[] { typeof(ItemDrop.ItemData) });
            _displayName = AccessTools.Method(_itemExt, "GetDisplayName", new[] { typeof(ItemDrop.ItemData) });
            _getRarity = AccessTools.Method(_itemExt, "GetRarity", new[] { typeof(ItemDrop.ItemData) });
            _magicHasSockets = AccessTools.Method(magic, "HasSockets");
            _panelGet = AccessTools.Method(_panelType, "Get", new[] { typeof(InventoryGui) });
            _panelRoot = AccessTools.Field(_panelType, "_root");
            _elItem = AccessTools.Field(_shardUi, "_item");
            _guiHover = AccessTools.Field(typeof(InventoryGui), "m_hoveredItem")
                ?? AccessTools.Field(typeof(InventoryGui), "m_hoverItem");
            Type selection = AccessTools.TypeByName("SeneaLUI.Inv.Selection");
            _gatherCursor = AccessTools.Method(selection, "GatherAtCursor");
            _seneaLPressItem = AccessTools.Field(selection, "_pItem");
            _resetUse = AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetButtonStatus), new[] { typeof(string) });
            return _open != null;
        }

        static bool FeatureOn()
        {
            return _available
                && SeneaLHudLayoutPlugin.ShardSocketWindow != null
                && SeneaLHudLayoutPlugin.ShardSocketWindow.Value;
        }

        static bool BeforeTryGetPress(InventoryGui __0, ref ItemDrop.ItemData __1, ref bool __2, ref bool __result)
        {
            if (!FeatureOn() || __0 == null || !HotkeyPressed() || TradeOrRecycleOpen())
            {
                return true;
            }

            ItemDrop.ItemData found = ResolveItem(out bool container);
            if (found == null || !ItemHasSockets(found) || ItemBlocked(found, container))
            {
                return true;
            }

            __1 = found;
            __2 = false;
            __result = true;
            _wantHost = true;
            EatUse();
            return false;
        }

        static bool BeforeEpicOpen()
        {
            if (FeatureOn() && TradeOrRecycleOpen())
            {
                return false;
            }

            return true;
        }

        static bool BeforeEpicClose()
        {
            if (!FeatureOn() || !_hosting || _allowEpicClose || TradeOrRecycleOpen())
            {
                return true;
            }

            if (!HostedItemStillOwned())
            {
                return true;
            }

            return false;
        }

        static void AfterEpicOpen(ItemDrop.ItemData item)
        {
            if (!FeatureOn() || item == null || TradeOrRecycleOpen())
            {
                return;
            }

            _wantHost = true;
            Host(item);
        }

        static void AfterEnterElement(InventoryGrid __0, Vector2i __1)
        {
            if (__0 == null)
            {
                return;
            }

            _cachedHover = __0.GetItem(__1);
            _cachedInv = __0.GetInventory();
        }

        static bool BeforeUseItem(ItemDrop.ItemData item)
        {
            if (!FeatureOn() || item == null || !InventoryGui.IsVisible() || !HotkeyPressed() || TradeOrRecycleOpen())
            {
                return true;
            }

            if (!ItemHasSockets(item) || ItemBlocked(item, false))
            {
                return true;
            }

            EatUse();
            OpenFor(item);
            return false;
        }

        static void BeforeHide()
        {
            _allowEpicClose = true;
            if (_hosting)
            {
                CloseWindow(true);
            }
        }

        static void AfterPlace(object __instance)
        {
            if (!_hosting)
            {
                return;
            }

            Dock(__instance);
        }

        public static void Tick()
        {
            bool opened = _openedThisFrame;
            _openedThisFrame = false;
            if (!FeatureOn())
            {
                if (_window != null && _window.activeSelf)
                {
                    CloseWindow(true);
                }

                RemoveSocketsButton();
                return;
            }

            RemoveSocketsButton();

            bool inventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            if (!inventoryOpen || TradeOrRecycleOpen() || !HostedItemStillOwned())
            {
                if (_hosting)
                {
                    CloseWindow(true);
                }

                return;
            }

            if (_hosting)
            {
                if (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape))
                {
                    if (CanClose())
                    {
                        CloseWindow(true);
                    }

                    return;
                }

                BringToFront();
                LiftDragIcons();
                Dock(CurrentPanel());
            }

            if (opened || ConsoleIsOpen() || TextInput.IsVisible())
            {
                return;
            }

            if (HotkeyPressed() && !TradeOrRecycleOpen())
            {
                TryOpenHovered();
            }
        }

        static bool HotkeyPressed()
        {
            KeyboardShortcut hotkey = SeneaLHudLayoutPlugin.ShardSocketHotkey != null
                ? SeneaLHudLayoutPlugin.ShardSocketHotkey.Value
                : new KeyboardShortcut(KeyCode.E);
            if (hotkey.MainKey == KeyCode.None)
            {
                return false;
            }

            if (!ModifiersHeld(hotkey))
            {
                return false;
            }

            if (ZInput.GetButtonDown("Use") && (hotkey.MainKey == KeyCode.E || MainKeyIsUse(hotkey.MainKey)))
            {
                return true;
            }

            return ZInput.GetKeyDown(hotkey.MainKey, false) || Input.GetKeyDown(hotkey.MainKey);
        }

        static bool MainKeyIsUse(KeyCode key)
        {
            return key == KeyCode.E;
        }

        static bool ModifiersHeld(KeyboardShortcut hotkey)
        {
            foreach (KeyCode modifier in hotkey.Modifiers)
            {
                if (modifier != KeyCode.None && !ZInput.GetKey(modifier, false) && !Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }

        static void EatUse()
        {
            try
            {
                if (_resetUse != null)
                {
                    _resetUse.Invoke(null, new object[] { "Use" });
                }
                else
                {
                    ZInput.ResetButtonStatus("Use");
                }
            }
            catch (Exception)
            {
            }
        }

        static bool ConsoleIsOpen()
        {
            return Console.instance != null && Console.IsVisible();
        }

        static bool TradeOrRecycleOpen()
        {
            if (StoreGui.instance != null && StoreGui.IsVisible())
            {
                return true;
            }

            Type recycle = AccessTools.TypeByName("SeneaLUI.Mods.Recycle");
            if (recycle != null)
            {
                FieldInfo on = AccessTools.Field(recycle, "On");
                if (on != null && on.GetValue(null) is bool recycleOn && recycleOn)
                {
                    FieldInfo item = AccessTools.Field(recycle, "Item");
                    if (item != null && item.GetValue(null) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool HostedItemStillOwned()
        {
            if (!_hosting)
            {
                return true;
            }

            ItemDrop.ItemData item = _item ?? (_elItem != null ? _elItem.GetValue(null) as ItemDrop.ItemData : null);
            Player player = Player.m_localPlayer;
            return item != null && player != null && player.GetInventory() != null && player.GetInventory().ContainsItem(item);
        }

        static bool IsPlayerInventoryTransform(RectTransform root, object panel)
        {
            if (root == null)
            {
                return false;
            }

            string name = root.name;
            if (name.IndexOf("EpicLoot_Shard", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("ShardSlots", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return false;
            }

            InventoryGrid playerGrid = gui.m_playerGrid;
            if (playerGrid != null)
            {
                Transform grid = playerGrid.transform;
                if (root == grid || root.IsChildOf(grid) || grid.IsChildOf(root))
                {
                    return true;
                }
            }

            FieldInfo playerPanel = AccessTools.Field(_panelType, "_playerPanel");
            if (playerPanel != null && panel != null)
            {
                var panelRt = playerPanel.GetValue(panel) as Transform;
                if (root == panelRt)
                {
                    return true;
                }
            }

            return name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0
                && name.IndexOf("Shard", StringComparison.OrdinalIgnoreCase) < 0
                && name.IndexOf("Socket", StringComparison.OrdinalIgnoreCase) < 0;
        }

        static bool CanClose()
        {
            if (SeneaLHudLayoutPlugin.ShardWindowRequireConfirmClose == null
                || !SeneaLHudLayoutPlugin.ShardWindowRequireConfirmClose.Value)
            {
                return true;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return true;
            }

            var drag = AccessTools.Field(typeof(InventoryGui), "m_dragGo")?.GetValue(gui) as GameObject;
            return drag == null;
        }

        static void TryOpenHovered()
        {
            ItemDrop.ItemData item = ResolveItem(out bool container);
            if (item == null || !ItemHasSockets(item) || ItemBlocked(item, container))
            {
                return;
            }

            OpenFor(item);
        }

        static void OpenFor(ItemDrop.ItemData item)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || item == null || TradeOrRecycleOpen())
            {
                return;
            }

            _wantHost = true;
            Host(item);
            try
            {
                _open.Invoke(null, new object[] { gui, item });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SeneaLHudLayout] Epic Loot ShardUI.Open failed: " + ex.Message);
                CloseWindow(false);
                return;
            }

            Dock(CurrentPanel());
        }

        static void Host(ItemDrop.ItemData item)
        {
            EnsureWindow();
            _item = item;
            _hosting = true;
            _openedThisFrame = true;
            _allowEpicClose = false;
            _window.SetActive(true);
            BringToFront();
            PaintTitle(item);
            Dock(CurrentPanel());
        }

        static void CloseWindow(bool callEpic)
        {
            InventoryGui gui = InventoryGui.instance;
            _allowEpicClose = true;
            if (callEpic)
            {
                try
                {
                    if (_closeGui != null && gui != null)
                    {
                        _closeGui.Invoke(null, new object[] { gui });
                    }
                    else if (_close != null)
                    {
                        _close.Invoke(null, null);
                    }
                }
                catch (Exception)
                {
                }
            }

            RestoreParent();
            _hosting = false;
            _wantHost = false;
            _item = null;
            if (_window != null)
            {
                _window.SetActive(false);
            }
        }

        static object CurrentPanel()
        {
            InventoryGui gui = InventoryGui.instance;
            if (_panelGet == null || gui == null)
            {
                return null;
            }

            try
            {
                return _panelGet.Invoke(null, new object[] { gui });
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void Dock(object panel)
        {
            if (!_hosting || !_wantHost || _host == null || panel == null || _panelRoot == null)
            {
                return;
            }

            var root = _panelRoot.GetValue(panel) as RectTransform;
            if (root == null || IsPlayerInventoryTransform(root, panel))
            {
                return;
            }

            if (root.parent != _host)
            {
                if (_storedParent == null)
                {
                    _storedParent = root.parent;
                }

                root.SetParent(_host, false);
            }

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
            root.gameObject.SetActive(true);
            HideVanillaChrome(root);
        }

        static void HideVanillaChrome(RectTransform root)
        {
            SetChildActive(root, "Bkg", false);
            SetChildActive(root, "Title", false);
            SetChildActive(root, "selected_frame", false);
            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = false;
            }

            InventoryGrid[] grids = root.GetComponentsInChildren<InventoryGrid>(true);
            for (int i = 0; i < grids.Length; i++)
            {
                Image gridImage = grids[i].GetComponent<Image>();
                if (gridImage != null)
                {
                    gridImage.enabled = false;
                }

                FrameSlotCells(grids[i]);
            }
        }

        static void FrameSlotCells(InventoryGrid grid)
        {
            FieldInfo elementsField = AccessTools.Field(typeof(InventoryGrid), "m_elements");
            if (!(elementsField?.GetValue(grid) is IList elements))
            {
                return;
            }

            for (int i = 0; i < elements.Count; i++)
            {
                RectTransform rect = ElementRect(elements[i]);
                if (rect == null || rect.Find("SeneaLHudLayout_SlotBorder") != null)
                {
                    continue;
                }

                var go = new GameObject("SeneaLHudLayout_SlotBorder", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(rect, false);
                go.transform.SetAsLastSibling();
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(-2f, -2f);
                rt.offsetMax = new Vector2(2f, 2f);
                var image = go.GetComponent<Image>();
                image.sprite = SlotFrameSprite();
                image.type = Image.Type.Sliced;
                image.fillCenter = false;
                image.color = new Color(0.62f, 0.48f, 0.28f, 1f);
                image.raycastTarget = false;
            }
        }

        static Sprite SlotFrameSprite()
        {
            if (_slotFrame != null)
            {
                return _slotFrame;
            }

            const int size = 8;
            const int edge = 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color gold = Color.white;
            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < edge || y < edge || x >= size - edge || y >= size - edge;
                    tex.SetPixel(x, y, border ? gold : clear);
                }
            }

            tex.Apply();
            _slotFrame = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(edge, edge, edge, edge));
            return _slotFrame;
        }

        static void ShowVanillaChrome(RectTransform root)
        {
            SetChildActive(root, "Bkg", true);
            SetChildActive(root, "Title", true);
            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = true;
            }

            InventoryGrid[] grids = root.GetComponentsInChildren<InventoryGrid>(true);
            for (int i = 0; i < grids.Length; i++)
            {
                Image gridImage = grids[i].GetComponent<Image>();
                if (gridImage != null)
                {
                    gridImage.enabled = true;
                }
            }
        }

        static void SetChildActive(Transform root, string name, bool active)
        {
            Transform child = root.Find(name);
            if (child != null && child.gameObject.activeSelf != active)
            {
                child.gameObject.SetActive(active);
            }
        }

        static void RestoreParent()
        {
            object panel = CurrentPanel();
            if (panel == null || _panelRoot == null || _storedParent == null)
            {
                _storedParent = null;
                return;
            }

            var root = _panelRoot.GetValue(panel) as RectTransform;
            if (root != null && root.parent == _host)
            {
                ShowVanillaChrome(root);
                root.SetParent(_storedParent, false);
            }

            _storedParent = null;
        }

        static bool ItemHasSockets(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                if (_hasSockets != null && (bool)_hasSockets.Invoke(null, new object[] { item }))
                {
                    return true;
                }

                if (_getMagic != null && _magicHasSockets != null)
                {
                    object magic = _getMagic.Invoke(null, new object[] { item });
                    if (magic != null)
                    {
                        return (bool)_magicHasSockets.Invoke(magic, null);
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        static bool ItemBlocked(ItemDrop.ItemData item, bool container)
        {
            if (_blocked == null)
            {
                return container;
            }

            try
            {
                return (bool)_blocked.Invoke(null, new object[] { item, container });
            }
            catch (Exception)
            {
                return container;
            }
        }

        static ItemDrop.ItemData ResolveItem(out bool container)
        {
            container = false;
            InventoryGui gui = InventoryGui.instance;
            Player player = Player.m_localPlayer;
            if (gui == null || player == null)
            {
                return null;
            }

            Inventory playerInv = player.GetInventory();
            ItemDrop.ItemData seneaL = SeneaLCursorItem();
            if (seneaL != null)
            {
                container = playerInv != null && !playerInv.ContainsItem(seneaL);
                return seneaL;
            }

            if (_cachedHover != null && (_cachedInv == null || _cachedInv.ContainsItem(_cachedHover)))
            {
                container = playerInv != null && _cachedInv != null && _cachedInv != playerInv;
                return _cachedHover;
            }

            if (_guiHover != null)
            {
                var hovered = _guiHover.GetValue(gui) as ItemDrop.ItemData;
                if (hovered != null && ItemHasSockets(hovered))
                {
                    container = playerInv != null && !playerInv.ContainsItem(hovered);
                    return hovered;
                }
            }

            InventoryGrid[] grids = gui.GetComponentsInChildren<InventoryGrid>(true);
            for (int i = 0; i < grids.Length; i++)
            {
                InventoryGrid grid = grids[i];
                if (grid == null || !grid.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (_isSlotGrid != null)
                {
                    try
                    {
                        if ((bool)_isSlotGrid.Invoke(null, new object[] { grid }))
                        {
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (!TryHover(grid, out ItemDrop.ItemData item))
                {
                    continue;
                }

                Inventory inv = grid.GetInventory();
                container = playerInv != null && inv != playerInv;
                return item;
            }

            return null;
        }

        static ItemDrop.ItemData SeneaLCursorItem()
        {
            try
            {
                _gatherCursor?.Invoke(null, null);
                if (_seneaLPressItem != null)
                {
                    return _seneaLPressItem.GetValue(null) as ItemDrop.ItemData;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        static bool TryHover(InventoryGrid grid, out ItemDrop.ItemData item)
        {
            item = null;
            Inventory inv = grid.GetInventory();
            if (inv == null)
            {
                return false;
            }

            MethodInfo getHovered = AccessTools.Method(typeof(InventoryGrid), "GetHoveredElement");
            object hovered = getHovered == null ? null : getHovered.Invoke(grid, null);
            if (hovered != null)
            {
                Vector2i pos = ElementPos(hovered);
                item = grid.GetItem(pos);
                if (item != null && inv.ContainsItem(item))
                {
                    return true;
                }
            }

            var elementsField = AccessTools.Field(typeof(InventoryGrid), "m_elements");
            if (!(elementsField?.GetValue(grid) is IList elements))
            {
                return false;
            }

            Vector2 mouse = Input.mousePosition;
            for (int i = 0; i < elements.Count; i++)
            {
                object raw = elements[i];
                if (raw == null)
                {
                    continue;
                }

                RectTransform rect = ElementRect(raw);
                if (rect == null)
                {
                    continue;
                }

                Canvas canvas = rect.GetComponentInParent<Canvas>();
                Camera cam = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    cam = canvas.worldCamera != null ? canvas.worldCamera : canvas.rootCanvas.worldCamera;
                }

                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, mouse, cam))
                {
                    continue;
                }

                Vector2i pos = ElementPos(raw);
                item = grid.GetItem(pos);
                if (item != null && inv.ContainsItem(item))
                {
                    return true;
                }
            }

            return false;
        }

        static RectTransform ElementRect(object element)
        {
            var button = AccessTools.Field(element.GetType(), "m_button")?.GetValue(element) as Component;
            if (button != null)
            {
                return button.transform as RectTransform;
            }

            return AccessTools.Field(element.GetType(), "m_touchRect")?.GetValue(element) as RectTransform
                ?? (element as Component)?.transform as RectTransform;
        }

        static Vector2i ElementPos(object element)
        {
            PropertyInfo prop = AccessTools.Property(element.GetType(), "Position");
            if (prop != null && prop.GetValue(element, null) is Vector2i pos)
            {
                return pos;
            }

            FieldInfo field = AccessTools.Field(element.GetType(), "m_pos");
            if (field != null && field.GetValue(element) is Vector2i stored)
            {
                return stored;
            }

            return new Vector2i();
        }

        static void PaintTitle(ItemDrop.ItemData item)
        {
            string name = item.m_shared.m_name;
            string rarity = "";
            try
            {
                if (_displayName != null)
                {
                    string shown = _displayName.Invoke(null, new object[] { item }) as string;
                    if (!string.IsNullOrEmpty(shown))
                    {
                        name = shown;
                    }
                }

                if (_getRarity != null)
                {
                    object value = _getRarity.Invoke(null, new object[] { item });
                    if (value != null)
                    {
                        rarity = value.ToString();
                    }
                }
            }
            catch (Exception)
            {
            }

            if (Localization.instance != null)
            {
                name = Localization.instance.Localize(name);
            }

            if (_title != null)
            {
                _title.text = string.IsNullOrEmpty(rarity) ? name : name + "  ·  " + rarity;
            }

            if (_hint != null)
            {
                _hint.text = "Drag shard stones or runes into empty sockets.";
            }
        }

        static void RemoveSocketsButton()
        {
            if (_socketsButtonRemoved)
            {
                return;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return;
            }

            Transform existing = null;
            bool inventoryReady = false;
            Transform[] transforms = gui.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == ButtonName)
                {
                    existing = transforms[i];
                }

                if (transforms[i].name == "tabs")
                {
                    inventoryReady = true;
                }
            }

            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
                _socketsButtonRemoved = true;
                return;
            }

            if (inventoryReady)
            {
                _socketsButtonRemoved = true;
            }
        }

        static void EnsureWindow()
        {
            if (_window != null)
            {
                return;
            }

            Transform parent = BestParent() ?? InventoryGui.instance.transform;
            var go = new GameObject(WindowName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            go.transform.SetParent(parent, false);
            ApplyFrontCanvas(go.GetComponent<Canvas>());
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 280f);
            var border = go.GetComponent<Image>();
            border.sprite = CharacterExtras.Plate();
            border.color = new Color(0.62f, 0.48f, 0.28f, 1f);
            border.raycastTarget = true;
            _window = go;

            var fillGo = new GameObject("fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(rt, false);
            fillGo.transform.SetAsFirstSibling();
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            _panelFill = fillGo.GetComponent<Image>();
            _panelFill.sprite = CharacterExtras.Plate();
            _panelFill.color = new Color(0.08f, 0.07f, 0.05f, 0.96f);
            _panelFill.raycastTarget = true;

            _title = AddLabel(rt, "Shard sockets", 18f, new Vector2(0f, -14f), FontStyles.Bold);
            _hint = AddLabel(rt, "", 13f, new Vector2(0f, -40f), FontStyles.Normal);
            _hint.color = new Color(0.78f, 0.72f, 0.6f, 1f);
            AddClose(rt);

            var hostGo = new GameObject(HostName, typeof(RectTransform));
            hostGo.transform.SetParent(rt, false);
            _host = (RectTransform)hostGo.transform;
            _host.anchorMin = new Vector2(0f, 0f);
            _host.anchorMax = new Vector2(1f, 1f);
            _host.offsetMin = new Vector2(16f, 16f);
            _host.offsetMax = new Vector2(-16f, -72f);
            go.SetActive(false);
        }

        static Transform BestParent()
        {
            Type hudRoot = AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
            FieldInfo rootField = AccessTools.Field(hudRoot, "Root");
            var seneaL = rootField != null ? rootField.GetValue(null) as Transform : null;
            if (seneaL != null)
            {
                return seneaL;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return null;
            }

            Canvas canvas = gui.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform : gui.transform;
        }

        static void ApplyFrontCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            Canvas inherit = null;
            Transform parent = canvas.transform.parent;
            if (parent != null)
            {
                inherit = parent.GetComponentInParent<Canvas>();
            }

            if (inherit != null)
            {
                canvas.renderMode = inherit.renderMode;
                canvas.worldCamera = inherit.worldCamera;
                canvas.pixelPerfect = inherit.pixelPerfect;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = FrontSortOrder(inherit);
        }

        static int FrontSortOrder(Canvas inherit)
        {
            int order = FrontSort;
            if (inherit != null && inherit.sortingOrder + 20 > order && inherit.sortingOrder < DragSort)
            {
                order = inherit.sortingOrder + 20;
            }

            if (order >= DragSort)
            {
                order = DragSort - 50;
            }

            return order;
        }

        static void LiftDragIcons()
        {
            Type selection = AccessTools.TypeByName("SeneaLUI.Inv.Selection");
            FieldInfo layerField = AccessTools.Field(selection, "_layer");
            var layer = layerField != null ? layerField.GetValue(null) as Component : null;
            RaiseCanvas(layer != null ? layer.gameObject : null, DragSort, false);

            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return;
            }

            FieldInfo dragField = AccessTools.Field(typeof(InventoryGui), "m_dragGo");
            var dragGo = dragField != null ? dragField.GetValue(gui) as GameObject : null;
            RaiseCanvas(dragGo, DragSort, false);
        }

        static void RaiseCanvas(GameObject go, int order, bool addRaycaster)
        {
            if (go == null)
            {
                return;
            }

            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
                Canvas inherit = go.GetComponentInParent<Canvas>();
                if (inherit != null && inherit != canvas)
                {
                    canvas.renderMode = inherit.renderMode;
                    canvas.worldCamera = inherit.worldCamera;
                    canvas.pixelPerfect = inherit.pixelPerfect;
                }
            }

            canvas.overrideSorting = true;
            if (canvas.sortingOrder < order)
            {
                canvas.sortingOrder = order;
            }

            if (addRaycaster && go.GetComponent<GraphicRaycaster>() == null)
            {
                go.AddComponent<GraphicRaycaster>();
            }
        }

        static void BringToFront()
        {
            if (_window == null)
            {
                return;
            }

            Transform parent = BestParent();
            if (parent != null && _window.transform.parent != parent)
            {
                _window.transform.SetParent(parent, false);
            }

            _window.transform.SetAsLastSibling();
            ApplyFrontCanvas(_window.GetComponent<Canvas>());
            LiftDragIcons();
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
            var label = AddLabel(rt, "X", 16f, Vector2.zero, FontStyles.Bold);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (CanClose())
                {
                    CloseWindow(true);
                }
            });
        }

        static TextMeshProUGUI AddLabel(RectTransform parent, string text, float size, Vector2 pos, FontStyles style)
        {
            var go = new GameObject("label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-56f, size + 10f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.93f, 0.84f, 0.62f);
            label.raycastTarget = false;
            TmpFont.Apply(label);
            return label;
        }
    }
}
