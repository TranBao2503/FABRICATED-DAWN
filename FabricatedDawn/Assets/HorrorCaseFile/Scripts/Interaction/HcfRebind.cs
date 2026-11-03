using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Describes one key slot in a binding row.</summary>
    public struct HcfBinding
    {
        public string actionId;

        public int slot;

        /// <summary>Assigned key, or KeyCode.None for an empty slot.</summary>
        public KeyCode key;

        public VisualElement row;
    }

    /// <summary>Wires key binding controls without depending on an input backend.</summary>
    public static class HcfRebind
    {
        const string BindPrefix = "bind-";
        const string ListeningClass = "is-listening";
        const string ResolvedClass = "is-resolved";
        const string EmptyClass = "s05-key--empty";
        const string KeysClass = "s05-keys";
        const string KeyClass = "s05-key";
        const string HintClass = "s05-listen__hint";
        const string EmptyKey = "—";

        const string GlyphClass = "hcf-glyph";
        const string GlyphChars = "←↑→↓■";

        // The primary hint comes from UXML when present. The alternate hint has no UXML source.
        const string PrimaryHint = "press any key…";
        const string AltHint = "press second key…";

        /// <summary>Raised after a key slot changes in the UI.</summary>
        /// <remarks>Entering Play Mode clears subscribers even when Reload Domain is disabled.</remarks>
        public static event System.Action<HcfBinding> bindingChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPlayerState()
        {
            bindingChanged = null;
        }

        static readonly ConditionalWeakTable<VisualElement, object> s_Wired =
            new ConditionalWeakTable<VisualElement, object>();

        // State is keyed by row so multiple settings screens can coexist.
        static readonly ConditionalWeakTable<VisualElement, RowState> s_State =
            new ConditionalWeakTable<VisualElement, RowState>();

        sealed class RowState
        {
            public KeyCode[] current;

            public KeyCode[] defaults;

            public bool[] empty;

            public string[] ghost;

            // Authored listening line, taken before the first arm overwrites it.
            public string hint;
        }

        /// <summary>Initializes binding rows and registers their input handlers once.</summary>
        public static void Wire(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            List<VisualElement> rows = HcfScreenInteractions.Collect(root, BindPrefix);
            if (rows.Count == 0)
            {
                return;
            }

            object marker;
            if (s_Wired.TryGetValue(root, out marker))
            {
                return;
            }

            s_Wired.Add(root, new object());

            VisualElement listening = null;
            int listeningSlot = 0;

            foreach (VisualElement row in rows)
            {
                s_State.Remove(row);
                s_State.Add(row, Snapshot(row));

                if (row.ClassListContains(ListeningClass))
                {
                    listening = row;
                    listeningSlot = 0;
                }
            }

            System.Action stop = () =>
            {
                if (listening == null)
                {
                    return;
                }

                listening.RemoveFromClassList(ListeningClass);
                listening = null;
                listeningSlot = 0;
            };

            System.Action<VisualElement, int> start = (bindRow, bindSlot) =>
            {
                stop();
                listening = bindRow;
                listeningSlot = bindSlot;
                bindRow.AddToClassList(ListeningClass);

                Label hint = Hint(bindRow);
                if (hint != null)
                {
                    hint.text = bindSlot == 0 ? PrimaryHintOf(bindRow) : AltHint;
                }
            };

            foreach (VisualElement row in rows)
            {
                VisualElement target = row;
                List<Label> slots = Slots(row);

                int pointerSlot = -1;
                target.RegisterCallback<PointerDownEvent>(evt => pointerSlot = -1, TrickleDown.TrickleDown);
                for (int i = 0; i < slots.Count; i++)
                {
                    int index = i;
                    slots[i].RegisterCallback<PointerDownEvent>(evt => pointerSlot = index);
                }

                System.Action arm = () =>
                {
                    int slot;
                    if (pointerSlot >= 0)
                    {
                        slot = pointerSlot;
                        pointerSlot = -1;
                    }
                    else if (target == listening)
                    {
                        slot = listeningSlot + 1;
                    }
                    else
                    {
                        slot = 0;
                    }

                    if (slot >= slots.Count || (target == listening && slot == listeningSlot))
                    {
                        stop();
                        return;
                    }

                    start(target, slot);
                };

                Button button = row as Button;
                if (button != null)
                {
                    button.clicked += arm;
                }
                else
                {
                    target.RegisterCallback<ClickEvent>(evt => arm());
                }
            }

            int lastFrame = -1;
            // TrickleDown handles Escape and Delete before anything downstream sees them.
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (Time.frameCount == lastFrame)
                {
                    return;
                }

                if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                {
                    VisualElement row = listening != null ? listening : FocusedRow(root, rows);
                    if (row == null)
                    {
                        return;
                    }

                    List<Label> slots = Slots(row);
                    RowState state = State(row);
                    int slot = row == listening ? listeningSlot : LastFilled(state);
                    if (slot < 0 || slot >= slots.Count || slot >= state.current.Length)
                    {
                        return;
                    }

                    lastFrame = Time.frameCount;
                    bool wasEmpty = state.current[slot] == KeyCode.None;
                    ClearSlot(row, slots[slot], slot);
                    stop();
                    MarkConflicts(rows);
                    if (!wasEmpty)
                    {
                        Raise(Bind(row, slot, KeyCode.None));
                    }

                    evt.StopImmediatePropagation();
                    return;
                }

                if (listening == null)
                {
                    return;
                }

                if (evt.keyCode == KeyCode.Escape)
                {
                    lastFrame = Time.frameCount;
                    stop();
                    evt.StopImmediatePropagation();
                    return;
                }

                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    return;
                }

                if (LabelOf(evt.keyCode) == null)
                {
                    return;
                }

                List<Label> keys = Slots(listening);
                if (listeningSlot < 0 || listeningSlot >= keys.Count)
                {
                    return;
                }

                lastFrame = Time.frameCount;
                VisualElement bound = listening;
                int boundSlot = listeningSlot;
                KeyCode previous = CurrentOf(bound, boundSlot);
                FillSlot(bound, keys[listeningSlot], listeningSlot, evt.keyCode);
                stop();
                MarkConflicts(rows);
                if (previous != evt.keyCode)
                {
                    Raise(Bind(bound, boundSlot, evt.keyCode));
                }

                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            MarkConflicts(rows);
        }

        /// <summary>Restores keys authored in UXML and reports changed slots.</summary>
        public static void RestoreDefaults(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            List<VisualElement> rows = HcfScreenInteractions.Collect(root, BindPrefix);
            List<HcfBinding> changed = new List<HcfBinding>();

            foreach (VisualElement row in rows)
            {
                RowState state;
                if (s_State.TryGetValue(row, out state))
                {
                    List<Label> slots = Slots(row);
                    for (int i = 0; i < slots.Count && i < state.defaults.Length; i++)
                    {
                        KeyCode before = state.current[i];
                        KeyCode restored = state.defaults[i];
                        if (restored == KeyCode.None)
                        {
                            ClearSlot(row, slots[i], i);
                        }
                        else
                        {
                            FillSlot(row, slots[i], i, restored);
                        }

                        if (before != restored)
                        {
                            changed.Add(Bind(row, i, restored));
                        }
                    }
                }

                row.RemoveFromClassList(ListeningClass);
            }

            MarkConflicts(rows);

            for (int i = 0; i < changed.Count; i++)
            {
                Raise(changed[i]);
            }
        }

        /// <summary>Returns the current slots, including unassigned slots.</summary>
        public static List<HcfBinding> Bindings(VisualElement root)
        {
            List<HcfBinding> bindings = new List<HcfBinding>();
            if (root == null)
            {
                return bindings;
            }

            foreach (VisualElement row in HcfScreenInteractions.Collect(root, BindPrefix))
            {
                RowState state = State(row);
                for (int i = 0; i < state.current.Length; i++)
                {
                    bindings.Add(Bind(row, i, state.current[i]));
                }
            }

            return bindings;
        }

        static HcfBinding Bind(VisualElement row, int slot, KeyCode key)
        {
            return new HcfBinding
            {
                actionId = ActionId(row),
                slot = slot,
                key = key,
                row = row,
            };
        }

        static void Raise(HcfBinding binding)
        {
            System.Action<HcfBinding> handler = bindingChanged;
            if (handler != null)
            {
                handler(binding);
            }
        }

        static string ActionId(VisualElement row)
        {
            string name = row == null ? null : row.name;
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            return name.StartsWith(BindPrefix, System.StringComparison.Ordinal)
                ? name.Substring(BindPrefix.Length)
                : name;
        }

        static RowState Snapshot(VisualElement row)
        {
            List<Label> slots = Slots(row);
            Label hint = Hint(row);
            RowState state = new RowState
            {
                current = new KeyCode[slots.Count],
                defaults = new KeyCode[slots.Count],
                empty = new bool[slots.Count],
                ghost = new string[slots.Count],
                hint = hint == null ? null : hint.text,
            };

            for (int i = 0; i < slots.Count; i++)
            {
                KeyCode key = IsEmpty(slots[i]) ? KeyCode.None : KeyOf(slots[i].text);

                bool empty = key == KeyCode.None;
                state.empty[i] = empty;
                state.ghost[i] = empty ? slots[i].text : null;
                state.defaults[i] = key;
                state.current[i] = key;
            }

            return state;
        }

        // The line the markup authored, so editing it in UXML reaches the screen. The literal
        // only stands in for a row whose hint label is missing or blank.
        static string PrimaryHintOf(VisualElement row)
        {
            RowState state = State(row);
            return string.IsNullOrEmpty(state.hint) ? PrimaryHint : state.hint;
        }

        static RowState State(VisualElement row)
        {
            RowState state;
            if (!s_State.TryGetValue(row, out state))
            {
                state = Snapshot(row);
                s_State.Add(row, state);
            }

            return state;
        }

        static KeyCode CurrentOf(VisualElement row, int slot)
        {
            RowState state = State(row);
            return slot >= 0 && slot < state.current.Length ? state.current[slot] : KeyCode.None;
        }

        static void FillSlot(VisualElement row, Label slot, int index, KeyCode key)
        {
            RowState state = State(row);
            if (index >= 0 && index < state.current.Length)
            {
                state.current[index] = key;
            }

            slot.text = LabelOf(key);
            slot.RemoveFromClassList(EmptyClass);
            ApplyGlyphClass(slot);
        }

        static void ApplyGlyphClass(Label slot)
        {
            string text = slot.text;
            bool glyph = !string.IsNullOrEmpty(text) && GlyphChars.IndexOf(text[0]) >= 0;
            slot.EnableInClassList(GlyphClass, glyph);
        }

        static void ClearSlot(VisualElement row, Label slot, int index)
        {
            RowState state = State(row);
            if (index >= 0 && index < state.current.Length)
            {
                state.current[index] = KeyCode.None;
            }

            slot.text = Ghost(row, index);
            ApplyGlyphClass(slot);
            slot.AddToClassList(EmptyClass);
        }

        static string Ghost(VisualElement row, int index)
        {
            RowState state = State(row);
            if (index >= 0 && index < state.ghost.Length && state.empty[index])
            {
                return state.ghost[index];
            }

            return EmptyKey;
        }

        static bool IsEmpty(Label slot)
        {
            // Snapshot reads authored state before a RowState is available.
            return slot.ClassListContains(EmptyClass)
                || string.IsNullOrEmpty(slot.text)
                || string.CompareOrdinal(slot.text, EmptyKey) == 0;
        }

        static int LastFilled(RowState state)
        {
            for (int i = state.current.Length - 1; i >= 0; i--)
            {
                if (state.current[i] != KeyCode.None)
                {
                    return i;
                }
            }

            return -1;
        }

        static void MarkConflicts(List<VisualElement> rows)
        {
            Dictionary<KeyCode, int> counts = new Dictionary<KeyCode, int>();
            HashSet<KeyCode> inRow = new HashSet<KeyCode>();
            foreach (VisualElement row in rows)
            {
                RowState state = State(row);
                inRow.Clear();
                for (int i = 0; i < state.current.Length; i++)
                {
                    if (state.current[i] == KeyCode.None || !inRow.Add(state.current[i]))
                    {
                        continue;
                    }

                    int n;
                    counts.TryGetValue(state.current[i], out n);
                    counts[state.current[i]] = n + 1;
                }
            }

            foreach (VisualElement row in rows)
            {
                RowState state = State(row);
                bool conflicted = false;
                for (int i = 0; i < state.current.Length; i++)
                {
                    int n;
                    if (state.current[i] != KeyCode.None
                        && counts.TryGetValue(state.current[i], out n) && n > 1)
                    {
                        conflicted = true;
                        break;
                    }
                }

                row.EnableInClassList(ResolvedClass, !conflicted);
            }
        }

        static VisualElement FocusedRow(VisualElement root, List<VisualElement> rows)
        {
            FocusController focus = root.focusController;
            VisualElement node = focus == null ? null : focus.focusedElement as VisualElement;
            for (; node != null; node = node.parent)
            {
                if (rows.Contains(node))
                {
                    return node;
                }
            }

            return null;
        }

        static string LabelOf(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return key.ToString();
            }

            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            {
                return (key - KeyCode.Alpha0).ToString();
            }

            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            {
                return (key - KeyCode.Keypad0).ToString();
            }

            if (key >= KeyCode.F1 && key <= KeyCode.F12)
            {
                return "F" + (key - KeyCode.F1 + 1);
            }

            switch (key)
            {
                case KeyCode.UpArrow:
                    return "↑";
                case KeyCode.DownArrow:
                    return "↓";
                case KeyCode.LeftArrow:
                    return "←";
                case KeyCode.RightArrow:
                    return "→";
                case KeyCode.Space:
                    return "SPACE";
                case KeyCode.Tab:
                    return "TAB";
                case KeyCode.Escape:
                    return "ESC";
                case KeyCode.Return:
                    return "ENTER";
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "SHIFT";
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "CTRL";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return "ALT";
                case KeyCode.Comma:
                    return ",";
                case KeyCode.Period:
                    return ".";
                case KeyCode.Slash:
                    return "/";
                case KeyCode.Semicolon:
                    return ";";
                default:
                    return null;
            }
        }

        static KeyCode KeyOf(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return KeyCode.None;
            }

            if (label.Length == 1)
            {
                char single = label[0];
                if (single >= 'A' && single <= 'Z')
                {
                    return KeyCode.A + (single - 'A');
                }

                if (single >= '0' && single <= '9')
                {
                    return KeyCode.Alpha0 + (single - '0');
                }
            }

            int number;
            if (label.Length > 1 && label[0] == 'F'
                && int.TryParse(label.Substring(1), out number) && number >= 1 && number <= 12)
            {
                return KeyCode.F1 + (number - 1);
            }

            switch (label)
            {
                case "↑":
                    return KeyCode.UpArrow;
                case "↓":
                    return KeyCode.DownArrow;
                case "←":
                    return KeyCode.LeftArrow;
                case "→":
                    return KeyCode.RightArrow;
                case "SPACE":
                    return KeyCode.Space;
                case "TAB":
                    return KeyCode.Tab;
                case "SHIFT":
                    return KeyCode.LeftShift;
                case "CTRL":
                    return KeyCode.LeftControl;
                case "ALT":
                    return KeyCode.LeftAlt;
                case "ESC":
                    return KeyCode.Escape;
                case "ENTER":
                    return KeyCode.Return;
                case ",":
                    return KeyCode.Comma;
                case ".":
                    return KeyCode.Period;
                case "/":
                    return KeyCode.Slash;
                case ";":
                    return KeyCode.Semicolon;
                default:
                    return KeyCode.None;
            }
        }

        static List<Label> Slots(VisualElement row)
        {
            List<Label> found = new List<Label>();
            VisualElement keys = FindByClass(row, KeysClass);
            if (keys == null)
            {
                return found;
            }

            keys.Query<Label>().ForEach(label =>
            {
                if (label.ClassListContains(KeyClass))
                {
                    found.Add(label);
                }
            });

            return found;
        }

        static Label Hint(VisualElement row)
        {
            Label found = null;
            row.Query<Label>().ForEach(label =>
            {
                if (found == null && label.ClassListContains(HintClass))
                {
                    found = label;
                }
            });

            return found;
        }

        static VisualElement FindByClass(VisualElement owner, string className)
        {
            if (owner == null)
            {
                return null;
            }

            VisualElement found = null;
            owner.Query<VisualElement>().ForEach(element =>
            {
                if (found == null && element.ClassListContains(className))
                {
                    found = element;
                }
            });

            return found;
        }
    }
}
