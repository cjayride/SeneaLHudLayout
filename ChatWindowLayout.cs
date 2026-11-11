using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class ChatWindowLayout
    {
        const float MinEdge = 80f;
        const float InputH = 34f;
        const float InputPad = 8f;
        const float InputGap = 10f;

        static readonly Vector3[] Corners = new Vector3[4];

        static FieldInfo _panelField;
        static FieldInfo _tgtPos;
        static FieldInfo _tgtSize;
        static FieldInfo _curPos;
        static FieldInfo _curSize;
        static FieldInfo _snapNext;
        static MethodInfo _detach;
        static MethodInfo _attach;
        static FieldInfo _scrollHeight;
        static FieldInfo _chatBuffer;
        static FieldInfo _maxVisible;
        static FieldInfo _updateChat;
        static FieldInfo _budget;
        static FieldInfo _shift;
        static FieldInfo _have;
        static FieldInfo _fitText;
        static FieldInfo _fitWin;
        static FieldInfo _fitScreen;
        static bool _bound;

        static Vector2 _basePos;
        static Vector2 _baseSize;
        static Vector3 _panelBL;
        static Vector3 _logBL;
        static float _logW;
        static float _logH;
        static bool _haveBase;
        static bool _haveInput;
        static bool _touched;
        static Vector2 _lastOffset;
        static Vector2 _lastDelta;
        static RectTransform _clip;
        static Transform _logParent;
        static Vector2 _logAnchorMin;
        static Vector2 _logAnchorMax;
        static Vector2 _logPivot;
        static Vector2 _logPos;
        static Vector2 _logSize;
        static bool _clipped;

        // 1.5.52 FPS: the text object the clip style was applied to (see OnBeforeRender).
        static TMP_Text _clipStyled;
        static bool _renderHooked;
        static bool _rendering;
        static int _clipLines;
        static int _savedMax = -1;
        static TextOverflowModes _logOverflow;
        static TextAlignmentOptions _logAlign;
        static bool _savedStyle;
        static bool _idleRestored;
        static Vector2 _seenPos;
        static Vector2 _seenSize;
        static int _stable;

        public static void Apply(Harmony harmony)
        {
            Type skin = AccessTools.TypeByName("SeneaLUI.Hud.ChatSkin");
            if (skin == null)
            {
                return;
            }

            _panelField = AccessTools.Field(skin, "_panel");
            _tgtPos = AccessTools.Field(skin, "_tgtPos");
            _tgtSize = AccessTools.Field(skin, "_tgtSize");
            _curPos = AccessTools.Field(skin, "_curPos");
            _curSize = AccessTools.Field(skin, "_curSize");
            _snapNext = AccessTools.Field(skin, "_snapNext");
            _detach = AccessTools.Method(skin, "Detach");
            _attach = AccessTools.Method(skin, "Attach", new[] { typeof(Chat) });
            _scrollHeight = AccessTools.Field(typeof(Terminal), "m_scrollHeight");
            _chatBuffer = AccessTools.Field(typeof(Terminal), "m_chatBuffer");
            _maxVisible = AccessTools.Field(typeof(Terminal), "m_maxVisibleBufferLength");
            _updateChat = AccessTools.Field(skin, "UpdateChat");
            _budget = AccessTools.Field(skin, "_budget");
            _shift = AccessTools.Field(skin, "_shift");
            _have = AccessTools.Field(skin, "_have");
            _fitText = AccessTools.Field(skin, "_fitText");
            _fitWin = AccessTools.Field(skin, "_fitWin");
            _fitScreen = AccessTools.Field(skin, "_fitScreen");
            _bound = _panelField != null && _tgtPos != null && _tgtSize != null && _curPos != null && _curSize != null && _snapNext != null;
            if (_bound && !_renderHooked)
            {
                Canvas.willRenderCanvases += OnBeforeRender;
                _renderHooked = true;
            }

            MethodInfo fit = AccessTools.Method(skin, "Fit", new[] { typeof(Chat), typeof(bool) });
            if (fit != null)
            {
                harmony.Patch(fit, postfix: new HarmonyMethod(typeof(ChatWindowLayout), nameof(FitPostfix)));
            }

            MethodInfo chatUpdate = AccessTools.Method(typeof(Chat), "Update");
            if (chatUpdate != null)
            {
                harmony.Patch(chatUpdate, postfix: new HarmonyMethod(typeof(ChatWindowLayout), nameof(ChatUpdatePostfix)));
            }

            MethodInfo addString = AccessTools.Method(typeof(Terminal), "AddString", new[] { typeof(string) });
            if (addString != null)
            {
                harmony.Patch(addString, prefix: new HarmonyMethod(typeof(ChatWindowLayout), nameof(AddStringPrefix)));
            }

            Bind(SeneaLHudLayoutPlugin.ChatWindowRecommendedLayout);
            Bind(SeneaLHudLayoutPlugin.ChatWindowPosX);
            Bind(SeneaLHudLayoutPlugin.ChatWindowPosY);
            Bind(SeneaLHudLayoutPlugin.ChatWindowWidth);
            Bind(SeneaLHudLayoutPlugin.ChatWindowHeight);
        }

        // 1.5.53: chat behind ALL the windows, by canvas order. The 1.5.52 probe settled the
        // shape at runtime (three full canvas ladders in LogOutput.log):
        //  - invVisible=0 on every inventory edge: SeneaL UI replaces the vanilla InventoryGui,
        //    so the crafting window the user crafts in is VNEI's ("VNEI(Clone)(ov=630)"), and the
        //    1.5.52 sibling push keyed on InventoryGui.IsVisible() never fired.
        //  - NO canvas anywhere was flagged CHAT while chat was hidden: the canvas that governs
        //    the skinned panel (Terminal.m_chatWindow = "Chat_box", panel = Chat_box/SeneaLUI_chat)
        //    is only in the active list when the chat is SHOWN - and when shown it draws over the
        //    600/630 windows, which is the user's complaint.
        // The fix is order, not siblings: every Canvas inside the chat window's subtree is pinned
        // to sortingOrder 300 - the tier the vanilla "Chat" canvas already uses (the probe shows
        // "Chat(ov=300)"), which sits below HUD(400), inventory(600), VNEI(630), everything. This
        // reproduces vanilla's own chat layering for the skinned window. Writes only on drift.
        const int ChatTier = 300;

        static int _chatSubtreeId;
        static Canvas[] _chatCanvases;

        static void PinChatCanvasesBehindWindows()
        {
            Chat chat = Chat.instance;
            RectTransform win = chat != null ? chat.m_chatWindow : null;
            if (win == null)
            {
                return;
            }

            int id = win.GetInstanceID();
            if (id != _chatSubtreeId || _chatCanvases == null)
            {
                _chatCanvases = win.GetComponentsInChildren<Canvas>(true);
                _chatSubtreeId = id;
            }
            else
            {
                // The chat window rebuilds its children when the skin changes; a destroyed
                // canvas reads null, which re-collects the list once.
                for (int i = 0; i < _chatCanvases.Length; i++)
                {
                    if (!_chatCanvases[i])
                    {
                        _chatCanvases = win.GetComponentsInChildren<Canvas>(true);
                        break;
                    }
                }
            }

            Canvas[] canvases = _chatCanvases;

            // The panel's draw tier is decided by the NEAREST Canvas ancestor: if the chat window
            // owns no canvas, that is the IngameGui tree canvas, and a skinned panel with its own
            // high-sorted canvas (SeneaL UI sorts its windows 28000-32600) sails over the 600/630
            // windows. Guarantee the chat window carries a canvas first, then pin every canvas the
            // chat subtree owns (all of them, root included - they are chat's by definition) to the
            // vanilla chat tier. The one canvas never touched is the IngameGui root OUTSIDE the
            // subtree: it governs the whole HUD, not the chat.
            Canvas onWindow = win.GetComponent<Canvas>();
            if (onWindow == null)
            {
                onWindow = win.gameObject.AddComponent<Canvas>();
                canvases = win.GetComponentsInChildren<Canvas>(true);
                _chatCanvases = canvases;
            }

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (!canvas)
                {
                    continue;
                }

                bool isRoot = canvas.rootCanvas == canvas;
                if (isRoot)
                {
                    // A ROOT canvas in the chat subtree (SeneaL UI roots its windows, the vanilla
                    // "Chat" roots at 300): the order alone decides, overrideSorting is inert.
                    if (canvas.overrideSorting)
                    {
                        canvas.overrideSorting = false;
                    }

                    if (canvas.sortingOrder != ChatTier)
                    {
                        canvas.sortingOrder = ChatTier;
                    }
                }
                else
                {
                    if (!canvas.overrideSorting || canvas.sortingOrder != ChatTier)
                    {
                        canvas.overrideSorting = true;
                        canvas.sortingOrder = ChatTier;
                    }
                }
            }
        }

        // 1.5.52: chat behind the inventory/crafting windows. Vanilla writes ZERO sortingOrder
        // (verified across the decompiled Assembly-CSharp) and SeneaL UI's high canvases
        // (28000+, verified in the decompiled SeneaL-UI) do NOT carry the inventory windows - so
        public static void Tick()
        {
            PinChatCanvasesBehindWindows();

            if (!_bound || Hidden())
            {
                return;
            }

            if (!UserOn())
            {
                if (_touched || !_idleRestored)
                {
                    Restore();
                    _idleRestored = true;
                }

                return;
            }

            _idleRestored = false;

            if (!_haveBase)
            {
                Capture();
            }

            if (_haveBase)
            {
                Place();
            }
        }

        public static void FitPostfix()
        {
            if (!UserOn() || Hidden() || !_haveBase)
            {
                return;
            }

            Place();
        }

        public static void AddStringPrefix(Terminal __instance)
        {
            if (!(__instance is Chat chat) || Hidden() || !AutoScrollOn())
            {
                return;
            }

            if (InputOpen(chat))
            {
                return;
            }

            JumpToLatest(chat);
        }

        public static void ChatUpdatePostfix()
        {
            if (!_bound || Hidden())
            {
                return;
            }

            Chat chat = Chat.instance;
            if (chat == null || chat.m_input == null || !(chat.m_input.transform is RectTransform input))
            {
                return;
            }

            if (_clip != null && input.IsChildOf(_clip))
            {
                Transform home = chat.m_chatWindow != null ? chat.m_chatWindow : input.parent;
                input.SetParent(home, true);
            }

            if (!UserOn() || !_haveBase)
            {
                HandleKeys(chat);
                return;
            }

            RectTransform panel = Panel();
            if (panel == null)
            {
                return;
            }

            KeepInput(panel, input);
            HandleKeys(chat);
        }

        static void Bind(BepInEx.Configuration.ConfigEntry<float> entry)
        {
            if (entry != null)
            {
                entry.SettingChanged += (_, __) => Tick();
            }
        }

        static void Bind(BepInEx.Configuration.ConfigEntry<int> entry)
        {
            if (entry != null)
            {
                entry.SettingChanged += (_, __) => Tick();
            }
        }

        static void Bind(BepInEx.Configuration.ConfigEntry<bool> entry)
        {
            if (entry != null)
            {
                entry.SettingChanged += (_, __) => Tick();
            }
        }

        static bool RecommendedOn()
        {
            return SeneaLHudLayoutPlugin.ChatWindowRecommendedLayout != null
                && SeneaLHudLayoutPlugin.ChatWindowRecommendedLayout.Value;
        }

        static float LayoutPosX()
        {
            return RecommendedOn()
                ? SeneaLHudLayoutPlugin.ChatWindowRecommendedPosX
                : Cfg(SeneaLHudLayoutPlugin.ChatWindowPosX);
        }

        static float LayoutPosY()
        {
            return RecommendedOn()
                ? SeneaLHudLayoutPlugin.ChatWindowRecommendedPosY
                : Cfg(SeneaLHudLayoutPlugin.ChatWindowPosY);
        }

        static float LayoutWidth()
        {
            return RecommendedOn()
                ? SeneaLHudLayoutPlugin.ChatWindowRecommendedWidth
                : Cfg(SeneaLHudLayoutPlugin.ChatWindowWidth);
        }

        static float LayoutHeight()
        {
            return RecommendedOn()
                ? SeneaLHudLayoutPlugin.ChatWindowRecommendedHeight
                : Cfg(SeneaLHudLayoutPlugin.ChatWindowHeight);
        }

        static bool Hidden()
        {
            return SeneaLHudLayoutPlugin.HideChat != null && SeneaLHudLayoutPlugin.HideChat.Value;
        }

        static bool AutoScrollOn()
        {
            return SeneaLHudLayoutPlugin.ChatAutoScrollOnNewMessage == null
                || SeneaLHudLayoutPlugin.ChatAutoScrollOnNewMessage.Value;
        }

        static bool InputOpen(Chat chat)
        {
            return chat != null
                && chat.m_input != null
                && chat.m_input.gameObject.activeInHierarchy;
        }

        static void JumpToLatest(Chat chat)
        {
            if (_scrollHeight == null || chat == null)
            {
                return;
            }

            try
            {
                int current = (int)_scrollHeight.GetValue(chat);
                if (current == 0)
                {
                    return;
                }

                _scrollHeight.SetValue(chat, 0);
            }
            catch (Exception)
            {
            }
        }

        static bool UserOn()
        {
            if (RecommendedOn())
            {
                return true;
            }

            return !Mathf.Approximately(Cfg(SeneaLHudLayoutPlugin.ChatWindowPosX), 0f)
                || !Mathf.Approximately(Cfg(SeneaLHudLayoutPlugin.ChatWindowPosY), 0f)
                || !Mathf.Approximately(Cfg(SeneaLHudLayoutPlugin.ChatWindowWidth), 0f)
                || !Mathf.Approximately(Cfg(SeneaLHudLayoutPlugin.ChatWindowHeight), 0f);
        }

        static float Cfg(BepInEx.Configuration.ConfigEntry<float> entry)
        {
            return entry == null ? 0f : entry.Value;
        }

        static float Cfg(BepInEx.Configuration.ConfigEntry<int> entry)
        {
            return entry == null ? 0f : entry.Value;
        }

        static bool SkinHave()
        {
            try
            {
                return _have != null && _have.GetValue(null) is bool ready && ready;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool Capture()
        {
            if (_haveBase || _touched)
            {
                return _haveBase;
            }

            Chat chat = Chat.instance;
            RectTransform panel = Panel();
            if (chat == null || chat.m_output == null || panel == null || !SkinHave())
            {
                _stable = 0;
                return false;
            }

            Vector2 pos = panel.anchoredPosition;
            Vector2 size = panel.sizeDelta;
            if (size.x < MinEdge || size.y < MinEdge)
            {
                _stable = 0;
                return false;
            }

            if ((pos - _seenPos).sqrMagnitude > 1f || (size - _seenSize).sqrMagnitude > 1f)
            {
                _seenPos = pos;
                _seenSize = size;
                _stable = 0;
                return false;
            }

            _stable++;
            if (_stable < 3)
            {
                return false;
            }

            _basePos = pos;
            _baseSize = size;
            _panelBL = BottomLeft(panel);
            _logBL = BottomLeft(chat.m_output.rectTransform);
            _logW = chat.m_output.rectTransform.rect.width;
            _logH = chat.m_output.rectTransform.rect.height;
            _haveInput = chat.m_input != null && chat.m_input.transform is RectTransform;
            _haveBase = _logW > 1f && _logH > 1f;
            return _haveBase;
        }

        static void Place()
        {
            Chat chat = Chat.instance;
            RectTransform panel = Panel();
            if (chat == null || chat.m_output == null || panel == null)
            {
                return;
            }

            float dw = LayoutWidth();
            float dh = LayoutHeight();
            Vector2 offset = new Vector2(LayoutPosX(), LayoutPosY());
            Vector2 wantPos = _basePos + offset;
            Vector2 wantSize = new Vector2(
                Mathf.Max(MinEdge, _baseSize.x + dw),
                Mathf.Max(MinEdge, _baseSize.y + dh));
            WriteTgt(wantPos, wantSize);
            panel.anchoredPosition = wantPos;
            panel.sizeDelta = wantSize;
            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }

            float logW = Mathf.Max(40f, _logW + dw);
            float logH = Mathf.Max(20f, _logH + dh);
            if (dh < -0.5f)
            {
                float bottom = InputPad + InputH + InputGap;
                float clipH = Mathf.Max(20f, wantSize.y - bottom - InputPad);
                _clipLines = LinesFor(chat.m_output, clipH);
                PinLatest(chat, _clipLines);
                ClipLog(panel, chat.m_output);
            }
            else
            {
                ReleaseClip(chat.m_output);
                Vector3 delta = BottomLeft(panel) - _panelBL;
                FitBox(chat.m_output.rectTransform, _logBL + delta, logW, logH);
                if (dw < -0.5f)
                {
                    chat.m_output.textWrappingMode = TextWrappingModes.Normal;
                }
            }

            if (_haveInput && chat.m_input != null && chat.m_input.transform is RectTransform input)
            {
                KeepInput(panel, input);
            }

            _lastOffset = offset;
            _lastDelta = new Vector2(dw, dh);
            _touched = true;
        }

        static int LinesFor(TMP_Text text, float height)
        {
            float line = 18f;
            if (text != null)
            {
                line = text.fontSize + Mathf.Max(0f, text.lineSpacing);
            }

            if (line < 8f)
            {
                line = 18f;
            }

            return Mathf.Clamp(Mathf.FloorToInt(height / line), 1, 40);
        }

        static void PinLatest(Chat chat, int lines)
        {
            bool budgetChanged = SetBudget(lines);
            if (_maxVisible != null && chat != null)
            {
                try
                {
                    if (_savedMax < 0)
                    {
                        _savedMax = (int)_maxVisible.GetValue(chat);
                    }

                    _maxVisible.SetValue(chat, Mathf.Clamp(lines, 1, 40));
                }
                catch (Exception)
                {
                }
            }

            if (budgetChanged)
            {
                RefreshLog(chat, false);
            }
        }

        static void HandleKeys(Chat chat)
        {
            if (chat == null || chat.m_input == null || !chat.m_input.gameObject.activeInHierarchy)
            {
                return;
            }

            int step = _clipLines > 0 ? _clipLines : 5;
            int delta = 0;
            if (ZInput.GetKeyDown(KeyCode.PageUp))
            {
                delta += step;
            }

            if (ZInput.GetKeyDown(KeyCode.PageDown))
            {
                delta -= step;
            }

            if (delta == 0)
            {
                return;
            }

            ShiftScroll(chat, delta);
        }

        static void ShiftScroll(Chat chat, int delta)
        {
            if (_scrollHeight == null || chat == null)
            {
                return;
            }

            try
            {
                int current = (int)_scrollHeight.GetValue(chat);
                int count = 0;
                if (_chatBuffer != null && _chatBuffer.GetValue(chat) is System.Collections.IList buf)
                {
                    count = buf.Count;
                }

                int vis = _clipLines > 0 ? _clipLines : 5;
                int max = Mathf.Max(0, count - vis);
                int next = Mathf.Clamp(current + delta, 0, max);
                if (next == current)
                {
                    return;
                }

                _scrollHeight.SetValue(chat, next);
                RefreshLog(chat, false);
            }
            catch (Exception)
            {
            }
        }

        static bool SetBudget(int lines)
        {
            if (_budget == null)
            {
                return false;
            }

            try
            {
                object live = _budget.GetValue(null);
                if (live is int current && current == lines)
                {
                    return false;
                }

                _budget.SetValue(null, lines);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void RefreshLog(Chat chat, bool toLatest)
        {
            try
            {
                if (toLatest && _scrollHeight != null)
                {
                    _scrollHeight.SetValue(chat, 0);
                }

                if (_updateChat != null && _updateChat.GetValue(null) is Action<Terminal> update)
                {
                    update(chat);
                }
            }
            catch (Exception)
            {
            }
        }

        static void ClipLog(RectTransform panel, TMP_Text output)
        {
            RectTransform rt = output.rectTransform;
            if (!_clipped)
            {
                _logParent = rt.parent;
                _logAnchorMin = rt.anchorMin;
                _logAnchorMax = rt.anchorMax;
                _logPivot = rt.pivot;
                _logPos = rt.anchoredPosition;
                _logSize = rt.sizeDelta;
                _clipped = true;
            }

            if (_clip == null)
            {
                var go = new GameObject("SeneaLHudLayout_ChatClip", typeof(RectTransform));
                go.layer = panel.gameObject.layer;
                _clip = go.GetComponent<RectTransform>();
                go.AddComponent<RectMask2D>();
            }

            Transform home = panel.parent;
            _clip.SetParent(home, false);
            _clip.anchorMin = panel.anchorMin;
            _clip.anchorMax = panel.anchorMax;
            _clip.pivot = panel.pivot;
            float bottom = InputPad + InputH + InputGap;
            _clip.anchoredPosition = panel.anchoredPosition + new Vector2(InputPad, bottom);
            _clip.sizeDelta = new Vector2(
                Mathf.Max(40f, panel.sizeDelta.x - InputPad * 2f),
                Mathf.Max(20f, panel.sizeDelta.y - bottom - InputPad));
            InFrontOf(_clip, panel);

            Chat chat = Chat.instance;
            if (chat != null && chat.m_input != null)
            {
                Transform inp = chat.m_input.transform;
                if (inp.IsChildOf(rt) || inp.IsChildOf(_clip) || inp.parent == panel)
                {
                    Transform win = chat.m_chatWindow != null ? chat.m_chatWindow : panel.parent;
                    inp.SetParent(win, true);
                }
            }

            if (!_savedStyle)
            {
                _logOverflow = output.overflowMode;
                _logAlign = output.alignment;
                _savedStyle = true;
            }

            rt.SetParent(_clip, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            output.textWrappingMode = TextWrappingModes.Normal;
            output.overflowMode = TextOverflowModes.Overflow;
            output.alignment = TextAlignmentOptions.BottomLeft;
            output.maskable = true;
            output.ForceMeshUpdate(true, true);
        }

        static void OnBeforeRender()
        {
            if (_rendering || !_touched || !UserOn() || Hidden())
            {
                return;
            }

            Chat chat = Chat.instance;
            RectTransform panel = Panel();
            if (chat == null || panel == null)
            {
                return;
            }

            _rendering = true;
            try
            {
                if (_clipped && chat.m_output != null)
                {
                    // 1.5.52 FPS: ForceMeshUpdate(true, true) is the most expensive TMP call in
                    // the game, and this hook runs on EVERY canvas render (per frame). TMP's
                    // ForceMeshUpdate is internally a no-op when the text/geometry is already
                    // clean; the ONLY per-frame work that matters is the three property writes
                    // below, and setting alignment/overflow/wrapping marks the text dirty when
                    // the value CHANGES - so a frame of ForceMeshUpdate after the first
                    // application is pure waste. Apply the style once; re-apply when the panel
                    // rebuilds (the clip helper re-marks it) or the text object changes.
                    if (chat.m_output != _clipStyled)
                    {
                        _clipStyled = chat.m_output;
                        chat.m_output.alignment = TextAlignmentOptions.BottomLeft;
                        chat.m_output.overflowMode = TextOverflowModes.Overflow;
                        chat.m_output.textWrappingMode = TextWrappingModes.Normal;
                    }

                    chat.m_output.ForceMeshUpdate(true, true);
                }

                if (_haveInput && chat.m_input != null && chat.m_input.transform is RectTransform input)
                {
                    KeepInput(panel, input);
                }
            }
            finally
            {
                _rendering = false;
            }
        }

        static void KeepInput(RectTransform panel, RectTransform input)
        {
            if (input == null || panel == null)
            {
                return;
            }

            Chat chat = Chat.instance;
            if (input.parent == panel || (_clip != null && input.IsChildOf(_clip)))
            {
                Transform home = chat != null && chat.m_chatWindow != null ? chat.m_chatWindow : panel.parent;
                input.SetParent(home, true);
            }

            if (!input.gameObject.activeInHierarchy)
            {
                return;
            }

            Vector3 bl = panel.TransformPoint(new Vector3(InputPad, InputPad, 0f));
            float width = Mathf.Max(40f, panel.rect.width - InputPad * 2f);

            FitBox(input, bl, width, InputH);
        }

        static void InFrontOf(RectTransform rt, RectTransform panel)
        {
            if (rt == null || panel == null || rt.parent != panel.parent)
            {
                return;
            }

            int i = panel.GetSiblingIndex();
            if (rt.GetSiblingIndex() <= i)
            {
                rt.SetSiblingIndex(i + 1);
            }
        }

        static void ReleaseClip(TMP_Text output)
        {
            if (!_clipped || output == null)
            {
                return;
            }

            RectTransform rt = output.rectTransform;
            if (_logParent != null)
            {
                rt.SetParent(_logParent, false);
            }

            rt.anchorMin = _logAnchorMin;
            rt.anchorMax = _logAnchorMax;
            rt.pivot = _logPivot;
            rt.anchoredPosition = _logPos;
            rt.sizeDelta = _logSize;
            if (_savedStyle)
            {
                output.overflowMode = _logOverflow;
                output.alignment = _logAlign;
                _savedStyle = false;
            }

            _clipStyled = null;
            _clipped = false;
            RestoreVisible(Chat.instance);
            if (_clip != null)
            {
                UnityEngine.Object.Destroy(_clip.gameObject);
                _clip = null;
            }
        }

        static void FitBox(RectTransform rt, Vector3 worldBL, float width, float height)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy)
            {
                return;
            }

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Vector3 shift = worldBL - BottomLeft(rt);
            if (shift.sqrMagnitude > 0.01f && !float.IsNaN(shift.x) && !float.IsInfinity(shift.x))
            {
                rt.position += shift;
            }
        }

        static void RestoreVisible(Chat chat)
        {
            if (_savedMax < 0 || _maxVisible == null || chat == null)
            {
                _savedMax = -1;
                return;
            }

            try
            {
                _maxVisible.SetValue(chat, _savedMax);
            }
            catch (Exception)
            {
            }

            _savedMax = -1;
        }

        static void Restore()
        {
            Vector2 origPos = _basePos;
            Vector2 origSize = _baseSize;
            bool haveOrig = origSize.x >= MinEdge && origSize.y >= MinEdge;

            Chat chat = Chat.instance;
            Transform win = chat != null ? chat.m_chatWindow : null;
            if (chat != null && chat.m_output != null && win != null && chat.m_output.rectTransform.parent != win)
            {
                chat.m_output.rectTransform.SetParent(win, true);
            }

            if (chat != null && chat.m_input != null && chat.m_input.transform is RectTransform input)
            {
                if (win != null && input.parent != win)
                {
                    input.SetParent(win, true);
                }
            }

            if (chat != null && chat.m_output != null)
            {
                ReleaseClip(chat.m_output);
            }

            DropClip(chat);
            ClearFitCache();

            if (chat != null && _detach != null && _attach != null)
            {
                try
                {
                    _detach.Invoke(null, null);
                    ClearFitCache();
                    _attach.Invoke(null, new object[] { chat });
                    DropClip(chat);
                    ClearFitCache();
                }
                catch (Exception)
                {
                }
            }

            if (haveOrig)
            {
                WriteTgt(origPos, origSize);
                RectTransform panel = Panel();
                if (panel != null)
                {
                    panel.anchoredPosition = origPos;
                    panel.sizeDelta = origSize;
                }
            }

            _haveBase = false;
            _haveInput = false;
            _touched = false;
            _lastOffset = Vector2.zero;
            _lastDelta = Vector2.zero;
            _idleRestored = true;
            _stable = 0;
        }

        static void DropClip(Chat chat)
        {
            Transform win = chat != null ? chat.m_chatWindow : null;
            if (chat != null && chat.m_output != null)
            {
                RectTransform log = chat.m_output.rectTransform;
                if (_clip != null && log.parent == _clip)
                {
                    log.SetParent(win != null ? win : log.parent.parent, true);
                }
            }

            if (_clip != null)
            {
                UnityEngine.Object.Destroy(_clip.gameObject);
                _clip = null;
            }

            _clipStyled = null;
            _clipped = false;
            if (win == null)
            {
                return;
            }

            for (int i = win.childCount - 1; i >= 0; i--)
            {
                Transform child = win.GetChild(i);
                if (child == null || child.name != "SeneaLHudLayout_ChatClip")
                {
                    continue;
                }

                while (child.childCount > 0)
                {
                    child.GetChild(0).SetParent(win, true);
                }

                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        static bool ReadTgt(out Vector2 pos, out Vector2 size)
        {
            pos = default(Vector2);
            size = default(Vector2);
            try
            {
                pos = (Vector2)_tgtPos.GetValue(null);
                size = (Vector2)_tgtSize.GetValue(null);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void ClearFitCache()
        {
            try
            {
                if (_fitText != null)
                {
                    _fitText.SetValue(null, null);
                }

                if (_fitWin != null)
                {
                    _fitWin.SetValue(null, new Vector2(-1f, -1f));
                }

                if (_fitScreen != null)
                {
                    _fitScreen.SetValue(null, -1f);
                }

                if (_shift != null)
                {
                    _shift.SetValue(null, Vector2.zero);
                }
            }
            catch (Exception)
            {
            }
        }

        static RectTransform Panel()
        {
            try
            {
                return _panelField.GetValue(null) as RectTransform;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>The skinned chat panel, for the features that live inside it.</summary>
        public static RectTransform GetPanel()
        {
            return _bound ? Panel() : null;
        }

        /// <summary>World-space bottom-left corner of a rect.</summary>
        public static Vector3 GetBottomLeft(RectTransform rt)
        {
            return BottomLeft(rt);
        }

        /// <summary>Size a rect and move its bottom-left corner to a world point.</summary>
        public static void FitBoxTo(RectTransform rt, Vector3 worldBL, float width, float height)
        {
            FitBox(rt, worldBL, width, height);
        }

        static Vector3 BottomLeft(RectTransform rt)
        {
            rt.GetWorldCorners(Corners);
            return Corners[0];
        }

        static void WriteTgt(Vector2 pos, Vector2 size)
        {
            try
            {
                _tgtPos.SetValue(null, pos);
                _tgtSize.SetValue(null, size);
                _curPos.SetValue(null, pos);
                _curSize.SetValue(null, size);
                _snapNext.SetValue(null, true);
            }
            catch (Exception)
            {
            }
        }
    }
}
