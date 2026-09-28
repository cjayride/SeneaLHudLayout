using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class RepairTipPlace
    {
        static Type _type;
        static FieldInfo _anchor;
        static FieldInfo _root;
        static FieldInfo _panel;
        static FieldInfo _arrow;
        static FieldInfo _arrowIn;
        static FieldInfo _width;
        static FieldInfo _height;
        static FieldInfo _arrowOn;

        public static void Apply(Harmony harmony)
        {
            _type = AccessTools.TypeByName("SeneaLUI.Components.Tooltip");
            MethodInfo position = _type != null ? AccessTools.Method(_type, "Position") : null;
            if (position == null)
            {
                return;
            }

            _anchor = AccessTools.Field(_type, "_anchor");
            _root = AccessTools.Field(_type, "_root");
            _panel = AccessTools.Field(_type, "_panel");
            _arrow = AccessTools.Field(_type, "_arrow");
            _arrowIn = AccessTools.Field(_type, "_arrowIn");
            _width = AccessTools.Field(_type, "_width");
            _height = AccessTools.Field(_type, "_height");
            _arrowOn = AccessTools.Field(_type, "_arrowOn");
            harmony.Patch(position, postfix: new HarmonyMethod(typeof(RepairTipPlace), nameof(After)));
        }

        static void After()
        {
            RectTransform anchor = Get<RectTransform>(_anchor);
            RectTransform root = Get<RectTransform>(_root);
            RectTransform panel = Get<RectTransform>(_panel);
            RectTransform arrow = Get<RectTransform>(_arrow);
            RectTransform arrowIn = Get<RectTransform>(_arrowIn);
            if (anchor == null || root == null || panel == null || arrow == null || arrowIn == null || !IsRepair(anchor))
            {
                return;
            }

            float width = Get<float>(_width);
            float height = Get<float>(_height);
            Rect rect = root.rect;
            float screenW = rect.width;
            float screenH = rect.height;
            float scale = root.lossyScale.x > 0.0001f ? anchor.lossyScale.x / root.lossyScale.x : 1f;
            Vector2 anchorSize = anchor.rect.size * scale;
            Vector3 center = root.InverseTransformPoint(anchor.TransformPoint(anchor.rect.center));
            float fromLeft = center.x - rect.xMin;
            float aboveTop = rect.yMax - center.y - anchorSize.y * 0.5f;
            float belowTop = aboveTop + anchorSize.y;

            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);

            float x = Mathf.Clamp(fromLeft - width * 0.5f, 8f, Mathf.Max(8f, screenW - width - 8f));
            float y = belowTop + 12f;
            float fitted = y + height > screenH - 8f ? Mathf.Max(8f, screenH - 8f - height) : y;
            bool showArrow = Mathf.Abs(fitted - y) < 0.5f;
            panel.anchoredPosition = new Vector2(x, -fitted);

            float arrowX = Mathf.Clamp(fromLeft - x, 16f, Mathf.Max(16f, width - 16f));
            Vector2 arrowPos = new Vector2(arrowX - width * 0.5f, height * 0.5f);
            arrow.anchoredPosition = arrowPos;
            arrowIn.anchoredPosition = arrowPos;
            arrow.SetSiblingIndex(0);
            arrowIn.SetAsLastSibling();
            SetArrow(arrow, arrowIn, showArrow);
        }

        static bool IsRepair(RectTransform anchor)
        {
            string name = anchor.name;
            return name == "repair" || name == "RepairButton";
        }

        static void SetArrow(RectTransform arrow, RectTransform arrowIn, bool on)
        {
            if (_arrowOn != null && _arrowOn.GetValue(null) is bool current && current == on
                && arrow.gameObject.activeSelf == on && arrowIn.gameObject.activeSelf == on)
            {
                return;
            }

            if (_arrowOn != null)
            {
                _arrowOn.SetValue(null, on);
            }

            if (arrow.gameObject.activeSelf != on)
            {
                arrow.gameObject.SetActive(on);
            }

            if (arrowIn.gameObject.activeSelf != on)
            {
                arrowIn.gameObject.SetActive(on);
            }
        }

        static T Get<T>(FieldInfo field)
        {
            if (field == null)
            {
                return default(T);
            }

            object value = field.GetValue(null);
            return value is T typed ? typed : default(T);
        }
    }
}
