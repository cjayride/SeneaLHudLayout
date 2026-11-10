using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL UI already draws the piece health color bar just left of the cursor.
    /// These labels sit beside that bar. They never write the piece name into the hover text.
    /// </summary>
    static class PieceHealthLabel
    {
        const string ValueName = "SeneaLHudLayout_PieceHealth";
        const string PercentName = "SeneaLHudLayout_PieceHealthPercent";

        static RectTransform _bar;
        static TextMeshProUGUI _value;
        static TextMeshProUGUI _percent;
        static int _wearId;
        static int _current = -1;
        static int _percentValue = -1;

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Hud), "UpdateCrosshair"),
                postfix: new HarmonyMethod(typeof(PieceHealthLabel), nameof(AfterCrosshair)));
        }

        static void AfterCrosshair(Player player)
        {
            bool showValue = SeneaLHudLayoutPlugin.ShowPieceHealth != null
                && SeneaLHudLayoutPlugin.ShowPieceHealth.Value;
            bool showPercent = SeneaLHudLayoutPlugin.ShowPieceHealthPercent != null
                && SeneaLHudLayoutPlugin.ShowPieceHealthPercent.Value;
            if (!showValue && !showPercent)
            {
                Hide();
                return;
            }

            RectTransform bar = Bar();
            if (player == null || !bar || !bar.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            Piece piece = player.GetHoveringPiece();
            WearNTear wear = piece ? piece.GetComponent<WearNTear>() : null;
            if (!wear || wear.m_health <= 0f)
            {
                Hide();
                return;
            }

            float fraction = Mathf.Clamp01(wear.GetHealthPercentage());
            int current = Mathf.CeilToInt(fraction * wear.m_health);
            int percent = Mathf.RoundToInt(fraction * 100f);
            int max = Mathf.CeilToInt(wear.m_health);
            bool changed = wear.GetInstanceID() != _wearId || current != _current || percent != _percentValue;
            _wearId = wear.GetInstanceID();
            _current = current;
            _percentValue = percent;

            float valueSize = Size(SeneaLHudLayoutPlugin.ShowPieceHealthSize, 14f);
            float percentSize = Size(SeneaLHudLayoutPlugin.ShowPieceHealthPercentSize, 14f);
            if (showValue)
            {
                TextMeshProUGUI label = Ensure(bar, ValueName, ref _value);
                if (changed || label.text.Length == 0)
                {
                    label.text = current + "/" + max;
                }

                Place(label, bar, showPercent ? (valueSize + 2f) * 0.5f : 0f, valueSize);
                label.gameObject.SetActive(true);
            }
            else if (_value)
            {
                _value.gameObject.SetActive(false);
            }

            if (showPercent)
            {
                TextMeshProUGUI label = Ensure(bar, PercentName, ref _percent);
                if (changed || label.text.Length == 0)
                {
                    label.text = percent + "%";
                }

                Place(label, bar, showValue ? -(percentSize + 2f) * 0.5f : 0f, percentSize);
                label.gameObject.SetActive(true);
            }
            else if (_percent)
            {
                _percent.gameObject.SetActive(false);
            }
        }

        static float Size(BepInEx.Configuration.ConfigEntry<float> entry, float fallback)
        {
            return Mathf.Clamp(entry != null ? entry.Value : fallback, 8f, 32f);
        }

        // AfterCrosshair runs on Hud.UpdateCrosshair - EVERY FRAME. The whole-scene
        // FindObjectsOfType<RectTransform>() below is a scene-wide scan, and when the bar is
        // absent (SeneaL UI has not created it, the HUD is hidden) the cache stays null, so
        // before 1.5.50 the scan re-ran every frame with zero chance of ever succeeding.
        // The scan now retries on a 2s cadence: the bar is found as soon as it exists
        // (the labels appear at most 2s later, once, in a session) and the frame path is
        // a field check when it is missing.
        static float _nextBarScan;

        static RectTransform Bar()
        {
            if (_bar)
            {
                return _bar;
            }

            if (Time.unscaledTime < _nextBarScan)
            {
                return null;
            }

            _nextBarScan = Time.unscaledTime + 2f;

            RectTransform[] rects = Object.FindObjectsOfType<RectTransform>();
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].name == "piecehp")
                {
                    _bar = rects[i];
                    return _bar;
                }
            }

            return null;
        }

        static TextMeshProUGUI Ensure(RectTransform bar, string name, ref TextMeshProUGUI slot)
        {
            if (slot)
            {
                return slot;
            }

            Transform existing = bar.parent.Find(name);
            GameObject go = existing ? existing.gameObject : new GameObject(name);
            if (!existing)
            {
                go.transform.SetParent(bar.parent, false);
            }

            TmpFont.Ensure();
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (!tmp)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            TmpFont.Apply(tmp);
            if (tmp.font == null && Hud.instance != null && Hud.instance.m_hoverName != null && Hud.instance.m_hoverName.font != null)
            {
                tmp.font = Hud.instance.m_hoverName.font;
            }

            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.color = new Color(0.91f, 0.77f, 0.42f, 1f);
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            slot = tmp;
            return tmp;
        }

        static void Place(TextMeshProUGUI label, RectTransform bar, float y, float size)
        {
            RectTransform rt = label.rectTransform;
            rt.anchorMin = bar.anchorMin;
            rt.anchorMax = bar.anchorMax;
            rt.pivot = new Vector2(1f, 0.5f);
            float left = bar.anchoredPosition.x - bar.rect.width * bar.pivot.x - 4f;
            rt.anchoredPosition = new Vector2(left, bar.anchoredPosition.y + y);
            rt.sizeDelta = new Vector2(180f, size + 6f);
            label.fontSize = size;
        }

        static void Hide()
        {
            if (_value && _value.gameObject.activeSelf)
            {
                _value.gameObject.SetActive(false);
            }

            if (_percent && _percent.gameObject.activeSelf)
            {
                _percent.gameObject.SetActive(false);
            }
        }
    }
}
