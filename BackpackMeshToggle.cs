using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using SeneaLUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Small button to the right of the inventory BACKPACK section title. Toggles the 3D
    /// backpack model on the local player. The backpack slot, contents, and cape stay.
    /// </summary>
    static class BackpackMeshToggle
    {
        public static ConfigEntry<bool> ShowBackpackMesh;

        const string ButtonName = "SeneaLHudLayout_BackpackMesh";
        const float Icon = 18f;
        const float Height = 20f;
        const float Pad = 6f;
        const float Inner = 4f;

        static FieldInfo _title;
        static FieldInfo _sub;
        static FieldInfo _docked;
        static Type _containerView;
        static RectTransform _btn;
        static Image _icon;
        static TextMeshProUGUI _caption;
        static bool _appliedShow = true;
        static int _playerId;

        public static void Apply(Harmony harmony)
        {
            Type view = AccessTools.TypeByName("SeneaLUI.Inv.InventoryView");
            if (view == null)
            {
                return;
            }

            MethodInfo labels = AccessTools.Method(view, "PlaceFootLabels");
            MethodInfo tick = AccessTools.Method(view, "Tick", new[] { typeof(bool) });
            MethodInfo footer = AccessTools.Method(view, "PlaceFooter");
            MethodInfo pov = AccessTools.Method(view, "PlacePovTitles");

            Patch(harmony, labels, nameof(AfterLayout), "PlaceFootLabels");
            Patch(harmony, footer, nameof(AfterLayout), "PlaceFooter");
            Patch(harmony, pov, nameof(AfterLayout), "PlacePovTitles");
            Patch(harmony, tick, nameof(AfterTick), "Tick");

            _containerView = AccessTools.TypeByName("SeneaLUI.Inv.ContainerView");
            if (_containerView != null)
            {
                _title = AccessTools.Field(_containerView, "_title");
                _sub = AccessTools.Field(_containerView, "_sub");
                _docked = AccessTools.Field(_containerView, "_docked");
                Debug.Log(
                    "[SeneaL HUD Layout] Backpack button: ContainerView title=" +
                    (_title != null) + " sub=" + (_sub != null) + " docked=" + (_docked != null));
                foreach (MethodInfo m in _containerView.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name != "Tick" && m.Name != "Place" && !m.Name.StartsWith("Place", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Patch(harmony, m, nameof(AfterContainer), "ContainerView." + m.Name);
                }
            }

            Type vis = AccessTools.TypeByName("VisEquipment");
            if (vis != null)
            {
                foreach (MethodInfo m in vis.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!m.Name.StartsWith("Set", StringComparison.Ordinal) &&
                        !m.Name.StartsWith("Attach", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (m.Name.IndexOf("Shoulder", StringComparison.OrdinalIgnoreCase) < 0 &&
                        m.Name.IndexOf("Cape", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    Patch(harmony, m, nameof(AfterVis), m.Name);
                }
            }

            Type ab = AccessTools.TypeByName("AdventureBackpacks.AdventureBackpacks") ??
                      AccessTools.TypeByName("AdventureBackpacks.Plugin");
            if (ab == null)
            {
                return;
            }

            MethodInfo changed = AccessTools.Method(ab, "ApplyOnEquipmentChanged");
            Patch(harmony, changed, nameof(AfterVis), "ApplyOnEquipmentChanged");
        }

        static void Patch(Harmony harmony, MethodInfo method, string postfix, string label)
        {
            if (method == null)
            {
                return;
            }

            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(BackpackMeshToggle), postfix));
            }
            catch (Exception e)
            {
                Debug.LogError("[SeneaL HUD Layout] BackpackMeshToggle: " + label + " " + e.Message);
            }
        }

        static bool Shown()
        {
            return ShowBackpackMesh == null || ShowBackpackMesh.Value;
        }

        static void AfterVis()
        {
            ApplyMesh(force: true);
        }

        static void AfterLayout(object __instance)
        {
            try
            {
                PlaceFromOpenUi();
            }
            catch
            {
            }
        }

        static void AfterTick(object __instance, bool __0)
        {
            try
            {
                if (__instance == null || !__0)
                {
                    return;
                }

                PlaceFromOpenUi();
            }
            catch
            {
            }
        }

        static void AfterContainer(object __instance)
        {
            try
            {
                PlaceFromContainer(__instance);
            }
            catch
            {
            }
        }

        public static void Tick()
        {
            try
            {
                PollClick();
                ApplyMesh(force: false);
                PlaceFromOpenUi();
            }
            catch
            {
            }
        }

        static void PollClick()
        {
            if (_btn == null || !_btn.gameObject.activeInHierarchy || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            Canvas canvas = _btn.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(_btn, Input.mousePosition, cam))
            {
                Toggle();
            }
        }

        static void HideButton()
        {
            if (_btn != null && _btn.gameObject.activeSelf)
            {
                _btn.gameObject.SetActive(false);
            }
        }

        static void PlaceFromOpenUi()
        {
            InventoryGui gui = InventoryGui.instance;
            object cv = FindContainerView(gui);
            if (PlaceFromContainer(cv))
            {
                return;
            }

            HideButton();
            DestroyStrays();
        }

        static bool PlaceFromContainer(object cv)
        {
            if (cv == null)
            {
                return false;
            }

            TextMeshProUGUI name = AsTmp(Field(cv, _title));
            TextMeshProUGUI slots = AsTmp(Field(cv, _sub));
            if (!IsBackpackHeader(cv, name, slots))
            {
                return false;
            }

            PlaceButton(name ?? slots, slots);
            return true;
        }

        static bool Docked(object cv)
        {
            return _docked != null && cv != null && _docked.GetValue(cv) is bool on && on;
        }

        static bool IsBackpackHeader(object cv, TextMeshProUGUI name, TextMeshProUGUI slots)
        {
            if (name != null && name.gameObject.activeInHierarchy && LooksLikePackName(name.text))
            {
                return true;
            }

            return Docked(cv) &&
                   slots != null &&
                   slots.gameObject.activeInHierarchy &&
                   IsSlotCount(slots.text);
        }

        static object FindContainerView(InventoryGui gui)
        {
            if (gui == null || _containerView == null || !typeof(Component).IsAssignableFrom(_containerView))
            {
                return null;
            }

            Component[] found = gui.GetComponentsInChildren(_containerView, true);
            if (found == null || found.Length == 0)
            {
                return null;
            }

            object dockedPack = null;
            object namedPack = null;
            for (int i = 0; i < found.Length; i++)
            {
                Component c = found[i];
                TextMeshProUGUI name = AsTmp(Field(c, _title));
                TextMeshProUGUI slots = AsTmp(Field(c, _sub));
                if (!IsBackpackHeader(c, name, slots))
                {
                    continue;
                }

                bool docked = Docked(c);
                if (docked)
                {
                    dockedPack = c;
                    break;
                }

                if (namedPack == null)
                {
                    namedPack = c;
                }
            }

            return dockedPack ?? namedPack;
        }

        static object Field(object inst, FieldInfo field)
        {
            return inst == null || field == null ? null : field.GetValue(inst);
        }

        static TextMeshProUGUI AsTmp(object o)
        {
            if (o is TextMeshProUGUI t)
            {
                return t;
            }

            if (o is Component c)
            {
                return c.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (o is GameObject go)
            {
                return go.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            return null;
        }

        static void PlaceButton(TextMeshProUGUI title, TextMeshProUGUI slots)
        {
            if (title == null)
            {
                if (_btn != null && _btn.gameObject.activeSelf)
                {
                    _btn.gameObject.SetActive(false);
                }

                return;
            }

            bool on = title.gameObject.activeInHierarchy;
            if (!EnsureButton(title.rectTransform.parent as RectTransform))
            {
                return;
            }

            if (_btn.gameObject.activeSelf != on)
            {
                _btn.gameObject.SetActive(on);
            }

            if (!on)
            {
                return;
            }

            title.ForceMeshUpdate();
            RectTransform tr = title.rectTransform;
            Vector2 p = tr.anchoredPosition;
            float right = p.x + Mathf.Max(8f, title.preferredWidth);
            if (slots == null)
            {
                slots = SiblingSlotCount(title);
            }

            if (slots != null && slots != title)
            {
                slots.ForceMeshUpdate();
                RectTransform sr = slots.rectTransform;
                right = Mathf.Max(right, sr.anchoredPosition.x + Mathf.Max(8f, slots.preferredWidth));
            }

            _btn.anchorMin = tr.anchorMin;
            _btn.anchorMax = tr.anchorMax;
            _btn.pivot = new Vector2(0f, 0.5f);
            _btn.anchoredPosition = new Vector2(right + Pad, p.y - tr.sizeDelta.y * 0.25f);
            _btn.SetAsLastSibling();
            Color iconOn = new Color(0.70f, 0.62f, 0.42f, 0.90f);
            Color iconOff = new Color(0.48f, 0.45f, 0.38f, 0.75f);
            Color textOn = new Color(0.55f, 0.48f, 0.34f, 0.82f);
            Color textOff = new Color(0.42f, 0.40f, 0.34f, 0.70f);
            if (_icon != null)
            {
                _icon.color = Shown() ? iconOn : iconOff;
            }

            if (_caption != null)
            {
                string want = Shown() ? "Hide Backpack" : "Show Backpack";
                if (_caption.text != want)
                {
                    _caption.text = want;
                }

                _caption.color = Shown() ? textOn : textOff;
                if (title.font != null)
                {
                    _caption.font = title.font;
                    if (title.fontSharedMaterial != null)
                    {
                        _caption.fontSharedMaterial = title.fontSharedMaterial;
                    }
                }

                _caption.ForceMeshUpdate();
                float tw = Mathf.Max(48f, _caption.preferredWidth);
                _btn.sizeDelta = new Vector2(Inner + Icon + 4f + tw + Inner, Height);
            }
        }

        static TextMeshProUGUI SiblingSlotCount(TextMeshProUGUI title)
        {
            Transform parent = title.transform.parent;
            if (parent == null)
            {
                return null;
            }

            float y = title.rectTransform.anchoredPosition.y;
            TextMeshProUGUI[] tmps = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI t = tmps[i];
                if (t == null || t == title || !t.gameObject.activeInHierarchy || IsOurs(t.transform))
                {
                    continue;
                }

                if (Mathf.Abs(t.rectTransform.anchoredPosition.y - y) > 12f)
                {
                    continue;
                }

                if (IsSlotCount(t.text))
                {
                    return t;
                }
            }

            return null;
        }

        static bool IsSlotCount(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int slash = text.IndexOf('/');
            if (slash <= 0 || slash >= text.Length - 1)
            {
                return false;
            }

            int used;
            int max;
            if (!int.TryParse(text.Substring(0, slash).Trim(), out used) ||
                !int.TryParse(text.Substring(slash + 1).Trim(), out max))
            {
                return false;
            }

            return used >= 0 && max > 0 && max <= 48 && used <= max;
        }

        static bool LooksLikePackName(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.IndexOf("backpack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("satchel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("knappsack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("wetpack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("sherpa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("wisppack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("haversack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("cinderward", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsOurs(Transform t)
        {
            for (int i = 0; i < 8 && t != null; i++)
            {
                if (t.name == ButtonName)
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        static void DestroyStrays()
        {
            DestroyStraysUnder(InventoryGui.instance != null ? InventoryGui.instance.transform : null);
            Hud hud = Hud.instance;
            if (hud != null && hud.m_rootObject != null)
            {
                DestroyStraysUnder(hud.m_rootObject.transform);
            }
        }

        static void DestroyStraysUnder(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t.name != ButtonName)
                {
                    continue;
                }

                if (_btn != null && t == _btn)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(t.gameObject);
            }
        }

        static bool EnsureButton(RectTransform parent)
        {
            if (parent == null)
            {
                return false;
            }

            if (_btn != null && _btn.parent != parent)
            {
                UnityEngine.Object.Destroy(_btn.gameObject);
                _btn = null;
                _caption = null;
                _icon = null;
            }

            if (_btn != null && _btn.parent == parent && _caption != null)
            {
                if (Time.frameCount % 32 == 0)
                {
                    DestroyStrays();
                }

                return true;
            }

            Transform existing = parent.Find(ButtonName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject go = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            _btn = go.GetComponent<RectTransform>();
            _btn.sizeDelta = new Vector2(140f, Height);
            Image plate = go.GetComponent<Image>();
            plate.color = new Color(0.12f, 0.11f, 0.09f, 0.7f);
            plate.raycastTarget = true;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = plate;
            btn.onClick.AddListener(Toggle);

            GameObject iconGo = new GameObject("icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(Inner, 0f);
            irt.sizeDelta = new Vector2(Icon, Icon);
            _icon = iconGo.GetComponent<Image>();
            _icon.raycastTarget = false;
            _icon.preserveAspect = true;
            Sprite sprite = null;
            try
            {
                sprite = Icons.Get("backpack") ?? Icons.Get("cape") ?? SeneaLIcons.Get("cape");
            }
            catch
            {
            }

            _icon.sprite = sprite;
            _icon.enabled = sprite != null;

            GameObject textGo = new GameObject("t", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            _caption = textGo.AddComponent<TextMeshProUGUI>();
            TmpFont.Ensure();
            TmpFont.Apply(_caption);
            _caption.fontSize = 12f;
            _caption.alignment = TextAlignmentOptions.MidlineLeft;
            _caption.textWrappingMode = TextWrappingModes.NoWrap;
            _caption.overflowMode = TextOverflowModes.Overflow;
            _caption.raycastTarget = false;
            _caption.text = "Hide Backpack";
            RectTransform textRt = _caption.rectTransform;
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(Inner + Icon + 4f, 0f);
            textRt.offsetMax = new Vector2(-Inner, 0f);
            DestroyStrays();
            return true;
        }

        public static void Toggle()
        {
            if (ShowBackpackMesh == null)
            {
                return;
            }

            ShowBackpackMesh.Value = !ShowBackpackMesh.Value;
            ApplyMesh(force: true);
        }

        static void ApplyMesh(bool force)
        {
            bool show = Shown();
            Player player = Player.m_localPlayer;
            if (!player)
            {
                if (_playerId != 0)
                {
                    _playerId = 0;
                }

                return;
            }

            int id = player.GetInstanceID();
            if (id != _playerId)
            {
                _playerId = id;
                _appliedShow = !show;
                force = true;
            }

            if (!force && show == _appliedShow && Time.frameCount % 8 != 0)
            {
                return;
            }

            _appliedShow = show;

            VisEquipment vis = player.GetComponentInChildren<VisEquipment>(true);
            if (vis == null)
            {
                return;
            }

            string prefab = EquippedBackpackPrefab(player);
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || r.gameObject.layer == 5)
                {
                    continue;
                }

                if (!NameLooksLikeBackpack(r.transform) && !NameContains(r.transform, prefab))
                {
                    continue;
                }

                if (r.enabled != show)
                {
                    r.enabled = show;
                }
            }

            HideSlotVisuals(vis, "m_shoulderItem", "m_shoulderItemInstances", show);
            HideSlotVisuals(vis, "m_utilityItem", "m_utilityItemInstances", show);
        }

        static string EquippedBackpackPrefab(Player player)
        {
            Inventory inv = player.GetInventory();
            if (inv == null)
            {
                return null;
            }

            List<ItemDrop.ItemData> items = inv.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                if (item == null || !item.m_equipped)
                {
                    continue;
                }

                string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
                string shared = item.m_shared != null ? item.m_shared.m_name : null;
                if (LooksLikePackName(prefab) || LooksLikePackName(shared) ||
                    (!string.IsNullOrEmpty(prefab) && prefab.IndexOf("backpack", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(shared) && shared.IndexOf("backpack", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return prefab;
                }
            }

            return null;
        }

        static void HideSlotVisuals(VisEquipment vis, string itemField, string instField, bool show)
        {
            FieldInfo itemInfo = AccessTools.Field(typeof(VisEquipment), itemField);
            FieldInfo instInfo = AccessTools.Field(typeof(VisEquipment), instField);
            string item = itemInfo != null ? itemInfo.GetValue(vis) as string : null;
            if (string.IsNullOrEmpty(item) ||
                (item.IndexOf("backpack", StringComparison.OrdinalIgnoreCase) < 0 &&
                 !LooksLikePackName(item)))
            {
                return;
            }

            object bag = instInfo != null ? instInfo.GetValue(vis) : null;
            if (bag is GameObject one)
            {
                SetRenderers(one, show);
                return;
            }

            if (bag is IEnumerable list)
            {
                foreach (object o in list)
                {
                    SetRenderers(o as GameObject, show);
                }
            }
        }

        static void SetRenderers(GameObject go, bool show)
        {
            if (go == null)
            {
                return;
            }

            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null && rs[i].gameObject.layer != 5 && rs[i].enabled != show)
                {
                    rs[i].enabled = show;
                }
            }
        }

        static bool NameContains(Transform t, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            for (int i = 0; i < 8 && t != null; i++)
            {
                string n = t.name;
                if (!string.IsNullOrEmpty(n) &&
                    n.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        static bool NameLooksLikeBackpack(Transform t)
        {
            for (int i = 0; i < 8 && t != null; i++)
            {
                if (LooksLikePackName(t.name) ||
                    (!string.IsNullOrEmpty(t.name) &&
                     t.name.IndexOf("backpack", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }
    }
}
