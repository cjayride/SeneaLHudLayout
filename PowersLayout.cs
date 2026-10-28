using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class PowersLayout
    {
        const string PassiveGuid = "org.bepinex.plugins.passivepowers";
        const string KeyName = "SeneaLHudLayout_Key";
        const string BuffName = "SeneaLHudLayout_Buff";
        const string DownName = "SeneaLHudLayout_Down";
        const string ShortcutSection = "2 - Active Powers";

        static readonly Color KeyReady = new Color(0.93f, 0.88f, 0.78f);
        static readonly Color KeyLocked = new Color(0.45f, 0.45f, 0.45f);
        static readonly Color BuffColor = new Color(0.45f, 0.95f, 0.4f);
        static readonly Color DownColor = new Color(0.95f, 0.32f, 0.28f);
        static readonly List<StatusEffect> Effects = new List<StatusEffect>();
        static readonly FieldInfo GuardianPowerField = AccessTools.Field(typeof(Player), "m_guardianPower");
        static readonly FieldInfo EffectTimeField = AccessTools.Field(typeof(StatusEffect), "m_time");

        static bool _gpHidden;
        static GameObject _gp;
        static TMP_FontAsset _font;
        static Material _fontMaterial;

        public static void Apply(Hud hud, RectTransform senealRoot)
        {
            if (!Chainloader.PluginInfos.TryGetValue(PassiveGuid, out PluginInfo plugin))
            {
                return;
            }

            if (SeneaLHudLayoutPlugin.HideSenealPower.Value)
            {
                HideSenealSlot(senealRoot);
            }
            else
            {
                ShowSenealSlot();
            }

            PlacePowers(hud, senealRoot, plugin.Instance.Config);
        }

        public static void Restore(RectTransform senealRoot)
        {
            ShowSenealSlot();
            ResetScale();
        }

        static void HideSenealSlot(RectTransform senealRoot)
        {
            // Try multiple paths for guardian power element in SeneaL UI v1.2.5
            Transform gp = senealRoot.Find("Health/vitals/gp");
            if (gp == null) gp = senealRoot.Find("vitals/gp");
            if (gp == null) gp = senealRoot.Find("Health/guardianPower");
            if (gp == null) gp = senealRoot.Find("guardianPower");
            if (gp == null) gp = senealRoot.Find("vitals/guardianPower");
            if (gp == null) gp = senealRoot.Find("Health/vitals/guardianPower");
            if (gp == null) gp = senealRoot.Find("Health/vitalsCluster/gp");
            if (gp == null) gp = senealRoot.Find("vitalsCluster/gp");
            if (gp == null) gp = senealRoot.Find("Health/vitals/guardianPowerIcon");
            if (gp == null) gp = senealRoot.Find("vitals/guardianPowerIcon");
            if (gp == null) gp = senealRoot.Find("Health/vitals/guardianPowerSlot");
            if (gp == null) gp = senealRoot.Find("vitals/guardianPowerSlot");
            if (gp == null) gp = senealRoot.Find("Health/vitals/guardianPowerContainer");
            if (gp == null) gp = senealRoot.Find("vitals/guardianPowerContainer");
            if (gp == null) gp = senealRoot.Find("Health/vitalsCluster/guardianPower");
            if (gp == null) gp = senealRoot.Find("vitalsCluster/guardianPower");
            if (gp == null) gp = senealRoot.Find("Health/guardianPowerContainer");
            if (gp == null) gp = senealRoot.Find("guardianPowerContainer");
            
            if (gp == null)
            {
                // Debug.LogWarning("[SeneaL HUD Layout] Guardian power element not found - hotkeys may not display correctly");
                return;
            }

            _gp = gp.gameObject;
            if (_gp.activeSelf)
            {
                _gp.SetActive(false);
            }

            _gpHidden = true;
        }

        static void ShowSenealSlot()
        {
            if (_gpHidden && _gp != null)
            {
                _gp.SetActive(true);
            }

            _gpHidden = false;
        }

        static void PlacePowers(Hud hud, RectTransform senealRoot, ConfigFile config)
        {
            // ---------- Food (new SeneaL path) ----------
            RectTransform food = senealRoot.Find("Food/fx/vitals/food") as RectTransform;
            if (food == null) food = senealRoot.Find("Food/fx/vitals") as RectTransform;
            if (food == null) food = senealRoot.Find("Food") as RectTransform;

            /*if (food == null)
            {
                Debug.LogWarning("[SeneaLHudLayout] Food element still not found");
            }
            else
            {
                Debug.Log("[SeneaLHudLayout] Food found at Food/fx/vitals/food (or parent)");
            }*/

            RememberFont(hud);
            TmpFont.Ensure();

            // ---------- Collect power slots from SeneaL's GuardianPower ----------
            var shown = new List<Transform>();

            Transform gpRoot = senealRoot.Find("GuardianPower/fx/vitals");
            /*if (gpRoot == null)
            {
                Debug.LogWarning("[SeneaLHudLayout] GuardianPower/fx/vitals not found");
                return;
            }*/

            // gp, gp1, gp2 ... gp6
            for (int i = 0; i < gpRoot.childCount; i++)
            {
                Transform child = gpRoot.GetChild(i);
                string name = child.name;

                // Accept "gp", "gp1", "gp2", ...
                if (name == "gp" || (name.StartsWith("gp") && name.Length <= 3))
                {
                    if (child.gameObject.activeInHierarchy)
                        shown.Add(child);
                }
            }

            // Debug.Log($"[SeneaLHudLayout] Found {shown.Count} SeneaL power slots under GuardianPower");

            /*if (shown.Count == 0)
            {
                Debug.LogWarning("[SeneaLHudLayout] No active power slots found");
                return;
            }*/

            // ---------- Positioning ----------
            float gap = SeneaLHudLayoutPlugin.PowersGap.Value;
            float scale = SeneaLHudLayoutPlugin.PowersScale.Value;
            float offsetX = SeneaLHudLayoutPlugin.PowersOffsetX.Value;
            float offsetY = SeneaLHudLayoutPlugin.PowersOffsetY.Value;

            float x, y;

            if (food != null)
            {
                x = HudLayout.EdgeX(food, left: false) + gap + offsetX;
                y = HudLayout.EdgeY(food, bottom: true) + offsetY;
            }
            else
            {
                // Fallback
                x = 300f + offsetX;
                y = 180f + offsetY;
            }

            // Debug.Log($"[SeneaLHudLayout] Powers settings → scale={scale}, gap={gap}, offsetX={offsetX}, offsetY={offsetY}");

            float shared = SharedCooldown();
            RefreshEffects();
            RectTransform rightIcon = null;
            float rightEdge = float.NegativeInfinity;

            for (int i = 0; i < shown.Count; i++)
            {
                // Slot index: "gp" = 1, "gp1" = 2, "gp2" = 3, etc.
                int slot = 1;
                string name = shown[i].name;
                if (name.StartsWith("gp") && name.Length > 2 && int.TryParse(name.Substring(2), out int num))
                    slot = num + 1;

                shown[i].localScale = new Vector3(scale, scale, 1f);

                // Hide original SeneaL key
                Transform originalKey = shown[i].Find("key");
                if (originalKey != null)
                    originalKey.gameObject.SetActive(false);

                bool hasBounds = TryBounds(shown[i], out float minX, out float minY, out float maxX, out float maxY);

                if (hasBounds && shown[i] is RectTransform rt)
                {
                    // Absolute positioning (stable, no flicker)
                    float width = maxX - minX;
                    float height = maxY - minY;

                    Vector3 worldTarget = new Vector3(
                        x + width * 0.5f,
                        y + height * 0.5f,
                        rt.position.z);

                    if (rt.parent != null)
                    {
                        Vector3 local = rt.parent.InverseTransformPoint(worldTarget);
                        rt.anchoredPosition = new Vector2(local.x, local.y);
                    }
                    else
                    {
                        rt.position = worldTarget;
                    }

                    x += width + gap;
                }

                string keyText = ShortcutText(config, slot);
                SetKey(shown[i], keyText, shared > 0f);

                string power = PowerName(slot);
                float buff = power == null ? 0f : EffectTimeLeft("PassivePowers " + power);
                float down = power == null ? 0f : EffectTimeLeft("PassivePowers Depletion " + power);
                SetTimer(shown[i], BuffName, buff, BuffColor, above: true);
                SetTimer(shown[i], DownName, down, DownColor, above: false);

                RectTransform icon = FindPowerIcon(shown[i]);
                if (icon != null)
                {
                    float edge = HudLayout.EdgeX(icon, left: false);
                    if (edge > rightEdge)
                    {
                        rightEdge = edge;
                        rightIcon = icon;
                    }
                }
            }

            PlaceSharedCooldown(hud, rightIcon, shared, scale);
            HidePassiveWord(hud);
        }

        static void HidePassiveWord(Hud hud)
        {
            if (hud.m_gpCooldown == null)
            {
                return;
            }

            string passive = Localization.instance != null
                ? Localization.instance.Localize("$powers_type_passive")
                : "Passive";
            if (hud.m_gpCooldown.text == passive || hud.m_gpCooldown.text == "$powers_type_passive")
            {
                hud.m_gpCooldown.text = "";
                hud.m_gpCooldown.gameObject.SetActive(false);
            }
        }

        static void HidePowerTitle(Transform container)
        {
            Transform name = container.Find("Name");
            if (name != null && name.gameObject.activeSelf)
            {
                name.gameObject.SetActive(false);
            }
        }

        static void ResetScale()
        {
            Hud hud = Hud.instance;
            if (hud == null)
            {
                return;
            }

            if (hud.m_gpRoot != null)
            {
                Transform root = hud.m_gpRoot.transform;
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child.name.StartsWith("powerContainer"))
                    {
                        child.localScale = Vector3.one;
                    }
                }
            }

            if (hud.m_gpCooldown != null)
            {
                hud.m_gpCooldown.transform.localScale = Vector3.one;
            }
        }

        static float SharedCooldown()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, player.m_guardianPowerCooldown);
        }

        static void RefreshEffects()
        {
            Effects.Clear();
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            player.GetSEMan().GetHUDStatusEffects(Effects);
        }

        static float EffectTimeLeft(string effectName)
        {
            Player player = Player.m_localPlayer;
            if (player != null && !string.IsNullOrEmpty(effectName))
            {
                StatusEffect live = player.GetSEMan().GetStatusEffect(effectName.GetStableHashCode());
                if (live != null && live.m_ttl > 0f)
                {
                    float elapsed = EffectTimeField?.GetValue(live) is float time ? time : 0f;
                    return Mathf.Max(0f, live.m_ttl - elapsed);
                }
            }

            for (int i = 0; i < Effects.Count; i++)
            {
                StatusEffect effect = Effects[i];
                if (effect != null && effect.name == effectName && effect.m_ttl > 0f)
                {
                    float elapsed = EffectTimeField?.GetValue(effect) is float time ? time : 0f;
                    return Mathf.Max(0f, effect.m_ttl - elapsed);
                }
            }

            return 0f;
        }

        static string PowerName(int slot)
        {
            Player player = Player.m_localPlayer;
            if (player == null || GuardianPowerField == null)
            {
                return null;
            }

            string equipped = GuardianPowerField.GetValue(player) as string;
            if (string.IsNullOrEmpty(equipped))
            {
                return null;
            }

            string[] parts = equipped.Split(',');
            int index = slot - 1;
            if (index < 0 || index >= parts.Length)
            {
                return null;
            }

            string name = parts[index].Trim();
            return name.Length == 0 ? null : name;
        }

        /// <summary>
        /// One shared activation cooldown for every boss power. Sit it to the
        /// right of the last icon, centered on that icon, and hide it when ready.
        /// </summary>
        static void PlaceSharedCooldown(Hud hud, RectTransform icon, float seconds, float scale)
        {
            if (hud.m_gpCooldown == null)
            {
                return;
            }

            if (seconds <= 0f || icon == null || !(hud.m_gpCooldown.transform is RectTransform cooldown))
            {
                hud.m_gpCooldown.gameObject.SetActive(false);
                return;
            }

            hud.m_gpCooldown.gameObject.SetActive(true);
            hud.m_gpCooldown.text = StatusEffect.GetTimeString(seconds, false, false).ToLowerInvariant();
            cooldown.localScale = new Vector3(scale, scale, 1f);
            float iconMid = (HudLayout.EdgeY(icon, bottom: true) + HudLayout.EdgeY(icon, bottom: false)) * 0.5f;
            float iconRight = HudLayout.EdgeX(icon, left: false);
            float cooldownMid = (HudLayout.EdgeY(cooldown, bottom: true) + HudLayout.EdgeY(cooldown, bottom: false)) * 0.5f;
            float cooldownLeft = HudLayout.EdgeX(cooldown, left: true);
            cooldown.position += new Vector3((iconRight + 8f) - cooldownLeft, iconMid - cooldownMid, 0f);
        }

        static int SlotIndex(Transform container)
        {
            string name = container.name;
            int space = name.LastIndexOf(' ');
            if (space >= 0 && int.TryParse(name.Substring(space + 1), out int index))
            {
                return index;
            }

            return 1;
        }

        static string ShortcutText(ConfigFile config, int slot)
        {
            if (!config.TryGetEntry(ShortcutSection, "Shortcut for boss power " + slot, out ConfigEntry<KeyboardShortcut> entry))
            {
                return "";
            }

            KeyboardShortcut key = entry.Value;
            if (key.MainKey == KeyCode.None)
            {
                return "";
            }

            // Temporary – remove after confirming
            // Debug.Log($"[SeneaLHudLayout] Slot {slot} shortcut = '{result}' (found={found})");

            var parts = new List<string>();
            foreach (KeyCode modifier in key.Modifiers)
            {
                parts.Add(Pretty(modifier));
            }

            parts.Add(Pretty(key.MainKey));
            return string.Join("+", parts);
        }

        static string Pretty(KeyCode key)
        {
            string name = key.ToString();
            if (name.StartsWith("Alpha"))
            {
                return name.Substring(5);
            }

            if (name == "LeftShift" || name == "RightShift")
            {
                return "Shift";
            }

            if (name == "LeftControl" || name == "RightControl")
            {
                return "Ctrl";
            }

            if (name == "LeftAlt" || name == "RightAlt")
            {
                return "Alt";
            }

            return name;
        }

        static void RememberFont(Hud hud)
        {
            if (_font != null || hud.m_gpName == null)
            {
                return;
            }

            _font = hud.m_gpName.font;
            _fontMaterial = hud.m_gpName.fontSharedMaterial;
        }

        static void SetKey(Transform container, string text, bool locked)
        {
            TextMeshProUGUI label = EnsureLabel(container, KeyName, locked ? KeyLocked : KeyReady);
            label.text = text;
            label.color = locked ? KeyLocked : KeyReady;

            if (TryBounds(container, out float minX, out float minY, out float maxX, out _))
            {
                label.rectTransform.position = new Vector3(
                    (minX + maxX) * 0.5f,
                    minY - 2f,
                    label.rectTransform.position.z);
            }
            else
            {
                // Fallback: center under the container itself
                if (container is RectTransform rt)
                {
                    float cx = (HudLayout.EdgeX(rt, left: true) + HudLayout.EdgeX(rt, left: false)) * 0.5f;
                    float bottom = HudLayout.EdgeY(rt, bottom: true);
                    label.rectTransform.position = new Vector3(cx, bottom - 2f, label.rectTransform.position.z);
                }
            }
        }

        static void SetTimer(Transform container, string name, float seconds, Color color, bool above)
        {
            TextMeshProUGUI label = EnsureLabel(container, name, color);
            if (seconds <= 0.05f)
            {
                label.gameObject.SetActive(false);
                return;
            }

            label.gameObject.SetActive(true);
            label.text = StatusEffect.GetTimeString(seconds, false, false).ToLowerInvariant();
            label.color = color;

            RectTransform anchor = above ? TopAnchor(container) : container.Find(KeyName) as RectTransform;
            if (anchor == null)
            {
                anchor = FindPowerIcon(container);
            }

            if (anchor == null)
            {
                return;
            }

            RectTransform rt = label.rectTransform;
            float center = (HudLayout.EdgeX(anchor, left: true) + HudLayout.EdgeX(anchor, left: false)) * 0.5f;
            float labelCenter = (HudLayout.EdgeX(rt, left: true) + HudLayout.EdgeX(rt, left: false)) * 0.5f;
            float dy;
            if (above)
            {
                dy = (HudLayout.EdgeY(anchor, bottom: false) + 2f) - HudLayout.EdgeY(rt, bottom: true);
            }
            else
            {
                dy = (HudLayout.EdgeY(anchor, bottom: true) - 2f) - HudLayout.EdgeY(rt, bottom: false);
            }

            rt.position += new Vector3(center - labelCenter, dy, 0f);
        }

        static RectTransform TopAnchor(Transform container)
        {
            var icon = container.Find("Icon") as RectTransform;
            var title = container.Find("Name") as RectTransform;
            if (icon == null)
            {
                return title;
            }

            if (title == null)
            {
                return icon;
            }

            return HudLayout.EdgeY(title, bottom: false) > HudLayout.EdgeY(icon, bottom: false) ? title : icon;
        }

        static TextMeshProUGUI EnsureLabel(Transform container, string name, Color color)
        {
            Transform existing = container.Find(name);
            if (existing != null)
            {
                TextMeshProUGUI current = existing.GetComponent<TextMeshProUGUI>();
                TmpFont.Apply(current);
                return current;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(container, false);
            go.SetActive(false);
            var labelRt = go.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(96f, 18f);
            TmpFont.Ensure();
            var label = go.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                label.font = _font;
                label.fontSharedMaterial = _fontMaterial;
            }

            TmpFont.Apply(label);
            label.fontSize = 13f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.color = color;
            go.SetActive(true);

            return label;
        }

        static bool TryBounds(Transform root, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = float.PositiveInfinity;
            minY = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            maxY = float.NegativeInfinity;
            bool any = false;
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child.name == KeyName || child.name == BuffName || child.name == DownName)
                    {
                        continue;
                    }

                    stack.Push(child);
                    if (!(child is RectTransform rt) || !rt.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    float left = HudLayout.EdgeX(rt, left: true);
                    float right = HudLayout.EdgeX(rt, left: false);
                    float bottom = HudLayout.EdgeY(rt, bottom: true);
                    float top = HudLayout.EdgeY(rt, bottom: false);
                    if (left < minX) minX = left;
                    if (right > maxX) maxX = right;
                    if (bottom < minY) minY = bottom;
                    if (top > maxY) maxY = top;
                    any = true;
                }
            }

            return any;
        }
        static RectTransform FindPowerIcon(Transform container)
        {
            // SeneaL uses "power"
            Transform t = container.Find("power");
            if (t is RectTransform rt && rt.gameObject.activeInHierarchy)
                return rt;

            // Fallbacks
            string[] candidates = { "Icon", "icon", "PowerIcon", "powerIcon", "Image", "image" };
            foreach (string name in candidates)
            {
                t = container.Find(name);
                if (t is RectTransform r && r.gameObject.activeInHierarchy)
                    return r;
            }

            // Last resort
            var images = container.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.activeInHierarchy && img.rectTransform != null)
                    return img.rectTransform;
            }

            return null;
        }
    }
}