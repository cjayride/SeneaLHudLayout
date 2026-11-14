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
    /// Small button to the right of the inventory QUIVER label. Toggles the 3D quiver
    /// model on the local player. Ammo slots and the equipped item are unchanged.
    /// </summary>
    static class QuiverMeshToggle
    {
        public static ConfigEntry<bool> ShowQuiverMesh;

        const string ButtonName = "SeneaLHudLayout_QuiverMesh";
        const float Icon = 18f;
        const float Height = 20f;
        const float Pad = 6f;
        const float Inner = 4f;

        static FieldInfo _quiverTitle;
        static FieldInfo _baGo;
        static FieldInfo _baInstances;
        static bool _baLookup;
        static RectTransform _btn;
        static Image _icon;
        static TextMeshProUGUI _caption;
        static bool _appliedShow = true;

        public static void Apply(Harmony harmony)
        {
            Type view = AccessTools.TypeByName("SeneaLUI.Inv.InventoryView");
            if (view == null)
            {
                return;
            }

            _quiverTitle = AccessTools.Field(view, "_quiverTitle");
            MethodInfo labels = AccessTools.Method(view, "PlaceFootLabels");
            MethodInfo tick = AccessTools.Method(view, "Tick", new[] { typeof(bool) });
            if (_quiverTitle == null || labels == null)
            {
                return;
            }

            try
            {
                harmony.Patch(labels, postfix: new HarmonyMethod(typeof(QuiverMeshToggle), nameof(AfterLabels)));
            }
            catch (Exception e)
            {
                Debug.LogError("[SeneaL HUD Layout] QuiverMeshToggle: PlaceFootLabels " + e.Message);
            }

            if (tick != null)
            {
                try
                {
                    harmony.Patch(tick, postfix: new HarmonyMethod(typeof(QuiverMeshToggle), nameof(AfterTick)));
                }
                catch (Exception e)
                {
                    Debug.LogError("[SeneaL HUD Layout] QuiverMeshToggle: Tick " + e.Message);
                }
            }

            Type ba = AccessTools.TypeByName("BetterArchery.BetterArchery");
            if (ba == null)
            {
                return;
            }

            foreach (MethodInfo m in ba.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != "CreateOrUpdateQuiverVisual" && m.Name != "ApplyQuiverTransform" &&
                    m.Name != "RefreshQuiverVisualTransforms")
                {
                    continue;
                }

                try
                {
                    harmony.Patch(m, postfix: new HarmonyMethod(typeof(QuiverMeshToggle), nameof(AfterBaVisual)));
                }
                catch
                {
                }
            }
        }

        static bool Shown()
        {
            return ShowQuiverMesh == null || ShowQuiverMesh.Value;
        }

        static void AfterBaVisual()
        {
            ApplyMesh(force: true);
        }

        static void AfterLabels(object __instance)
        {
            try
            {
                PlaceButton(__instance);
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

                PlaceButton(__instance);
            }
            catch
            {
            }
        }

        public static void Tick()
        {
            try
            {
                ApplyMesh(force: false);
            }
            catch
            {
            }
        }

        static void PlaceButton(object view)
        {
            TextMeshProUGUI title = _quiverTitle.GetValue(view) as TextMeshProUGUI;
            if (title == null)
            {
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
            float w = Mathf.Max(8f, title.preferredWidth);
            RectTransform tr = title.rectTransform;
            Vector2 p = tr.anchoredPosition;
            _btn.anchorMin = tr.anchorMin;
            _btn.anchorMax = tr.anchorMax;
            _btn.pivot = new Vector2(0f, 0.5f);
            _btn.anchoredPosition = new Vector2(p.x + w + Pad, p.y - tr.sizeDelta.y * 0.25f);
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
                string want = Shown() ? "Hide Quiver" : "Show Quiver";
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

        static bool EnsureButton(RectTransform parent)
        {
            if (parent == null)
            {
                return false;
            }

            if (_btn != null && _btn.parent == parent && _caption != null)
            {
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
            _btn.sizeDelta = new Vector2(120f, Height);
            Image plate = go.GetComponent<Image>();
            plate.color = new Color(0.12f, 0.11f, 0.09f, 0.7f);
            plate.raycastTarget = true;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = plate;
            btn.onClick.AddListener(OnClick);

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
                sprite = Icons.Get("quiver") ?? Icons.Get("arrow");
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
            _caption.text = "Hide Quiver";
            RectTransform textRt = _caption.rectTransform;
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(Inner + Icon + 4f, 0f);
            textRt.offsetMax = new Vector2(-Inner, 0f);
            return true;
        }

        static void OnClick()
        {
            if (ShowQuiverMesh == null)
            {
                return;
            }

            ShowQuiverMesh.Value = !ShowQuiverMesh.Value;
            ApplyMesh(force: true);
        }

        static void ApplyMesh(bool force)
        {
            bool show = Shown();
            if (!force && show == _appliedShow && Time.frameCount % 8 != 0)
            {
                return;
            }

            _appliedShow = show;
            Player player = Player.m_localPlayer;
            if (!player)
            {
                return;
            }

            DiscoverBa();
            SetGo(_baGo != null ? _baGo.GetValue(null) as GameObject : null, show);
            SetCollection(_baInstances != null ? _baInstances.GetValue(null) : null, show);

            VisEquipment vis = player.GetComponentInChildren<VisEquipment>(true);
            if (vis == null)
            {
                return;
            }

            Transform root = vis.transform;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null)
                {
                    continue;
                }

                if (!NameLooksLikeQuiver(r.transform))
                {
                    continue;
                }

                if (r.gameObject.layer == 5)
                {
                    continue;
                }

                if (r.enabled != show)
                {
                    r.enabled = show;
                }
            }
        }

        static bool NameLooksLikeQuiver(Transform t)
        {
            for (int i = 0; i < 6 && t != null; i++)
            {
                string n = t.name;
                if (!string.IsNullOrEmpty(n) &&
                    n.IndexOf("quiver", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        static void DiscoverBa()
        {
            if (_baLookup)
            {
                return;
            }

            _baLookup = true;
            Type t = AccessTools.TypeByName("BetterArchery.BetterArchery");
            if (t == null)
            {
                return;
            }

            _baGo = AccessTools.Field(t, "QuiverGO") ?? AccessTools.Field(t, "QuiverInstance");
            _baInstances = AccessTools.Field(t, "QuiverInstances");
        }

        static void SetCollection(object bag, bool show)
        {
            if (bag == null)
            {
                return;
            }

            if (bag is GameObject one)
            {
                SetGo(one, show);
                return;
            }

            if (bag is IEnumerable list)
            {
                foreach (object o in list)
                {
                    if (o is GameObject g)
                    {
                        SetGo(g, show);
                    }
                    else if (o is DictionaryEntry de)
                    {
                        SetGo(de.Value as GameObject, show);
                    }
                    else if (o != null)
                    {
                        Type ot = o.GetType();
                        if (ot.IsGenericType && ot.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        {
                            SetGo(ot.GetProperty("Value")?.GetValue(o, null) as GameObject, show);
                        }
                    }
                }
            }
        }

        static void SetGo(GameObject go, bool show)
        {
            if (go == null)
            {
                return;
            }

            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null && rs[i].enabled != show)
                {
                    rs[i].enabled = show;
                }
            }
        }
    }
}
