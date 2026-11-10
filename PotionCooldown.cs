using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// [Items] PotionCooldowns: a countdown timer on the action-slot potions.
    ///
    /// A PotionsPlus potion (and every other Valheim consumable) blocks re-use while its consume
    /// status effect is active - the vanilla gate is Player.CanConsumeItem refusing the item when
    /// GetSEMan() already has that effect (or its category). "PotionsPlus Cooldown Duration" is that
    /// effect's m_ttl. So the timer is simply the active effect's GetRemaningTime().
    ///
    /// For each SeneaL UI action slot showing such an item, while its consume effect is active the
    /// slot is dimmed and the whole seconds left are drawn over it. Nothing is polled per item every
    /// frame: the check runs on a 0.1s cadence, and the label text is written only when the displayed
    /// second changes, so an idle HUD costs a dozen field reads ten times a second.
    /// </summary>
    static class PotionCooldown
    {
        const string OverlayName = "SeneaLHudLayout_CoolDim";
        const string LabelName = "SeneaLHudLayout_CoolTime";
        const float Interval = 0.1f;

        static readonly Color Dim = new Color(0.08f, 0.07f, 0.05f, 0.55f);
        static readonly Dictionary<object, int> Shown = new Dictionary<object, int>();

        static float _next;
        static bool _on = true;

        public static void Tick()
        {
            bool on = SeneaLHudLayoutPlugin.PotionCooldowns == null
                || SeneaLHudLayoutPlugin.PotionCooldowns.Value;

            if (!on)
            {
                if (_on)
                {
                    _on = false;
                    Clear();
                }

                return;
            }

            _on = true;
            if (Time.unscaledTime - _next < Interval)
            {
                return;
            }

            _next = Time.unscaledTime + Interval;
            Update();
        }

        static void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            SEMan seman = player.GetSEMan();
            if (seman == null)
            {
                return;
            }

            // 1.5.45: "could this be a cooldown timer for other things too?" - yes. The main hotbar
            // row (1-4: food, meads, anything with a consume cooldown) rides the same feature as the
            // action slots; the check is the same status-effect lookup the vanilla gate uses, so
            // every consumable counts, not only potions.
            List<object> slots = SlotCue.HotbarSlots();
            slots.AddRange(SlotCue.ActionSlots());
            for (int i = 0; i < slots.Count; i++)
            {
                Paint(slots[i], seman);
            }
        }

        static void Paint(object slot, SEMan seman)
        {
            ItemDrop.ItemData item = SlotCue.ItemFor(slot);
            float left = Left(item, seman);
            if (left < 1f)
            {
                Hide(slot);
                return;
            }

            int seconds = Mathf.CeilToInt(left);

            Image dim = SlotCue.EnsureSlotOverlay(slot, OverlayName);
            if (dim != null)
            {
                if (!dim.gameObject.activeSelf)
                {
                    dim.gameObject.SetActive(true);
                }

                // Alpha is [Items] PotionCooldownDimOpacity (0.55 default); 0 leaves the slot
                // undimmed, so the icon behind the countdown stays fully bright.
                float alpha = SeneaLHudLayoutPlugin.PotionCooldownDimOpacity == null
                    ? Dim.a
                    : Mathf.Clamp01(SeneaLHudLayoutPlugin.PotionCooldownDimOpacity.Value);
                Color tint = Dim;
                tint.a = alpha;
                dim.color = tint;

                if (alpha <= 0f)
                {
                    dim.gameObject.SetActive(false);
                }
            }

            TextMeshProUGUI label = SlotCue.EnsureSlotLabel(slot, LabelName);
            if (label == null)
            {
                return;
            }

            bool changed = !Shown.TryGetValue(slot, out int last) || last != seconds;
            Shown[slot] = seconds;
            if (changed)
            {
                // Half the old size (11) and TOP-aligned in the slot: the number hugs the slot's
                // top edge so the potion icon below stays readable (user: "hard to see what potion
                // it's sitting on").
                SlotCue.ShowSlotSeconds(slot, label, seconds, SlotCue.SlotCountText(slot),
                    11f, TextAlignmentOptions.Top);
            }
            else if (!label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Seconds of consume cooldown left for an item, 0 when it is not on cooldown. The consume
        /// effect active in the manager is the source of truth: it carries the level applied on drink,
        /// and it disappears the moment the item is drinkable again.
        /// </summary>
        static float Left(ItemDrop.ItemData item, SEMan seman)
        {
            if (item == null || item.m_shared == null)
            {
                return 0f;
            }

            StatusEffect consume = item.m_shared.m_consumeStatusEffect;
            if (consume == null)
            {
                return 0f;
            }

            StatusEffect active = seman.GetStatusEffect(consume.NameHash());
            if (active == null)
            {
                return 0f;
            }

            float remaining = active.GetRemaningTime();
            return remaining > 0f ? remaining : 0f;
        }

        static void Hide(object slot)
        {
            Shown.Remove(slot);

            RectTransform root = SlotCue.SlotRoot(slot);
            if (root == null)
            {
                return;
            }

            Transform dim = root.Find(OverlayName);
            if (dim != null && dim.gameObject.activeSelf)
            {
                dim.gameObject.SetActive(false);
            }

            Transform label = root.Find(LabelName);
            if (label != null && label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(false);
            }
        }

        static void Clear()
        {
            List<object> slots = SlotCue.HotbarSlots();
            slots.AddRange(SlotCue.ActionSlots());
            for (int i = 0; i < slots.Count; i++)
            {
                Hide(slots[i]);
            }

            Shown.Clear();
        }
    }
}
