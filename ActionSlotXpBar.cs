using System;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// [Hotbar] ShowXpBar: the character window's level/XP display copied to the HUD, attached
    /// flush to the right side of the action slots and centred on their exact middle.
    ///
    ///   ┌ Level 37   [▓▓▓▓▓░░░░░░░] 37% ┐   <- one card, thin 30%-alpha gold border around
    ///   │ 1234 / 3333                   │      everything; numbers plate: full card width,
    ///   └───────────────────────────────┘      flush under the top row, left-aligned at the
    ///                                          bar's start, lifted toward the bar
    ///
    /// The root is a ZERO-SIZE point at the slots' right edge and the HUD placement code skips
    /// this overlay when it measures the hotbar (HudLayout.FindHotbarBounds), so turning the bar
    /// on moves nothing on screen — not the action slots, not the vitals, nothing.
    ///
    /// Text uses the food-timer label's font and material (the readable style, as the food-bar
    /// player level); NO outline properties, which scramble Valheim HUD text (the 1.5.30 bug).
    /// [Hotbar] XpBarScale sizes the whole feature (1 = the character window's own size).
    /// </summary>
    static class ActionSlotXpBar
    {
        const string RootName = "SeneaLHudLayout_SlotXp";

        // The character window draws at 12/100x6/10; XpBarScale multiplies that base.
        const float BaseLevelFont = 12f;
        const float BaseBarWidth = 100f;
        const float BaseBarHeight = 6f;
        const float BaseSmallFont = 10f;
        const float BaseInnerGap = 8f;   // level text -> bar, inside the one top plate
        const float BaseBarGap = 6f;     // bar -> percent text, inside the one top plate
        const float PlatePadX = 7f;
        const float PlatePadY = 4f;
        const float BaseNumbersPad = 6f;  // 1.5.50: 4px (1.5.49) + 2 more below the numbers
        const float PlateMinHeight = 18f;
        const float SlotGap = 12f;

        // Card plates: black (1.5.44 made them opaque). 1.5.45: the alpha is [Hotbar] XpBarOpacity
        // (0.5 default, live) — the user asked for a 50% black backdrop; the gold border is the
        // card's only gold and the plates stay solid at 1. Tick re-applies on drift, F1 style.
        static readonly Color PlateFill = new Color(0.05f, 0.04f, 0.03f, 0.5f);
        static readonly Color GoldLine = new Color(0.93f, 0.82f, 0.52f, 1f);
        static readonly Color TextColor = new Color(0.93f, 0.82f, 0.52f, 1f);

        // 1.5.50: the border's colour is the gold at [Hotbar] XpBarBorderOpacity (0.15 default,
        // "reduce its opacity ANOTHER 10 percent" from 0.25). The gold TEXT keeps full alpha.
        static readonly Color BorderGold = new Color(0.93f, 0.82f, 0.52f, 0.15f);

        // Progress fill: GOLD (1.5.48, the visible progress bar; the knob to switch it back to
        // black was removed in 1.5.51 - invisible progress is never wanted).
        static readonly Color FillGold = GoldLine;

        static Transform _root;
        static Image _plateTop;
        static Image _plateAmount;

        // 1.5.47: THE BORDER IS FOUR LINE QUADS, NOT A 9-SLICED SPRITE. For six rounds the gold
        // "border" rendered as a solid gold rectangle filling the card (the 1.5.45/1.5.46 reports,
        // the user's own diagnosis: "you're drawing rectangles instead of making a real border"):
        // the 9-sliced XpGoldFrame sprite kept painting its opaque rim pixels across the whole
        // face. Four solid quads - one strip each for top/bottom/left/right - are geometrically
        // four lines: no quad has a "centre" to fill, so a filled gold rectangle is impossible.
        // Toggle: [Hotbar] XpBarBorder, live.
        // 1.5.49: 0.5px ("the Border lines are still too thick, cut in half its current size"),
        // alpha 0.25 ([Hotbar] XpBarBorderOpacity). Both knobs are live; the width is scale-scaled.
        const float BaseBorderThickness = 0.5f;
        static Image _borderTop;
        static Image _borderBottom;
        static Image _borderLeft;
        static Image _borderRight;
        static bool? _placedBorder;
        static float _placedBorderWidth = float.NaN;
        static float? _placedBorderAlpha;

        // The card's own box, recorded by Place so the live border-width knob can re-place the
        // four strips without re-measuring the labels.
        static float _cardW;
        static float _cardTop;
        static float _cardBottom;
        static float _cardCenterY;
        static float _cardH;
        static float BorderThickness => BorderWidthPx();

        static Image _back;
        static Image _fill;
        static TextMeshProUGUI _level;
        static TextMeshProUGUI _amount;
        static TextMeshProUGUI _percent;

        static System.Reflection.FieldInfo _hotbarField;
        static System.Reflection.FieldInfo _frameActions;
        static System.Reflection.FieldInfo _frameQuiver;
        static bool _lookup;

        // Whole-feature size from [Hotbar] XpBarScale; recomputed only when the setting changes.
        static float _scale = 1f;
        static float LevelFont => BaseLevelFont * _scale;
        static float SmallFont => BaseSmallFont * _scale;
        static float BarWidth => BaseBarWidth * _scale;
        static float BarHeight => Mathf.Max(4f, BaseBarHeight * _scale);
        static float InnerGap => BaseInnerGap * _scale;
        static float BarGap => BaseBarGap * _scale;
        static float PadX => PlatePadX * _scale;
        static float PadY => PlatePadY * _scale;
        static float NumbersPad => BaseNumbersPad * _scale;

        // Last placement / content, so the whole tick is a few reads and an early return.
        static float _placedRight = float.NaN;
        static float _placedCenter = float.NaN;
        static float _placedLevelWidth = float.NaN;
        static float _placedAmountWidth = float.NaN;
        static float _placedPercentWidth = float.NaN;
        static float _placedScale = float.NaN;
        static string _placedText = "";
        static float _placedFill = float.NaN;

        // The font copy is best-effort: SeneaL's food-timer label (the font source) only
        // exists once its HUD is built. While the copy fell back to TMP's default font,
        // retry once a second.
        static bool _fontApplied;
        static float _nextFont;

        // The SkillsReworked numbers arrive through reflection, so the data reads run on a 0.1s
        // cadence (same posture as the potion cooldown timers); following the slots stays per frame.
        const float DataInterval = 0.1f;
        static float _nextData;
        static int _dataLevel = -1;
        static float _dataFraction = -1f;
        static float _dataCurrent;
        static float _dataRequired;

        // The bar's black track alpha from [Hotbar] XpBarTrackOpacity (the user's 25%: see the
        // scenery through it). Live knob so "how see-through" is a slider, not a build.
        const float DefaultTrackOpacity = 0.25f;
        static float _placedTrack = float.NaN;

        static float TrackOpacity()
        {
            ConfigEntry<float> entry = SeneaLHudLayoutPlugin.ShowSlotXpBarTrackOpacity;
            float alpha = entry != null ? entry.Value : DefaultTrackOpacity;
            return Mathf.Clamp01(alpha);
        }

        // The CARD plates' black alpha from [Hotbar] XpBarOpacity. 1.5.46 default 0.75: the 0.5 the
        // user asked for measured out to "completely missing" over bright scenery (a 50% dark-warm
        // plate washes to light grey in snow/sky), so the knob keeps 0.5 one slider away and the
        // shipped default stays readable. Live knob.
        const float DefaultPlateOpacity = 0.75f;
        static float _placedPlate = float.NaN;

        static float PlateAlpha()
        {
            ConfigEntry<float> entry = SeneaLHudLayoutPlugin.ShowSlotXpBarOpacity;
            float alpha = entry != null ? entry.Value : DefaultPlateOpacity;
            return Mathf.Clamp01(alpha);
        }

        // The four-line border on/off from [Hotbar] XpBarBorder (live).
        static bool BorderOn()
        {
            ConfigEntry<bool> entry = SeneaLHudLayoutPlugin.ShowSlotXpBarBorder;
            return entry == null || entry.Value;
        }

        // Border line thickness in HUD pixels from [Hotbar] XpBarBorderWidth (0.5 default, live).
        static float BorderWidthPx()
        {
            ConfigEntry<float> entry = SeneaLHudLayoutPlugin.ShowSlotXpBarBorderWidth;
            float width = entry != null ? entry.Value : BaseBorderThickness;
            return Mathf.Clamp(width, 0.1f, 4f) * _scale;
        }

        // The border's colour from [Hotbar] XpBarBorderOpacity (0.15 default, live).
        static Color BorderColor()
        {
            ConfigEntry<float> entry = SeneaLHudLayoutPlugin.ShowSlotXpBarBorderOpacity;
            float alpha = entry != null ? Mathf.Clamp01(entry.Value) : 0.15f;
            Color gold = BorderGold;
            gold.a = alpha;
            return gold;
        }

        // 1.5.50: the DIGITS go white via TMP rich text ([Hotbar] XpBarWhiteNumbers, live) while
        // the label colour stays gold, so the untagged symbols ("Level ", " / ", "%") inherit gold.
        static bool DigitsWhite()
        {
            ConfigEntry<bool> entry = SeneaLHudLayoutPlugin.ShowSlotXpBarWhiteNumbers;
            return entry == null || entry.Value;
        }

        static string Digits(string number, bool white)
        {
            return white ? "<color=#FFFFFFFF>" + number + "</color>" : number;
        }

        // Lay the four border strips on the card box Place recorded. Called from Place and
        // whenever the live width knob moves, so a thickness change needs no re-measure.
        static void PlaceBorder()
        {
            if (_borderTop == null || _cardW <= 0f)
            {
                return;
            }

            float t = BorderThickness;
            _borderTop.rectTransform.localPosition = new Vector3(_cardW * 0.5f, _cardTop - t * 0.5f, 0f);
            _borderTop.rectTransform.sizeDelta = new Vector2(_cardW, t);
            _borderBottom.rectTransform.localPosition = new Vector3(_cardW * 0.5f, _cardBottom + t * 0.5f, 0f);
            _borderBottom.rectTransform.sizeDelta = new Vector2(_cardW, t);
            _borderLeft.rectTransform.localPosition = new Vector3(t * 0.5f, _cardCenterY, 0f);
            _borderLeft.rectTransform.sizeDelta = new Vector2(t, _cardH - 2f * t);
            _borderRight.rectTransform.localPosition = new Vector3(_cardW - t * 0.5f, _cardCenterY, 0f);
            _borderRight.rectTransform.sizeDelta = new Vector2(t, _cardH - 2f * t);
            _placedBorderWidth = t;
        }

        public static void Tick()
        {
            bool show = SeneaLHudLayoutPlugin.ShowSlotXpBar != null
                && SeneaLHudLayoutPlugin.ShowSlotXpBar.Value
                && Player.m_localPlayer != null;

            if (!show)
            {
                Hide();
                return;
            }

            RectTransform anchor = Anchor();
            RectTransform shift = HudLayout.HotbarShift;
            if (anchor == null || shift == null || !anchor.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            if (!Ensure(shift))
            {
                return;
            }

            if (!_fontApplied && Time.unscaledTime >= _nextFont)
            {
                _nextFont = Time.unscaledTime + 1f;
                CharacterExtras.CopyFoodBarFont(_level);
                CharacterExtras.CopyFoodBarFont(_percent);
                CharacterExtras.CopyFoodBarFont(_amount);
                KillLabelGlow();
                _fontApplied = HasFoodFont(_level);
            }

            if (!_diagDone && Time.unscaledTime >= _diagAt)
            {
                _diagDone = true;
                Debug.Log("[SeneaL HUD Layout] XP diag: " + Diag());
                Debug.Log("[SeneaL HUD Layout] XP cover: " + Probe());
            }

            if (Time.unscaledTime >= _nextData)
            {
                _nextData = Time.unscaledTime + DataInterval;
                SyncOrder();
                GuardCover();
                _dataLevel = CharacterExtras.Level(Player.m_localPlayer);

                // Fraction resolves the SkillsReworked reflection ReadLevelXp also uses, so call
                // it first; a negative level means SkillsReworked is not answering.
                _dataFraction = _dataLevel < 0 ? -1f : Mathf.Clamp01(CharacterExtras.Fraction(Player.m_localPlayer));
                if (_dataLevel >= 0)
                {
                    CharacterExtras.Xp(Player.m_localPlayer, out _dataCurrent, out _dataRequired);
                }
            }

            if (_dataLevel < 0)
            {
                Hide();
                return;
            }

            float scale = SeneaLHudLayoutPlugin.ShowSlotXpBarScale == null
                ? 1f
                : Mathf.Clamp(SeneaLHudLayoutPlugin.ShowSlotXpBarScale.Value, 0.5f, 3f);
            if (!Mathf.Approximately(scale, _scale))
            {
                _scale = scale;
                _placedFill = float.NaN;  // BarHeight changed: re-run the fill write at the new size
            }

            float fraction = _dataFraction < 0f ? 0f : _dataFraction;

            // 1.5.50: DIGITS WHITE, SYMBOLS GOLD ([Hotbar] XpBarWhiteNumbers, live). TMP rich text:
            // the digits ride in a <color=#FFFFFF> run, and "Level ", " / " and "%" stay untagged so
            // they inherit the label's gold. Colour tags add no glyphs, so the plate sizing
            // (preferredWidth) measures exactly as before, and the change key below flips with it.
            bool white = DigitsWhite();

            string levelText = "Level " + Digits(_dataLevel.ToString(), white);
            string amount = Digits(CharacterExtras.XpText(_dataCurrent), white)
                + " / "
                + Digits(CharacterExtras.XpText(_dataRequired), white);
            string percent = Digits(Mathf.RoundToInt(fraction * 100f).ToString(), white) + "%";
            string content = levelText + "|" + amount + "|" + percent + "|" + _scale;

            // Text is measured in the same step it changes, so the plates are sized to the words.
            bool changed = content != _placedText;
            if (changed)
            {
                _level.text = levelText;
                _amount.text = amount;
                _percent.text = percent;
                _level.ForceMeshUpdate();
                _amount.ForceMeshUpdate();
                _percent.ForceMeshUpdate();
            }

            float levelWidth = Mathf.Ceil(_level.preferredWidth);
            float amountWidth = Mathf.Ceil(_amount.preferredWidth);
            float percentWidth = Mathf.Ceil(_percent.preferredWidth);

            // Right edge and exact vertical centre of the widest visible right-hand row (action
            // slots, quiver), in the shift's space. Both boxes are re-framed by SeneaL every tick.
            Vector2 attach = AttachPoint(anchor, shift);

            if (changed
                || !Mathf.Approximately(attach.x, _placedRight)
                || !Mathf.Approximately(attach.y, _placedCenter)
                || !Mathf.Approximately(levelWidth, _placedLevelWidth)
                || !Mathf.Approximately(amountWidth, _placedAmountWidth)
                || !Mathf.Approximately(percentWidth, _placedPercentWidth)
                || !Mathf.Approximately(_scale, _placedScale))
            {
                _root.localPosition = new Vector3(attach.x, attach.y, 0f);
                Place(levelWidth, amountWidth, percentWidth);
                _placedRight = attach.x;
                _placedCenter = attach.y;
                _placedLevelWidth = levelWidth;
                _placedAmountWidth = amountWidth;
                _placedPercentWidth = percentWidth;
                _placedScale = _scale;
                _placedText = content;
            }

            // The filled part of the track is GOLD (1.5.48): the visible progress. (1.5.51: the
            // XpBarFillGold knob was removed - there is no reason to switch it off, the dark-black
            // fill of 1.5.41 was invisible progress, which is what the 1.5.48 report was.)
            // The very first write must happen even from the NaN start: Mathf.Abs(NaN - x) is
            // false, which is what left the 1.5.29 fill at Unity's default 100x100 box.
            float innerWidth = Mathf.Max(0f, BarWidth - 2f);
            float fill = innerWidth * fraction;
            if (float.IsNaN(_placedFill) || Mathf.Abs(fill - _placedFill) > 0.1f)
            {
                _fill.rectTransform.sizeDelta = new Vector2(fill, Mathf.Max(0f, BarHeight - 2f));
                _fill.rectTransform.anchoredPosition = new Vector2(1f, 0f);
                _placedFill = fill;
            }

            // Track opacity from [Hotbar] XpBarTrackOpacity, re-applied only when it drifts
            // (the F1 slider writes the entry live, so no restart needed).
            float track = TrackOpacity();
            if (float.IsNaN(_placedTrack) || Mathf.Abs(track - _placedTrack) > 0.01f)
            {
                _back.color = new Color(1f, 1f, 1f, track);
                _placedTrack = track;
            }

            // 1.5.45: the CARD plates' alpha from [Hotbar] XpBarOpacity (0.5 default, the user's
            // "give it an opacity of 50%" after 1.5.44's solid black). Same live-drift pattern.
            float plate = PlateAlpha();
            if (float.IsNaN(_placedPlate) || Mathf.Abs(plate - _placedPlate) > 0.01f)
            {
                Color top = PlateFill;
                top.a = plate;
                _plateTop.color = top;
                _plateAmount.color = top;
                _placedPlate = plate;
            }

            // 1.5.47: the four-line border on/off from [Hotbar] XpBarBorder, live.
            bool border = BorderOn();
            if (_placedBorder == null || _placedBorder.Value != border)
            {
                _borderTop.enabled = border;
                _borderBottom.enabled = border;
                _borderLeft.enabled = border;
                _borderRight.enabled = border;
                _placedBorder = border;
            }

            // 1.5.49: the border's alpha from [Hotbar] XpBarBorderOpacity (0.25 default) and its
            // thickness from [Hotbar] XpBarBorderWidth (0.5 default), both live F1 knobs.
            Color borderColor = BorderColor();
            if (_placedBorderAlpha == null || Mathf.Abs(borderColor.a - _placedBorderAlpha.Value) > 0.01f)
            {
                _borderTop.color = borderColor;
                _borderBottom.color = borderColor;
                _borderLeft.color = borderColor;
                _borderRight.color = borderColor;
                _placedBorderAlpha = borderColor.a;
            }

            float borderWidth = BorderThickness;
            if (float.IsNaN(_placedBorderWidth) || Mathf.Abs(borderWidth - _placedBorderWidth) > 0.01f)
            {
                PlaceBorder();
            }

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// The bar hugs the ACTION SLOTS (the user's reference), falling back to the quiver frame
        /// only when the action slots are not on screen.
        /// </summary>
        static RectTransform Anchor()
        {
            EnsureLookup();
            RectTransform actions = Frame(_frameActions);
            RectTransform quiver = Frame(_frameQuiver);
            if (actions != null && actions.gameObject.activeInHierarchy)
            {
                return actions;
            }

            if (quiver != null && quiver.gameObject.activeInHierarchy)
            {
                return quiver;
            }

            return actions != null ? actions : quiver;
        }

        static RectTransform Frame(System.Reflection.FieldInfo field)
        {
            if (field == null || _hotbarField == null)
            {
                return null;
            }

            object hotbar = _hotbarField.GetValue(null);
            return hotbar == null ? null : field.GetValue(hotbar) as RectTransform;
        }

        static float RightWorld(RectTransform rt)
        {
            return rt.TransformPoint(new Vector3(rt.rect.xMax, 0f, 0f)).x;
        }

        /// <summary>
        /// (x, y) of the attachment point in the shift's space: the anchor's CONTENT right edge
        /// (the last visible slot, not the frame's padded rect), at the anchor's exact rect centre
        /// — the vertical middle of the action-slot buttons.
        /// </summary>
        static Vector2 AttachPoint(RectTransform anchor, RectTransform shift)
        {
            float rightX = ContentRight(anchor);
            Vector3 rightWorld = anchor.TransformPoint(new Vector3(rightX, 0f, 0f));
            Vector3 centerWorld = anchor.TransformPoint(new Vector3(0f, anchor.rect.center.y, 0f));
            Vector2 rightLocal = shift.InverseTransformPoint(rightWorld);
            Vector2 centerLocal = shift.InverseTransformPoint(centerWorld);
            return new Vector2(rightLocal.x + SlotGap, centerLocal.y);
        }

        /// <summary>
        /// The widest extent of the anchor's own ACTIVE children in the anchor's local space —
        /// the last visible slot's right edge. The frame's rect extends past the last slot with
        /// padding, and the gap the user reported was that padding.
        /// </summary>
        static float ContentRight(RectTransform frame)
        {
            float right = float.NegativeInfinity;
            for (int i = 0; i < frame.childCount; i++)
            {
                if (frame.GetChild(i) is RectTransform child && child.gameObject.activeInHierarchy)
                {
                    right = Mathf.Max(right, child.rect.xMax + child.anchoredPosition.x);
                }
            }

            return float.IsNegativeInfinity(right) ? frame.rect.xMax : Mathf.Min(right, frame.rect.xMax);
        }

        /// <summary>
        /// Lay out the card in the zero-size root (root origin = slots' right edge, at their mid
        /// height; y is up):
        ///   top row:    [Level 37]  gap  [bar]  gap  [37%]
        ///   bottom row: [1234 / 3333]  hugging its numbers, lifted close under the bar,
        ///               FULL card width, left-aligned at the bar's start (like the character panel)
        /// One thin 30%-alpha gold frame outlines the whole card (top edge, both sides, bottom
        /// edge). The top plate's centre sits at the root origin's height, so the level/bar/% row
        /// is exactly on the slots' vertical middle.
        /// </summary>
        static void Place(float levelWidth, float amountWidth, float percentWidth)
        {
            float topRowH = Mathf.Max(14f * _scale, LevelFont + 6f * _scale);
            // The numbers plate hugs its digits and is flush under the top plate, and the bar
            // sits at the BOTTOM of the top plate (not centred in it): the digits start ~2px
            // under the bar (1.5.36 shipped with the numbers' CENTRE at the same y as 1.5.35 -
            // the plate shrank around unmoved text, so "moved up" was invisible).
            // 1.5.49: THE PAD GOES UNDER THE DIGITS, NOT ABOVE THEM. 1.5.45/1.5.48 slid the whole
            // numbers plate DOWN by the pad, which the user read exactly right: "it pushed down on
            // the wrong side. you added padding above the numbers but below the xp bar itself".
            // The plate is now TALLER (digitsRowH + pad) with the digits moved UP inside it by
            // pad/2, and the plate stays flush under the top plate: gap bar->digits is back to the
            // 1.5.44 look (which the user liked), and every pad pixel sits BELOW "1234 / 3333",
            // above the card's bottom border.
            float digitsRowH = SmallFont + 2f * _scale;
            float amountRowH = digitsRowH + NumbersPad;

            float barX = PadX + levelWidth + InnerGap;
            // The numbers plate spans the same left-to-right length as the row above it.
            float cardW = barX + BarWidth + BarGap + percentWidth + PadX;

            // The CARD (frame) stays centred on the slots' mid height (the anchor the user set):
            // the pad-free topRowH*0.25 + digitsRowH*0.5 is the 1.5.44 centring the user approved;
            // the pad only ever grows the plate downwards, so the top row and bar never move.
            float topCenterY = topRowH * 0.25f + digitsRowH * 0.5f;
            float amountCenterY = topCenterY - topRowH * 0.5f - amountRowH * 0.5f;
            // Bar 1 unit above the top plate's bottom edge; text rides at the bar's height so
            // "Level 37" and "37%" sit on the bar, not above it.
            float barOffsetY = -(topRowH - BarHeight) * 0.5f + 1f * _scale;
            float cardTop = topCenterY + topRowH * 0.5f;
            float cardBottom = amountCenterY - amountRowH * 0.5f;
            float cardCenterY = (cardTop + cardBottom) * 0.5f;
            float cardH = cardTop - cardBottom;

            _plateTop.rectTransform.localPosition = new Vector3(cardW * 0.5f, topCenterY, 0f);
            _plateTop.rectTransform.sizeDelta = new Vector2(cardW, topRowH);

            _level.rectTransform.localPosition = new Vector3(
                -cardW * 0.5f + PadX + levelWidth * 0.5f, barOffsetY, 0f);
            _level.rectTransform.sizeDelta = new Vector2(levelWidth, topRowH);

            _back.rectTransform.localPosition = new Vector3(
                -cardW * 0.5f + barX + BarWidth * 0.5f, barOffsetY, 0f);
            _back.rectTransform.sizeDelta = new Vector2(BarWidth, BarHeight);

            _percent.rectTransform.localPosition = new Vector3(
                -cardW * 0.5f + barX + BarWidth + BarGap + percentWidth * 0.5f, barOffsetY, 0f);
            _percent.rectTransform.sizeDelta = new Vector2(percentWidth, topRowH);

            _plateAmount.rectTransform.localPosition = new Vector3(cardW * 0.5f, amountCenterY, 0f);
            _plateAmount.rectTransform.sizeDelta = new Vector2(cardW, amountRowH);

            // Left-aligned at the bar's start (barX), not centred under the bar. 1.5.49: the plate
            // grew DOWNWARD by NumbersPad, so its centre is pad/2 lower than the digits' old spot;
            // the digits ride pad/2 ABOVE the plate centre, back at the 1.5.44 position, and all
            // the pad stays under them.
            _amount.rectTransform.localPosition = new Vector3(
                -cardW * 0.5f + barX + amountWidth * 0.5f, NumbersPad * 0.5f, 0f);
            _amount.rectTransform.sizeDelta = new Vector2(amountWidth, digitsRowH);

            // The border outlines the CARD: x spans 0..cardW from the root origin, y spans
            // cardH around cardCenterY (centring it on the root origin left it half a card width
            // left, the 1.5.34 report). 1.5.47: four line quads - horizontals span the full card
            // width, verticals run between them, so the corners are mitred with no overlap.
            _cardW = cardW;
            _cardTop = cardTop;
            _cardBottom = cardBottom;
            _cardCenterY = cardCenterY;
            _cardH = cardH;
            PlaceBorder();
        }

        static bool Ensure(RectTransform shift)
        {
            if (_root != null && _root.parent == shift)
            {
                return true;
            }

            Transform existing = shift.Find(RootName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject go = new GameObject(RootName, typeof(RectTransform));
            go.transform.SetParent(shift, false);
            go.layer = shift.gameObject.layer;
            _root = go.transform;

            // Nested canvas, one step above the HUD canvas: other mods stretch overlays across
            // the whole hotbar (Skyheim Compat clones skyheim_cooldown onto every slot root and
            // SetAsLastSibling), and a gold overlay drawn after our siblings painted OVER the
            // finished card - "the yellow is ON TOP of the whole bar". The card canvas sorts one
            // above the HUD canvas's own order, so it draws over those overlays; separate canvases
            // for popups/tooltips keep their normal order above the card.
            Canvas parentCanvas = shift.GetComponentInParent<Canvas>();
            Canvas cardCanvas = go.AddComponent<Canvas>();
            cardCanvas.overrideSorting = true;
            cardCanvas.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 1;
            _cardCanvas = cardCanvas;
            _cardParentCanvas = parentCanvas;

            // A zero-size point with centred pivot: the root's rect is never counted in the
            // hotbar's bounds, and plates position themselves around the origin (0, 0.5 anchors).
            RectTransform rootRt = (RectTransform)_root;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.zero;
            rootRt.pivot = new Vector2(0f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            rootRt.localScale = Vector3.one;

            // Cards build stamp: settles "am I looking at the new build?" from LogOutput.txt.
            Debug.Log(
                "[SeneaL HUD Layout] XP bar built, v" + SeneaLHudLayoutPlugin.VERSION);

            // Draw order = sibling order: the two plates first, then the gold frame LAST among
            // the root's boxes so it outlines the whole card over the plates; text lives inside
            // its plate and draws over that plate. (The 1.5.35 shadow strip is gone: its gradient
            // spread past the card's corners and read as a yellow glow on the side borders.)
            _plateTop = MakePlate("plateTop", _root);
            _plateAmount = MakePlate("plateAmount", _root);

            // 1.5.47: the border is FOUR thin solid quads (drawn last = on top of the plates),
            // never a 9-sliced sprite. The old XpGoldFrame is gone from the build for good.
            _borderTop = MakeLine("borderTop", _root);
            _borderBottom = MakeLine("borderBottom", _root);
            _borderLeft = MakeLine("borderLeft", _root);
            _borderRight = MakeLine("borderRight", _root);
            _placedBorder = null;
            _placedBorderWidth = float.NaN;
            _placedBorderAlpha = null;

            _back = MakeImage("bar", _plateTop.transform);
            _back.color = Color.white;  // Tick applies the track alpha on the first frame
            _back.sprite = BarFrame();
            _back.type = Image.Type.Sliced;

            _fill = MakeImage("fill", _back.transform);
            _fill.color = FillGold;

            _level = MakeLabel("level", _plateTop.transform, TextAlignmentOptions.MidlineLeft, LevelFont);
            _percent = MakeLabel("percent", _plateTop.transform, TextAlignmentOptions.MidlineLeft, SmallFont);
            _amount = MakeLabel("amount", _plateAmount.transform, TextAlignmentOptions.Midline, SmallFont);

            CenterAll();

            RectTransform fillRt = _fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            // Start collapsed: never Unity's default 100x100, which is what sprawled over the bar.
            fillRt.anchoredPosition = new Vector2(1f, 0f);
            fillRt.sizeDelta = Vector2.zero;

            _placedRight = float.NaN;
            _placedLevelWidth = float.NaN;
            _placedAmountWidth = float.NaN;
            _placedPercentWidth = float.NaN;
            _placedScale = float.NaN;
            _placedText = "";
            _placedFill = float.NaN;
            _placedTrack = float.NaN;
            _placedPlate = float.NaN;
            _placedBorder = null;
            _diagAt = Time.unscaledTime + 2f;
            _diagDone = false;
            return true;
        }

        static float _diagAt;
        static bool _diagDone;

        // The card's own canvas, sorted just above the HUD canvas so foreign hotbar overlays
        // cannot paint over it. The HUD canvas's order is re-read on the data cadence because
        // SeneaL re-asserts it when the HUD shows.
        static Canvas _cardCanvas;
        static Canvas _cardParentCanvas;

        static void SyncOrder()
        {
            if (_cardCanvas == null)
            {
                return;
            }

            if (_cardParentCanvas == null || _cardParentCanvas == _cardCanvas.rootCanvas)
            {
                _cardParentCanvas = _cardCanvas.transform.parent != null
                    ? _cardCanvas.transform.parent.GetComponentInParent<Canvas>()
                    : null;
                if (_cardParentCanvas == null || _cardParentCanvas == _cardCanvas)
                {
                    return;
                }
            }

            int want = _cardParentCanvas.sortingOrder + 1;
            if (_cardCanvas.sortingOrder != want)
            {
                _cardCanvas.sortingOrder = want;
            }
        }

        /// <summary>
        /// The card's live state, one log line: colours, sprites, sizes, the label font material
        /// (the copied SeneaL food-timer material is the last unproven suspect for the pale-yellow
        /// panel) and every CanvasGroup alpha above the card. This ends the guessing: the user
        /// pastes one line and the yellow pixel is identified.
        /// </summary>
        static string Diag()
        {
            var sb = new System.Text.StringBuilder(512);
            sb.Append("plateTop ").Append(Box(_plateTop));
            sb.Append(" | plateAmount ").Append(Box(_plateAmount));
            sb.Append(" | borderTop ").Append(Box(_borderTop));
            sb.Append(" | borderBottom ").Append(Box(_borderBottom));
            sb.Append(" | borderLeft ").Append(Box(_borderLeft));
            sb.Append(" | borderRight ").Append(Box(_borderRight));
            sb.Append(" | bar ").Append(Box(_back));
            sb.Append(" | fill ").Append(Box(_fill));
            sb.Append(" | fillCol ").Append(ColorText(_fill.color));
            sb.Append(" | barCol ").Append(ColorText(_back.color));
            sb.Append(" | plateCol ").Append(ColorText(_plateTop.color));
            sb.Append(" | scale ").Append(_scale.ToString("F2"));
            // 1.5.47: the border is four line quads; their colour IS the border, no sprite centre.
            sb.Append(" | borderCol ").Append(ColorText(_borderTop.color));
            sb.Append(" | canvas ").Append(_cardCanvas != null
                ? _cardCanvas.sortingOrder.ToString()
                : "none");

            Material m = _level != null ? _level.fontSharedMaterial : null;
            if (m != null)
            {
                sb.Append(" | labelMat ").Append(m.name)
                  .Append(" glow=").Append(Float(m, "_GlowPower"))
                  .Append(" glowOuter=").Append(Float(m, "_GlowOuter"))
                  .Append(" underlay=").Append(Float(m, "_UnderlayPower"));
            }

            Transform t = _root;
            for (int depth = 0; t != null && depth < 8; ++depth, t = t.parent)
            {
                CanvasGroup group = t.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    sb.Append(" | cg:").Append(t.name).Append('=').Append(group.alpha.ToString("F2"));
                }
            }

            return sb.ToString();
        }

        static string Box(Image image)
        {
            if (image == null)
            {
                return "null";
            }

            Vector2 size = image.rectTransform.sizeDelta;
            return image.name
                + " size(" + size.x.ToString("F0") + "x" + size.y.ToString("F0") + ")"
                + " sprite=" + (image.sprite != null ? image.sprite.name : "-")
                + " on=" + image.enabled;
        }

        static string ColorText(Color c)
        {
            return "(" + c.r.ToString("F2") + "," + c.g.ToString("F2") + "," + c.b.ToString("F2") +
                "," + c.a.ToString("F2") + ")";
        }

        /// <summary>
        /// 1.5.43: the gold that "takes the entire space of the XP bar" is NOT one of our boxes
        /// (diag proved ours are all dark) - it is a foreign graphic whose rect covers the finished
        /// card (Skyheim Compat stretches a clone across every SeneaL slot root; SeneaL and
        /// HotbarSwitch stretch panels in the hotbar). The card's plates are semi-transparent
        /// black, so a warm graphic under them shows through as "a rectangle of gold with reduced
        /// opacity". GuardCover finds the one covering the bar and repaints it the card's black at
        /// its own alpha, so the card reads black behind the text. The log line names the object.
        /// </summary>
        static Graphic _cover;
        static Graphic _coverTop;
        static readonly System.Collections.Generic.HashSet<string> CoverLogged =
            new System.Collections.Generic.HashSet<string>();
        static float _nextScan;
        const float ScanInterval = 1.5f;

        static void GuardCover()
        {
            if (_back == null)
            {
                return;
            }

            // Owners re-assert their own graphics (the compat overlays SetActive(true) every tick),
            // so keep re-applying the fix on the data cadence: repaint the one behind, hide the one
            // drawn on top. Both are single field writes.
            if (_cover != null && !Blackened(_cover.color))
            {
                PaintCover(_cover);
            }

            if (_coverTop != null && _coverTop.gameObject.activeSelf)
            {
                _coverTop.gameObject.SetActive(false);
            }

            if (Time.unscaledTime < _nextScan)
            {
                return;
            }

            _nextScan = Time.unscaledTime + ScanInterval;

            Canvas canvas = _root.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
            int ourOrder = rootCanvas != null ? rootCanvas.sortingOrder : int.MinValue;

            // The CARD, not just the 100px bar: the reported gold filled the entire card space.
            Rect card = ScreenRect(_plateTop.rectTransform, cam);
            float area = card.width * card.height;
            if (area < 64f)
            {
                return;
            }

            RectTransform shift = HudLayout.HotbarShift;
            if (shift == null)
            {
                return;
            }

            Graphic[] all = UnityEngine.Object.FindObjectsOfType<Graphic>();
            for (int i = 0; i < all.Length; ++i)
            {
                Graphic graphic = all[i];
                if (graphic == null || graphic == _cover || graphic == _coverTop
                    || !graphic.isActiveAndEnabled)
                {
                    continue;
                }

                Transform t = graphic.transform;

                // 1.5.47: scope widened. Searched before: ONLY the shift subtree (slot visuals and
                // clones). That found nothing in the 1.5.46 session while the user still saw a gold
                // card - the covering graphic lives in the HUD canvas OUTSIDE the shift subtree
                // (SeneaL UI's own hotbar frame/glow panels). So: the shift subtree AND every
                // graphic in the same root canvas as our card. Never our own card, never the shift
                // root, never a foreign canvas (chat, inventory, tooltips keep their own space).
                Canvas graphicCanvas = graphic.canvas != null ? graphic.canvas.rootCanvas : null;
                bool inScope = Under(t, shift)
                    || (rootCanvas != null && graphicCanvas == rootCanvas);
                if (!inScope || Under(t, _root) || t == shift)
                {
                    continue;
                }

                Color color = graphic.color;
                if (color.a <= 0.02f)
                {
                    continue;
                }

                Rect rect = ScreenRect(graphic.rectTransform, cam);
                if (CoverFraction(rect, card) < 0.75f)
                {
                    continue;
                }

                // 1.5.44: any colour that fills the card counts, not only warm tints — a white-tinted
                // image stretched over a gold sprite paints gold while its Image.color stays white,
                // which is what the 1.5.43 warm-only guard missed.
                string path = Path(graphic.transform);
                bool onTop = graphicCanvas == null || graphicCanvas.sortingOrder >= ourOrder;

                if (onTop)
                {
                    _coverTop = graphic;
                    graphic.gameObject.SetActive(false);
                    LogCover(path + " (draws over the card) col " + ColorText(color));
                }
                else
                {
                    _cover = graphic;
                    PaintCover(graphic);
                    LogCover(path + " (behind the card) was col " + ColorText(color));
                }

                return;
            }
        }

        static void LogCover(string message)
        {
            if (CoverLogged.Add(message))
            {
                Debug.Log("[SeneaL HUD Layout] XP cover: fixed " + message);
            }
        }

        static void PaintCover(Graphic graphic)
        {
            // Card black at the graphic's own opacity, capped so the card never turns opaque: the
            // gold covered the card at reduced opacity, so its replacement must stay see-through.
            float alpha = Mathf.Min(graphic.color.a, 0.45f);
            graphic.color = new Color(PlateFill.r, PlateFill.g, PlateFill.b, alpha);
        }

        static bool Blackened(Color color)
        {
            return color.r <= 0.2f && color.g <= 0.2f && color.b <= 0.2f;
        }

        /// <summary>Yellow/gold family: red and green bright, blue clearly lower than green.</summary>
        static bool Warm(Color color)
        {
            return color.r >= 0.45f && color.g >= 0.30f && color.b <= 0.85f * color.g;
        }

        static bool Under(Transform t, Transform ancestor)
        {
            for (int depth = 0; t != null && depth < 16; ++depth, t = t.parent)
            {
                if (t == ancestor)
                {
                    return true;
                }
            }

            return false;
        }

        static float CoverFraction(Rect rect, Rect bar)
        {
            float area = bar.width * bar.height;
            if (area <= 0f || rect.width < 1f || rect.height < 1f)
            {
                return 0f;
            }

            Rect hit = Rect.MinMaxRect(
                Mathf.Max(rect.xMin, bar.xMin),
                Mathf.Max(rect.yMin, bar.yMin),
                Mathf.Min(rect.xMax, bar.xMax),
                Mathf.Min(rect.yMax, bar.yMax));

            if (hit.width <= 0f || hit.height <= 0f)
            {
                return 0f;
            }

            return hit.width * hit.height / area;
        }

        /// <summary>
        /// Names the cover: the 1.5.41 user report says the yellow sits ON TOP of the whole bar,
        /// so the culprit is some OTHER mod's graphic drawn over the card, not one of ours (the
        /// diag proved ours are all dark). This computes the bar's screen rect, then walks every
        /// active Graphic in the scene and lists the ones whose screen rect overlaps the bar -
        /// name, live colour, sprite. Runs ONCE, 2s after the card builds. With the card now on
        /// its own sorting canvas this list should come back clean; if the yellow survives, the
        /// line names the exact object.
        /// </summary>
        static string Probe()
        {
            if (_back == null)
            {
                return "no bar";
            }

            Canvas canvas = _root.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            Rect bar = ScreenRect(_back.rectTransform, cam);
            if (bar.width < 2f || bar.height < 2f)
            {
                return "bar off screen " + RectText(bar);
            }

            var sb = new System.Text.StringBuilder(512);
            sb.Append("bar").Append(RectText(bar));
            int shown = 0;

            Graphic[] all = UnityEngine.Object.FindObjectsOfType<Graphic>();
            for (int i = 0; i < all.Length; ++i)
            {
                Graphic graphic = all[i];
                if (graphic == null || !graphic.isActiveAndEnabled || graphic.canvasRenderer == null)
                {
                    continue;
                }

                Transform t = graphic.transform;
                bool mine = false;
                for (int depth = 0; t != null && depth < 12; ++depth, t = t.parent)
                {
                    if (t == _root)
                    {
                        mine = true;
                        break;
                    }
                }

                if (mine)
                {
                    continue;
                }

                Rect rect = ScreenRect(graphic.rectTransform, cam);
                if (rect.width < 1f || rect.height < 1f || !rect.Overlaps(bar))
                {
                    continue;
                }

                if (shown++ >= 14)
                {
                    sb.Append(" | ...");
                    break;
                }

                string sprite = graphic is Image image && image.sprite != null ? image.sprite.name : "-";
                sb.Append(" | ").Append(Path(graphic.transform))
                  .Append(" col").Append(ColorText(graphic.color))
                  .Append(" spr=").Append(sprite)
                  .Append(" r").Append(RectText(rect));
            }

            return shown == 0 ? sb.Append(" | CLEAN").ToString() : sb.ToString();
        }

        static Rect ScreenRect(RectTransform rt, Camera cam)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < 4; ++i)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        static string RectText(Rect rect)
        {
            return "(" + rect.xMin.ToString("F0") + "," + rect.yMin.ToString("F0") +
                " " + rect.width.ToString("F0") + "x" + rect.height.ToString("F0") + ")";
        }

        static string Path(Transform t)
        {
            var sb = new System.Text.StringBuilder(64);
            var stack = new System.Collections.Generic.List<string>(8);
            for (int depth = 0; t != null && depth < 8; ++depth, t = t.parent)
            {
                stack.Add(t.name);
            }

            for (int i = stack.Count - 1; i >= 0; --i)
            {
                sb.Append(stack[i]);
                if (i > 0)
                {
                    sb.Append('/');
                }
            }

            return sb.ToString();
        }

        static string Float(Material m, string name)
        {
            return m.HasFloat(name) ? m.GetFloat(name).ToString("F2") : "-";
        }

        /// <summary>
        /// The labels copy SeneaL's food-timer font material (the shipped look), and that style
        /// glows: every gold letter blooms a yellow haze over the card - the "yellow background"
        /// the user sees over provably-dark plates. The 1.5.39 strip targeted desktop TMP
        /// property names and did nothing: the diag showed the material's shader is
        /// TextMeshPro/Mobile/Distance Field, which has NO _GlowPower/_GlowOuter/_UnderlayPower
        /// (diag "glow=-"). So: walk the material's OWN shader and zero every glow/underlay/
        /// outline/shine/reflect/dilate property that shader actually has (the copy, never the
        /// shared asset).
        /// </summary>
        static void KillLabelGlow()
        {
            Strip(_level);
            Strip(_percent);
            Strip(_amount);
        }

        static void Strip(TextMeshProUGUI label)
        {
            Material m = label != null ? label.fontSharedMaterial : null;
            if (m == null || m.shader == null)
            {
                return;
            }

            Shader shader = m.shader;
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                string property = shader.GetPropertyName(i);
                if (!IsExtraEffect(property))
                {
                    continue;
                }

                ShaderPropertyType type = shader.GetPropertyType(i);
                if (type == ShaderPropertyType.Float || type == ShaderPropertyType.Range)
                {
                    if (m.HasFloat(property) && !Mathf.Approximately(m.GetFloat(property), 0f))
                    {
                        m.SetFloat(property, 0f);
                    }
                }
                else if (type == ShaderPropertyType.Color && m.HasColor(property))
                {
                    Color color = m.GetColor(property);
                    if (!Mathf.Approximately(color.a, 0f))
                    {
                        color.a = 0f;
                        m.SetColor(property, color);
                    }
                }
            }
        }

        static bool IsExtraEffect(string property)
        {
            string n = property.ToLowerInvariant();
            return n.Contains("glow")
                || n.Contains("underlay")
                || n.Contains("outline")
                || n.Contains("shiny")
                || n.Contains("reflect")
                || n.Contains("dilate")
                || n.Contains("bevel")
                || n.Contains("bump");
        }

        /// <summary>
        /// Children of a plate use centre anchors + centre pivot, so localPosition is the offset
        /// from the plate's centre (x right, y up).
        /// </summary>
        static void CenterAll()
        {
            Center(_plateTop.rectTransform);
            Center(_plateAmount.rectTransform);
            Center(_borderTop.rectTransform);
            Center(_borderBottom.rectTransform);
            Center(_borderLeft.rectTransform);
            Center(_borderRight.rectTransform);
            Center(_back.rectTransform);
            Center(_level.rectTransform);
            Center(_percent.rectTransform);
            Center(_amount.rectTransform);
        }

        static void Center(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static Image MakePlate(string name, Transform parent)
        {
            // Solid dark fill from our OWN 1x1 sprite (the colour carries the 0.72 alpha): a
            // dedicated texture, so no shared sprite can ever tint the card (the 1.5.36
            // "whole panel is pale yellow" report - suspected a shared/modified sprite; the
            // plate colour is set every card build, not just once).
            Image image = MakeImage(name, parent);
            image.sprite = PlateSprite();
            image.color = PlateFill;
            return image;
        }

        static Sprite _plate;

        static Sprite PlateSprite()
        {
            if (_plate != null)
            {
                return _plate;
            }

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.name = "SeneaLHudLayout_XpPlate";
            tex.SetPixels32(new Color32[] { new Color32(255, 255, 255, 255) });
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = tex.name;
            _plate = sprite;
            return _plate;
        }

        /// <summary>
        /// One BORDER line: a solid quad (our own 1x1 white sprite, tinted gold), never a 9-sliced
        /// sprite. A quad paints exactly its own rectangle, so the border is provably a line.
        /// (1.5.47: the old 9-sliced XpGoldFrame and its builder were DELETED - six rounds of
        /// "gold rectangle filling the card" reports ended with the four-quadrilateral border.)
        /// </summary>
        static Image MakeLine(string name, Transform parent)
        {
            Image image = MakeImage(name, parent);
            image.color = BorderGold;
            return image;
        }

        static Image MakeImage(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            Image image = go.AddComponent<Image>();
            image.sprite = CharacterExtras.PixelSprite();
            image.raycastTarget = false;
            return image;
        }

        static Sprite _barFrame;

        /// <summary>
        /// The XP bar's own track: a BLACK interior and a gold 1px rim, both baked OPAQUE; the
        /// Image's colour alpha (from [Hotbar] XpBarTrackOpacity, default 0.25) scales the whole
        /// thing live. The user's spec verbatim: gold is only the border, the fill is black at
        /// about 25% so the scenery shows through. Zero yellow inside the bar: every gold FILL
        /// shipped before 1.5.41 was yellow by definition, which is what the whole hunt was.
        /// </summary>
        static Sprite BarFrame()
        {
            if (_barFrame != null)
            {
                return _barFrame;
            }

            const int size = 12;
            const int edge = 1;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "SeneaLHudLayout_XpBarFrame";
            Color32 solid = new Color(0f, 0f, 0f, 1f);
            // 1.5.46: the bar's rim is BLACK, not gold. The user's rule is "it's only supposed to
            // be black" for the bar, and this 1px gold rim was the last yellow pixel left on the
            // XP bar; the card's own frame carries the gold border now.
            Color32 rim = new Color(0.08f, 0.08f, 0.09f, 1f);

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onEdge = x < edge || y < edge || x >= size - edge || y >= size - edge;
                    pixels[y * size + x] = onEdge ? rim : solid;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            // pixelsPerUnit 1: the 1px rim is 1 HUD unit (one screen pixel). At 100 the rim is
            // 0.01 units wide and the sampler blurs it - the 1.5.35 "glowing border" report.
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f,
                0, SpriteMeshType.FullRect, new Vector4(edge, edge, edge, edge));
            sprite.name = tex.name;
            _barFrame = sprite;
            return _barFrame;
        }

        static TextMeshProUGUI MakeLabel(string name, Transform parent, TextAlignmentOptions align, float size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = align;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = TextColor;
            // The food-timer label's exact font AND material, the same source the readable
            // food-bar player level copies. No outline: TMP swaps the text's material for a
            // clone of the font asset's base material when outline properties are set, and
            // Valheim's baked HUD atlases do not survive that swap — every glyph drew as a
            // random character (the 1.5.30 "scrambled text" report). The dark plates provide
            // the contrast instead.
            CharacterExtras.CopyFoodBarFont(label);
            _fontApplied = HasFoodFont(label);
            return label;
        }

        static bool HasFoodFont(TextMeshProUGUI label)
        {
            return label != null
                && label.font != null
                && !ReferenceEquals(label.font, TMP_Settings.defaultFontAsset);
        }

        static void EnsureLookup()
        {
            if (_lookup)
            {
                return;
            }

            _lookup = true;
            Type rootType = HarmonyLib.AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
            if (rootType == null)
            {
                return;
            }

            _hotbarField = HarmonyLib.AccessTools.Field(rootType, "_hotbar");
            if (_hotbarField == null)
            {
                return;
            }

            Type hotbarType = _hotbarField.FieldType;
            _frameActions = HarmonyLib.AccessTools.Field(hotbarType, "FrameActions");
            _frameQuiver = HarmonyLib.AccessTools.Field(hotbarType, "FrameQuiver");
        }

        static void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }
    }
}
