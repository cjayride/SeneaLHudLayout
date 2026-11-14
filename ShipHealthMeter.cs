using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Hull health under the sailing wind disc while you are at the helm.
    /// Prefers SeneaL UI's ShipView.Wind (vanilla wind is hidden). Falls back to
    /// Hud.m_shipWindIndicatorRoot. Off by default; the pack can turn it on.
    /// </summary>
    static class ShipHealthMeter
    {
        const string RootName = "SeneaLHudLayout_ShipHealth";
        const float Gap = 6f;
        const float BarW = 84f;
        const float BarH = 8f;
        const float NumGap = 1f;
        const float LabelH = 12f;

        static readonly Color Track = new Color(0.08f, 0.07f, 0.05f, 0.82f);
        static readonly Color Good = new Color(0.45f, 0.82f, 0.38f, 1f);
        static readonly Color Warn = new Color(0.95f, 0.75f, 0.22f, 1f);
        static readonly Color Bad = new Color(0.90f, 0.28f, 0.20f, 1f);
        static readonly Color LabelColor = new Color(0.91f, 0.77f, 0.42f, 1f);

        static RectTransform _root;
        static Image _fill;
        static TextMeshProUGUI _label;
        static PropertyInfo _shipProp;
        static PropertyInfo _windProp;
        static PropertyInfo _windShownProp;
        static bool _looked;
        static int _shownHp = int.MinValue;
        static int _shownMax = int.MinValue;

        public static void Tick()
        {
            bool on = SeneaLHudLayoutPlugin.ShowShipHealth != null
                && SeneaLHudLayoutPlugin.ShowShipHealth.Value;
            Player player = Player.m_localPlayer;
            Ship ship = player != null ? player.GetControlledShip() : null;
            WearNTear wear = ship != null ? ship.GetComponent<WearNTear>() : null;
            if (!on || wear == null || wear.m_health <= 0f)
            {
                Hide();
                return;
            }

            RectTransform wind = WindAnchor();
            if (wind == null || !wind.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            Ensure();
            if (_root == null)
            {
                return;
            }

            float pct = Mathf.Clamp01(wear.GetHealthPercentage());
            int current = Mathf.CeilToInt(pct * wear.m_health);
            int max = Mathf.CeilToInt(wear.m_health);
            _fill.fillAmount = pct;
            _fill.color = pct > 0.5f ? Good : (pct > 0.25f ? Warn : Bad);

            bool numbers = SeneaLHudLayoutPlugin.ShowShipHealthNumbers == null
                || SeneaLHudLayoutPlugin.ShowShipHealthNumbers.Value;
            if (_label != null)
            {
                if (numbers)
                {
                    if (current != _shownHp || max != _shownMax)
                    {
                        _label.text = current + "/" + max;
                        _shownHp = current;
                        _shownMax = max;
                    }

                    if (!_label.gameObject.activeSelf)
                    {
                        _label.gameObject.SetActive(true);
                    }
                }
                else if (_label.gameObject.activeSelf)
                {
                    _label.gameObject.SetActive(false);
                }
            }

            Place(wind, numbers);
            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }
        }

        static RectTransform WindAnchor()
        {
            RectTransform seneal = SeneaLWind();
            if (seneal != null && seneal.gameObject.activeInHierarchy)
            {
                return seneal;
            }

            Hud hud = Hud.instance;
            if (hud == null || hud.m_shipHudRoot == null || !hud.m_shipHudRoot.activeInHierarchy)
            {
                return null;
            }

            if (hud.m_shipWindIndicatorRoot != null
                && hud.m_shipWindIndicatorRoot.gameObject.activeInHierarchy)
            {
                return hud.m_shipWindIndicatorRoot;
            }

            return null;
        }

        static RectTransform SeneaLWind()
        {
            if (!_looked)
            {
                _looked = true;
                Type root = AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
                Type ship = AccessTools.TypeByName("SeneaLUI.Hud.ShipView");
                _shipProp = root == null ? null : AccessTools.Property(root, "Ship");
                _windProp = ship == null ? null : AccessTools.Property(ship, "Wind");
                _windShownProp = ship == null ? null : AccessTools.Property(ship, "WindShown");
            }

            if (_shipProp == null || _windProp == null)
            {
                return null;
            }

            object view = _shipProp.GetValue(null, null);
            if (view == null)
            {
                return null;
            }

            if (_windShownProp != null && _windShownProp.GetValue(view, null) is bool shown && !shown)
            {
                return null;
            }

            return _windProp.GetValue(view, null) as RectTransform;
        }

        static void Ensure()
        {
            if (_root)
            {
                return;
            }

            GameObject go = new GameObject(RootName, typeof(RectTransform));
            _root = go.GetComponent<RectTransform>();
            go.layer = 5;

            Image track = ImageOn(go.transform, "track");
            track.color = Track;
            RectTransform trackRt = track.rectTransform;
            trackRt.anchorMin = new Vector2(0f, 1f);
            trackRt.anchorMax = new Vector2(1f, 1f);
            trackRt.pivot = new Vector2(0.5f, 1f);
            trackRt.anchoredPosition = Vector2.zero;
            trackRt.sizeDelta = new Vector2(0f, BarH);

            _fill = ImageOn(trackRt, "fill");
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 1f;
            RectTransform fillRt = _fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);

            TmpFont.Ensure();
            GameObject labelGo = new GameObject("label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = go.layer;
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            TmpFont.Apply(_label);
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = LabelColor;
            _label.fontSize = 11f;
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.raycastTarget = false;
            RectTransform labelRt = _label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -(BarH + NumGap));
            labelRt.sizeDelta = new Vector2(0f, LabelH);
        }

        static Image ImageOn(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            Image image = go.AddComponent<Image>();
            image.sprite = CharacterExtras.PixelSprite();
            image.raycastTarget = false;
            return image;
        }

        static void Place(RectTransform wind, bool numbers)
        {
            float height = BarH + (numbers ? NumGap + LabelH : 0f);
            _root.sizeDelta = new Vector2(BarW, height);

            Transform parent = wind.parent;
            if (parent == null || parent == wind)
            {
                Hud hud = Hud.instance;
                parent = hud != null && hud.m_shipHudRoot != null
                    ? hud.m_shipHudRoot.transform
                    : wind;
            }

            if (_root.parent != parent)
            {
                _root.SetParent(parent, false);
                _root.gameObject.layer = parent.gameObject.layer;
            }

            _root.localRotation = Quaternion.identity;
            _root.localScale = Vector3.one;
            _root.anchorMin = wind.anchorMin;
            _root.anchorMax = wind.anchorMax;
            _root.pivot = new Vector2(0.5f, 1f);

            Vector2 size = wind.rect.size;
            Vector2 pivot = wind.pivot;
            Vector2 wp = wind.anchoredPosition;
            float cx = wp.x + (0.5f - pivot.x) * size.x;
            float bottom = wp.y - pivot.y * size.y;
            _root.anchoredPosition = new Vector2(cx, bottom - Gap);
        }

        static void Hide()
        {
            _shownHp = int.MinValue;
            _shownMax = int.MinValue;
            if (_root && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }
    }
}
