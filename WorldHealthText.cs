using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class WorldHealthText
    {
        const string LabelName = "SeneaLHudLayout_Health";
        static readonly FieldInfo HudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
        static readonly FieldInfo CharacterField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_character");
        static readonly FieldInfo GuiField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_gui");
        static readonly FieldInfo HealthTextField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_healthText");
        static readonly FieldInfo NameField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_name");

        public static void Apply(Harmony harmony)
        {
            MethodInfo update = AccessTools.Method(typeof(EnemyHud), "UpdateHuds");
            if (update != null)
            {
                harmony.Patch(update, postfix: new HarmonyMethod(typeof(WorldHealthText), nameof(AfterUpdateHuds)));
            }
        }

        static void AfterUpdateHuds(EnemyHud __instance)
        {
            if (__instance == null || HudsField == null)
            {
                return;
            }

            bool show = SeneaLHudLayoutPlugin.ShowWorldHealthNumbers != null
                && SeneaLHudLayoutPlugin.ShowWorldHealthNumbers.Value;

            IDictionary huds = HudsField.GetValue(__instance) as IDictionary;
            if (huds == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in huds)
            {
                object data = entry.Value;
                if (data == null)
                {
                    continue;
                }

                Character character = CharacterField?.GetValue(data) as Character;
                GameObject gui = GuiField?.GetValue(data) as GameObject;
                if (!character || !gui)
                {
                    continue;
                }

                if (!show)
                {
                    Transform ours = gui.transform.Find(LabelName);
                    if (!ours)
                    {
                        Transform health = gui.transform.Find("Health");
                        ours = health ? health.Find(LabelName) : null;
                    }

                    if (ours && ours.gameObject.activeSelf)
                    {
                        ours.gameObject.SetActive(false);
                    }

                    Transform vanillaText = gui.transform.Find("Health/HealthText");
                    if (vanillaText && !vanillaText.gameObject.activeSelf)
                    {
                        vanillaText.gameObject.SetActive(true);
                    }

                    continue;
                }

                TMP_Text label = HealthTextField?.GetValue(data) as TMP_Text;
                if (!label)
                {
                    label = EnsureLabel(gui.transform, NameField?.GetValue(data) as TMP_Text);
                    HealthTextField?.SetValue(data, label);
                }

                Transform vanilla = gui.transform.Find("Health/HealthText");
                if (vanilla && vanilla.gameObject != label.gameObject)
                {
                    vanilla.gameObject.SetActive(false);
                }

                int hp = Mathf.CeilToInt(character.GetHealth());
                int max = Mathf.Max(1, Mathf.CeilToInt(character.GetMaxHealth()));
                label.text = hp + "/" + max;
                PlaceAbove(label, gui.transform.Find("Health"));
                label.gameObject.SetActive(true);
            }
        }

        static TMP_Text EnsureLabel(Transform gui, TMP_Text name)
        {
            Transform health = gui.Find("Health");
            if (!health)
            {
                return null;
            }

            Transform existing = gui.Find(LabelName);
            if (!existing)
            {
                existing = health.Find(LabelName);
            }

            TextMeshProUGUI tmp = existing ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (tmp == null)
            {
                GameObject go = new GameObject(LabelName);
                go.transform.SetParent(gui, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }

            if (name != null && name.font != null)
            {
                tmp.font = name.font;
                if (name.fontSharedMaterial != null)
                {
                    tmp.fontSharedMaterial = name.fontSharedMaterial;
                }
            }
            else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.color = new Color(0.96f, 0.93f, 0.86f);
            tmp.fontSize = name != null ? Mathf.Max(12f, name.fontSize * 0.8f) : 14f;
            return tmp;
        }

        static void PlaceAbove(TMP_Text label, Transform health)
        {
            if (!health)
            {
                return;
            }

            RectTransform bar = health as RectTransform;
            RectTransform rt = label.rectTransform;
            Transform parent = bar != null ? bar.parent : health.parent;
            if (parent && rt.parent != parent)
            {
                rt.SetParent(parent, false);
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            if (bar != null)
            {
                float top = bar.anchoredPosition.y + bar.rect.height * (1f - bar.pivot.y);
                rt.anchoredPosition = new Vector2(bar.anchoredPosition.x, top + 1f);
                rt.sizeDelta = new Vector2(Mathf.Max(64f, bar.rect.width + 8f), 18f);
            }

            rt.SetAsLastSibling();
        }
    }
}
