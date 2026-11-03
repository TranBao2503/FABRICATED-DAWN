using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Wires shared selection, navigation and value readout behavior.</summary>
    public static class HcfScreenInteractions
    {
        const string ActiveClass = "is-active";
        const string MenuPrefix = "menu-item-";
        const string CellPrefix = "cell-";
        const string TabPrefix = "tab-";
        const string SliderPrefix = "slider-";
        const string ProgressPrefix = "progress-";
        const string ValueSuffix = "-value";

        const string KeyboardClass = "hcf-kbd";

        const string PickOnClickClass = "hcf-pick-on-click";

        const string DropdownPopupClass = "unity-base-dropdown";

        // Used when no --hcf-slider-steps is declared.
        const float SliderStepsFallback = 20f;

        static readonly CustomStyleProperty<float> s_SliderStepsProperty =
            new CustomStyleProperty<float>("--hcf-slider-steps");

        // Pixel gap that tells a real mouse move from the synthetic moves a panel emits on layout
        // changes. Tuned by hand, not a knob for content.
        const float PointerNoise = 2f;

        // Input mode persists while screen roots are replaced.
        static bool s_Keyboard;
        static Vector2 s_Pointer;
        static bool s_PointerKnown;

        // Wired roots, held weakly so a screen the host drops is collected anyway. The input
        // mode above is one global flag, so the class it drives has to reach every live root
        // and not only the one that caught the event: a HUD and a pause overlay can sit in
        // one panel as siblings, and the rings must appear on both.
        static readonly List<System.WeakReference> s_Roots = new List<System.WeakReference>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPlayerState()
        {
            s_Keyboard = false;
            s_Pointer = Vector2.zero;
            s_PointerKnown = false;
            s_Roots.Clear();
        }

        static readonly ConditionalWeakTable<VisualElement, object> s_Wired =
            new ConditionalWeakTable<VisualElement, object>();

        /// <summary>Wires a populated screen root once.</summary>
        public static void Wire(VisualElement root)
        {
            if (root == null || root.childCount == 0)
            {
                return;
            }

            object marker;
            if (s_Wired.TryGetValue(root, out marker))
            {
                return;
            }

            s_Wired.Add(root, new object());

            WireSelection(root, MenuPrefix);
            WireSelection(root, CellPrefix);
            WireTabs(root);
            WireSliderReadouts(root);
            WireProgressReadouts(root);

            WireKeyboardMode(root);
            WireArrowNavigation(root);
        }

        /// <summary>Tracks the active element in a named selection group.</summary>
        public sealed class Selection
        {
            readonly List<VisualElement> m_Items;
            readonly ReadOnlyCollection<VisualElement> m_View;
            int m_Index;

            public event System.Action<int, VisualElement> changed;

            internal Selection(List<VisualElement> items, int index)
            {
                m_Items = items;
                m_View = new ReadOnlyCollection<VisualElement>(items);
                m_Index = index;
            }

            /// <summary>Read-only view of the group items.</summary>
            public IList<VisualElement> items => m_View;

            public int index => m_Index;

            public VisualElement current => m_Index >= 0 && m_Index < m_Items.Count ? m_Items[m_Index] : null;

            public void Select(int index)
            {
                if (index < 0 || index >= m_Items.Count || index == m_Index)
                {
                    return;
                }

                Apply(index);
            }

            /// <summary>Applies an index and raises changed, including for -1.</summary>
            public void Apply(int index)
            {
                for (int i = 0; i < m_Items.Count; i++)
                {
                    m_Items[i].EnableInClassList(ActiveClass, i == index);
                }

                m_Index = index;
                if (changed != null)
                {
                    changed(index, current);
                }
            }
        }

        static readonly ConditionalWeakTable<VisualElement, Dictionary<string, Selection>> s_Groups =
            new ConditionalWeakTable<VisualElement, Dictionary<string, Selection>>();

        static Dictionary<string, Selection> Groups(VisualElement root, bool create)
        {
            Dictionary<string, Selection> map;
            if (s_Groups.TryGetValue(root, out map))
            {
                return map;
            }

            if (!create)
            {
                return null;
            }

            map = new Dictionary<string, Selection>();
            s_Groups.Add(root, map);
            return map;
        }

        /// <summary>Returns a selection group created while wiring the root.</summary>
        public static Selection GetSelection(VisualElement root, string prefix)
        {
            Dictionary<string, Selection> map = root == null ? null : Groups(root, false);
            Selection group;
            if (map == null || !map.TryGetValue(prefix, out group))
            {
                return null;
            }

            return group;
        }

        static void WireSelection(VisualElement root, string prefix)
        {
            List<VisualElement> items = Collect(root, prefix);
            if (items.Count == 0)
            {
                return;
            }

            // Keep -1 when UXML has no active item. Starting at 0 would make Select(0) return
            // before adding the active class.
            int active = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ClassListContains(ActiveClass))
                {
                    active = i;
                    break;
                }
            }

            Selection group = new Selection(items, active);
            Groups(root, true)[prefix] = group;

            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                VisualElement item = items[i];

                if (!PicksOnClick(item))
                {
                    item.RegisterCallback<PointerEnterEvent>(evt =>
                    {
                        if (!item.enabledInHierarchy)
                        {
                            return;
                        }

                        group.Select(index);
                        item.Focus();
                    });
                }

                // Buttons consume pointer events, so selection listens in TrickleDown.
                item.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (!item.enabledInHierarchy)
                    {
                        return;
                    }

                    group.Select(index);
                    item.Focus();
                }, TrickleDown.TrickleDown);
            }
        }

        static bool PicksOnClick(VisualElement item)
        {
            for (VisualElement node = item; node != null; node = node.parent)
            {
                if (node.ClassListContains(PickOnClickClass))
                {
                    return true;
                }
            }

            return false;
        }

        static void WireKeyboardMode(VisualElement root)
        {
            Track(root);
            ApplyKeyboardMode(root);

            root.RegisterCallback<NavigationMoveEvent>(evt => SetKeyboardMode(true), TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(evt => SetKeyboardMode(true), TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(evt => SetKeyboardMode(true), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (TrackPointer(evt.position))
                {
                    SetKeyboardMode(false);
                }
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(evt =>
            {
                TrackPointer(evt.position);
                SetKeyboardMode(false);
            }, TrickleDown.TrickleDown);
        }

        static void Track(VisualElement root)
        {
            for (int i = s_Roots.Count - 1; i >= 0; i--)
            {
                object held = s_Roots[i].Target;
                if (held == null || held == root)
                {
                    s_Roots.RemoveAt(i);
                }
            }

            s_Roots.Add(new System.WeakReference(root));
        }

        static bool TrackPointer(Vector2 position)
        {
            Vector2 last = s_Pointer;
            bool known = s_PointerKnown;

            s_Pointer = position;
            s_PointerKnown = true;

            return known && (position - last).sqrMagnitude > PointerNoise * PointerNoise;
        }

        static void SetKeyboardMode(bool on)
        {
            s_Keyboard = on;

            for (int i = s_Roots.Count - 1; i >= 0; i--)
            {
                VisualElement root = s_Roots[i].Target as VisualElement;
                if (root == null)
                {
                    s_Roots.RemoveAt(i);
                    continue;
                }

                ApplyKeyboardMode(root);
            }
        }

        static void ApplyKeyboardMode(VisualElement root)
        {
            if (root.ClassListContains(KeyboardClass) == s_Keyboard)
            {
                return;
            }

            root.EnableInClassList(KeyboardClass, s_Keyboard);
        }

        static void WireArrowNavigation(VisualElement root)
        {
            List<VisualElement> nodes = CollectNodes(root);
            if (nodes.Count == 0)
            {
                return;
            }

            Dictionary<string, Selection> groups = Groups(root, false);

            List<VisualElement> tabs = Collect(root, TabPrefix);

            root.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                bool sequential = false;
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Left:
                    case NavigationMoveEvent.Direction.Right:
                    case NavigationMoveEvent.Direction.Up:
                    case NavigationMoveEvent.Direction.Down:
                        break;
                    case NavigationMoveEvent.Direction.Next:
                    case NavigationMoveEvent.Direction.Previous:
                        if (tabs.Count > 0)
                        {
                            return;
                        }

                        sequential = true;
                        break;
                    default:
                        return;
                }

                if (DropdownOpen(root))
                {
                    return;
                }

                FocusController controller = root.focusController;
                VisualElement focused = controller == null ? null : controller.focusedElement as VisualElement;
                VisualElement from = Owner(nodes, focused);
                if (from == null)
                {
                    VisualElement entry = focused == root ? Entry(groups, nodes) : null;
                    if (entry == null)
                    {
                        return;
                    }

                    FocusControl(entry);
                    Consume(root, evt);
                    return;
                }

                if (!sequential && StepAlongAxis(root, from, evt))
                {
                    return;
                }

                VisualElement target = sequential
                    ? StepInOrder(nodes, from, evt.direction == NavigationMoveEvent.Direction.Next)
                    : Nearest(nodes, from, evt.direction);
                if (target != null)
                {
                    target = PreferActiveTab(tabs, from, target);

                    SelectIn(groups, target);
                    FocusControl(target);
                }

                Consume(root, evt);
            });
        }

        static VisualElement StepInOrder(List<VisualElement> nodes, VisualElement from, bool forward)
        {
            int start = nodes.IndexOf(from);
            if (start < 0)
            {
                return null;
            }

            int step = forward ? 1 : -1;
            for (int i = 1; i <= nodes.Count; i++)
            {
                int index = (start + step * i) % nodes.Count;
                if (index < 0)
                {
                    index += nodes.Count;
                }

                if (IsLive(nodes[index]))
                {
                    return nodes[index];
                }
            }

            return null;
        }

        static List<VisualElement> CollectNodes(VisualElement root)
        {
            List<VisualElement> nodes = new List<VisualElement>();
            root.Query<VisualElement>().ForEach(element =>
            {
                if (IsControl(element))
                {
                    nodes.Add(element);
                }
            });
            return nodes;
        }

        /// <summary>True for the elements arrow navigation steps between.</summary>
        /// <remarks>
        /// Kit controls plus the drag-zoom viewports: those are plain VisualElement, but they
        /// answer arrows themselves, so navigation has to be able to reach them. A screen made
        /// of one viewport (S12) would otherwise have no navigation graph at all.
        /// </remarks>
        public static bool IsControl(VisualElement element)
        {
            if (element == null)
            {
                return false;
            }

            bool control = element is Button
                || element is Toggle
                || element is Slider
                || element is DropdownField
                || element is RadioButtonGroup
                || HcfDragZoom.IsViewport(element);
            return control && !InsideScroller(element);
        }

        static bool InsideScroller(VisualElement element)
        {
            for (VisualElement parent = element.parent; parent != null; parent = parent.parent)
            {
                if (parent is Scroller)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the element is enabled and no ancestor has display set to none.</summary>
        public static bool IsShown(VisualElement element)
        {
            if (element == null || !element.enabledInHierarchy)
            {
                return false;
            }

            // display does not inherit, so a hidden tab page reports none only on itself:
            // the walk is what keeps the controls inside it out of the navigation graph.
            for (VisualElement node = element; node != null; node = node.parent)
            {
                if (node.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsLive(VisualElement node)
        {
            if (!IsShown(node))
            {
                return false;
            }

            Rect bounds = node.worldBound;
            return bounds.width > 0f && bounds.height > 0f;
        }

        static bool DropdownOpen(VisualElement root)
        {
            IPanel panel = root.panel;
            VisualElement tree = panel == null ? null : panel.visualTree;
            return tree != null && tree.Q<VisualElement>(null, DropdownPopupClass) != null;
        }

        static VisualElement Owner(List<VisualElement> nodes, VisualElement focused)
        {
            for (VisualElement element = focused; element != null; element = element.parent)
            {
                if (nodes.Contains(element))
                {
                    return element;
                }
            }

            return null;
        }

        static bool StepAlongAxis(VisualElement root, VisualElement from, NavigationMoveEvent evt)
        {
            Slider slider = from as Slider;
            if (slider != null)
            {
                int step = AxisStep(evt.direction, slider.direction == SliderDirection.Horizontal);
                if (step == 0)
                {
                    return false;
                }

                if (slider.inverted)
                {
                    step = -step;
                }

                float span = slider.highValue - slider.lowValue;
                slider.value = Mathf.Clamp(slider.value + step * span / SliderSteps(slider),
                    slider.lowValue, slider.highValue);
                Consume(root, evt);
                return true;
            }

            RadioButtonGroup group = from as RadioButtonGroup;
            if (group != null)
            {
                Rect bounds = group.worldBound;
                int step = AxisStep(evt.direction, bounds.width >= bounds.height);
                if (step == 0)
                {
                    return false;
                }

                List<RadioButton> segments = Segments(group);
                int next = NextSegment(segments, group.value, step);
                if (next >= 0)
                {
                    group.value = next;

                    segments[next].Focus();
                }

                Consume(root, evt);
                return true;
            }

            return false;
        }

        // Number of arrow steps across the slider range.
        static float SliderSteps(Slider slider)
        {
            // Unity does not send CustomStyleResolvedEvent for a value inherited from an ancestor.
            // The rule targets each slider directly, and the value is cheap to read on demand.
            float steps;
            if (!slider.customStyle.TryGetValue(s_SliderStepsProperty, out steps))
            {
                return SliderStepsFallback;
            }

            // Below one step an arrow press would overshoot the range, and zero would divide by it.
            // The comparison also rejects NaN.
            return steps > 1f ? steps : 1f;
        }

        static int AxisStep(NavigationMoveEvent.Direction direction, bool horizontal)
        {
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Right: return horizontal ? 1 : 0;
                case NavigationMoveEvent.Direction.Left: return horizontal ? -1 : 0;
                case NavigationMoveEvent.Direction.Up: return horizontal ? 0 : 1;
                case NavigationMoveEvent.Direction.Down: return horizontal ? 0 : -1;
                default: return 0;
            }
        }

        static List<RadioButton> Segments(RadioButtonGroup group)
        {
            return group.Query<RadioButton>().ToList();
        }

        static int NextSegment(List<RadioButton> segments, int from, int step)
        {
            if (segments.Count == 0)
            {
                return -1;
            }

            for (int i = Mathf.Clamp(from + step, 0, segments.Count - 1);
                i >= 0 && i < segments.Count;
                i += step)
            {
                if (IsShown(segments[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        static VisualElement Nearest(List<VisualElement> nodes, VisualElement from,
            NavigationMoveEvent.Direction direction)
        {
            bool horizontal;
            bool forward;
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Left: horizontal = true; forward = false; break;
                case NavigationMoveEvent.Direction.Right: horizontal = true; forward = true; break;
                case NavigationMoveEvent.Direction.Up: horizontal = false; forward = false; break;
                case NavigationMoveEvent.Direction.Down: horizontal = false; forward = true; break;
                default: return null;
            }

            Rect origin = from.worldBound;
            VisualElement best = null;
            float bestScore = 0f;
            foreach (VisualElement node in nodes)
            {
                if (!IsLive(node))
                {
                    continue;
                }

                Rect bounds = node.worldBound;
                float mark = horizontal ? bounds.center.x : bounds.center.y;
                float edge = horizontal
                    ? (forward ? origin.xMax : origin.xMin)
                    : (forward ? origin.yMax : origin.yMin);
                if (forward ? mark <= edge : mark >= edge)
                {
                    continue;
                }

                float score = Gap(origin, bounds, horizontal, forward)
                    + 0.5f * Spread(origin, bounds, horizontal);
                if (best == null || score < bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best;
        }

        static VisualElement PreferActiveTab(List<VisualElement> tabs, VisualElement from, VisualElement target)
        {
            if (tabs.Count == 0 || !tabs.Contains(target) || tabs.Contains(from))
            {
                return target;
            }

            foreach (VisualElement tab in tabs)
            {
                if (tab.parent == target.parent && tab.ClassListContains(ActiveClass) && IsLive(tab))
                {
                    return tab;
                }
            }

            return target;
        }

        static float Gap(Rect origin, Rect bounds, bool horizontal, bool forward)
        {
            float gap;
            if (horizontal)
            {
                gap = forward ? bounds.xMin - origin.xMax : origin.xMin - bounds.xMax;
            }
            else
            {
                gap = forward ? bounds.yMin - origin.yMax : origin.yMin - bounds.yMax;
            }

            return gap > 0f ? gap : 0f;
        }

        static float Spread(Rect origin, Rect bounds, bool horizontal)
        {
            float originLow = horizontal ? origin.yMin : origin.xMin;
            float originHigh = horizontal ? origin.yMax : origin.xMax;
            float boundsLow = horizontal ? bounds.yMin : bounds.xMin;
            float boundsHigh = horizontal ? bounds.yMax : bounds.xMax;

            if (boundsLow > originHigh)
            {
                return boundsLow - originHigh;
            }

            if (originLow > boundsHigh)
            {
                return originLow - boundsHigh;
            }

            return 0f;
        }

        static void SelectIn(Dictionary<string, Selection> groups, VisualElement target)
        {
            if (groups == null)
            {
                return;
            }

            foreach (Selection group in groups.Values)
            {
                for (int i = 0; i < group.items.Count; i++)
                {
                    if (group.items[i] == target)
                    {
                        group.Select(i);
                        return;
                    }
                }
            }
        }

        static VisualElement Entry(Dictionary<string, Selection> groups, List<VisualElement> nodes)
        {
            VisualElement active = FirstActive(groups);
            if (active != null)
            {
                return active;
            }

            foreach (VisualElement node in nodes)
            {
                if (IsLive(node))
                {
                    return node;
                }
            }

            return null;
        }

        static VisualElement FirstActive(Dictionary<string, Selection> groups)
        {
            if (groups == null)
            {
                return null;
            }

            foreach (Selection group in groups.Values)
            {
                VisualElement current = group.current;
                if (current != null && IsLive(current))
                {
                    return current;
                }
            }

            return null;
        }

        /// <summary>Focuses a control and scrolls it into view.</summary>
        public static void FocusControl(VisualElement node)
        {
            if (node == null)
            {
                return;
            }

            RadioButtonGroup group = node as RadioButtonGroup;
            if (group != null)
            {
                List<RadioButton> segments = Segments(group);
                if (group.value >= 0 && group.value < segments.Count)
                {
                    segments[group.value].Focus();
                    ScrollIntoView(node);
                    return;
                }
            }

            node.Focus();
            ScrollIntoView(node);
        }

        static void ScrollIntoView(VisualElement node)
        {
            for (VisualElement parent = node.parent; parent != null; parent = parent.parent)
            {
                ScrollView scroll = parent as ScrollView;
                if (scroll != null)
                {
                    scroll.ScrollTo(node);
                    return;
                }
            }
        }

        static void Consume(VisualElement root, NavigationMoveEvent evt)
        {
            evt.StopPropagation();
            if (root.focusController != null)
            {
                root.focusController.IgnoreEvent(evt);
            }
        }

        static void WireTabs(VisualElement root)
        {
            List<VisualElement> tabs = Collect(root, TabPrefix);
            if (tabs.Count == 0)
            {
                return;
            }

            foreach (VisualElement tab in tabs)
            {
                Button button = tab as Button;
                if (button == null)
                {
                    continue;
                }

                VisualElement picked = tab;
                button.clicked += () =>
                {
                    foreach (VisualElement other in tabs)
                    {
                        if (other.parent == picked.parent)
                        {
                            other.RemoveFromClassList(ActiveClass);
                        }
                    }

                    picked.AddToClassList(ActiveClass);
                };
            }
        }

        public static void CycleTabs(VisualElement root)
        {
            List<VisualElement> tabs = Collect(root, TabPrefix);
            if (tabs.Count == 0)
            {
                return;
            }

            int active = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].ClassListContains(ActiveClass))
                {
                    active = i;
                    break;
                }
            }

            for (int step = 1; step <= tabs.Count; step++)
            {
                int next = (active + step) % tabs.Count;
                Button button = tabs[next] as Button;
                if (button != null && button.enabledInHierarchy)
                {
                    button.Focus();
                    using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
                    {
                        evt.target = button;
                        button.SendEvent(evt);
                    }

                    return;
                }
            }
        }

        static void WireSliderReadouts(VisualElement root)
        {
            root.Query<Slider>().ForEach(slider =>
            {
                if (string.IsNullOrEmpty(slider.name) || !slider.name.StartsWith(SliderPrefix))
                {
                    return;
                }

                Label readout = root.Q<Label>(slider.name + ValueSuffix);
                if (readout == null)
                {
                    return;
                }

                string sample = readout.text;
                readout.text = Format(sample, slider.value);
                slider.RegisterValueChangedCallback(evt => readout.text = Format(sample, evt.newValue));
            });
        }

        static void WireProgressReadouts(VisualElement root)
        {
            root.Query<ProgressBar>().ForEach(bar =>
            {
                if (string.IsNullOrEmpty(bar.name) || !bar.name.StartsWith(ProgressPrefix))
                {
                    return;
                }

                Label readout = root.Q<Label>(bar.name + ValueSuffix);
                if (readout == null)
                {
                    return;
                }

                string sample = readout.text;
                readout.text = Format(sample, bar.value);
                bar.RegisterValueChangedCallback(evt => readout.text = Format(sample, evt.newValue));
            });
        }

        public static string Format(string sample, float value)
        {
            if (string.IsNullOrEmpty(sample))
            {
                return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
            }

            int first = -1;
            int last = -1;
            for (int i = 0; i < sample.Length; i++)
            {
                if (char.IsDigit(sample[i]))
                {
                    if (first < 0)
                    {
                        first = i;
                    }

                    last = i;
                }
            }

            if (first < 0)
            {
                return sample;
            }

            string head = sample.Substring(0, first);
            string tail = sample.Substring(last + 1);
            string body = sample.Substring(first, last - first + 1);

            int dot = body.IndexOf('.');
            int decimals = dot < 0 ? 0 : body.Length - dot - 1;
            string number = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
            return head + number + tail;
        }

        public static List<VisualElement> Collect(VisualElement root, string prefix)
        {
            List<VisualElement> result = new List<VisualElement>();
            root.Query<VisualElement>().ForEach(element =>
            {
                if (!string.IsNullOrEmpty(element.name) && element.name.StartsWith(prefix))
                {
                    result.Add(element);
                }
            });
            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }
    }
}
