using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using HorrorCaseFile.UI;

namespace HorrorCaseFile.Demo
{
    /// <summary>Loads kit screens and provides demo-only navigation.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class HcfDemoDirector : MonoBehaviour
    {
        const float GrainStep = 5f;
        const float DefaultGrain = 55f;
        const string ActiveClass = "is-active";

        [Tooltip("UIDocument used by the demo scene.")]
        public UIDocument document;

        [Tooltip("Kit screens in demo order.")]
        public VisualTreeAsset[] screens;

        [Tooltip("Startup screen suffix. Falls back to the first screen.")]
        public string startScreen = "BootSplash";

        [Tooltip("Enable screen paging and grain hotkeys.")]
        public bool demoHotkeys = true;

        [Tooltip("Hide the cursor on gameplay screens.")]
        public bool hideCursorInGameplay = true;

        [Tooltip("Cycle through gameplay states until the first user input.")]
        public bool demoStateWalk = true;

        public static HcfDemoDirector Instance { get; private set; }

        readonly Stack<int> m_History = new Stack<int>();

        VisualElement m_Root;
        VisualElement m_Host;
        VisualElement m_Slot;
        int m_Index;
        float m_Grain = DefaultGrain;

        float m_GrainOn = DefaultGrain;
        int m_LastKeyFrame = -1;
        bool m_HideCursor;

        public int ScreenCount => screens != null ? screens.Length : 0;

        public string CurrentSlug => SlugOf(m_Index);

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            UnityEngine.Cursor.visible = true;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            Build();
            Show(StartIndex());
        }

        int StartIndex()
        {
            int index = IndexOf(startScreen);
            return index >= 0 ? index : 0;
        }

        void Build()
        {
            if (m_Root != null)
            {
                return;
            }

            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            if (document == null || document.rootVisualElement == null)
            {
                return;
            }

            m_Root = document.rootVisualElement;
            m_Root.Clear();

            m_Host = new VisualElement { name = "hcf-screen-host" };
            m_Host.style.flexGrow = 1f;
            m_Root.Add(m_Host);

            m_Root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void Show(int index)
        {
            Build();
            if (m_Host == null || ScreenCount == 0)
            {
                return;
            }

            m_Index = Mathf.Clamp(index, 0, ScreenCount - 1);

            m_Host.Clear();
            m_Slot = new VisualElement { name = "hcf-screen-slot" };
            // Slot centering belongs to host structure. The opaque .hcf-screen root supplies color
            // and fills the slot with a 1920x1080 minimum size.
            m_Slot.style.flexGrow = 1f;
            m_Slot.style.alignItems = Align.Center;
            m_Slot.style.justifyContent = Justify.Center;
            m_Slot.focusable = true;
            m_Host.Add(m_Slot);

            VisualTreeAsset asset = screens[m_Index];
            if (asset != null)
            {
                asset.CloneTree(m_Slot);
            }

            HcfScreenInteractions.Wire(m_Slot);
            HcfSpecimenDemo.Wire(m_Slot, CurrentSlug);
            HcfDemoFlow.Wire(m_Slot, CurrentSlug, this);
            HcfSettingsControls.Wire(m_Slot);
            HcfRebind.Wire(m_Slot);
            HcfDragZoom.Wire(m_Slot);

            HcfGrainOverlay.SetGrainAll(m_Grain);

            m_Slot.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
            TakeFocus();

            m_HideCursor = hideCursorInGameplay && IsGameplaySlug(CurrentSlug);
        }

        void TakeFocus()
        {
            VisualElement slot = m_Slot;
            if (slot == null)
            {
                return;
            }

            slot.schedule.Execute(() =>
            {
                if (m_Slot != slot)
                {
                    return;
                }

                VisualElement target = FirstSelectable(slot);
                if (target != null)
                {
                    HcfScreenInteractions.FocusControl(target);
                }
                else
                {
                    slot.Focus();
                }
            });
        }

        static VisualElement FirstSelectable(VisualElement root)
        {
            VisualElement active = null;
            VisualElement first = null;
            root.Query<VisualElement>().ForEach(element =>
            {
                if (!element.focusable || !HcfScreenInteractions.IsShown(element))
                {
                    return;
                }

                if (first == null && HcfScreenInteractions.IsControl(element))
                {
                    first = element;
                }

                if (active == null && element.ClassListContains(ActiveClass))
                {
                    active = element;
                }
            });

            return active != null ? active : first;
        }

        static bool IsGameplaySlug(string slug)
        {
            switch (slug)
            {
                case "Hud":
                case "HudCritical":
                case "HudHiding":
                case "HudPursuit":
                case "Prompts":
                case "Objectives":
                case "Loading":
                    return true;
                default:
                    return false;
            }
        }

        void LateUpdate()
        {
            ApplyCursor();
        }

        void ApplyCursor()
        {
            bool visible = !m_HideCursor;
            if (UnityEngine.Cursor.visible != visible)
            {
                UnityEngine.Cursor.visible = visible;
            }
        }

        void OnSlotPointerDown(PointerDownEvent evt)
        {
            if (m_Slot != null && evt.target == m_Slot)
            {
                m_Slot.Focus();
            }
        }

        public void GoTo(string slug)
        {
            int index = IndexOf(slug);
            if (index < 0)
            {
                return;
            }

            m_History.Push(m_Index);
            Show(index);
        }

        public void GoToReplace(string slug)
        {
            int index = IndexOf(slug);
            if (index < 0)
            {
                return;
            }

            Show(index);
        }

        public void Back()
        {
            if (m_History.Count > 0)
            {
                Show(m_History.Pop());
                return;
            }

            int index = IndexOf(DefaultBackSlug(CurrentSlug));
            if (index >= 0 && index != m_Index)
            {
                Show(index);
            }
        }

        public void ClearHistory()
        {
            m_History.Clear();
        }

        static string DefaultBackSlug(string slug)
        {
            switch (slug)
            {
                case "Hud":
                case "HudCritical":
                case "HudHiding":
                case "HudPursuit":
                case "Prompts":
                case "Objectives":
                case "PauseMenu":
                case "PauseSettings":
                case "Inventory":
                case "InventoryEmpty":
                case "Combine":
                case "Examine":
                case "Journal":
                case "JournalEmpty":
                case "Map":
                case "Map1F":
                case "MapB1":
                    return "Hud";
                default:
                    return "MainMenu";
            }
        }

        int IndexOf(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return -1;
            }

            for (int i = 0; i < ScreenCount; i++)
            {
                if (string.CompareOrdinal(SlugOf(i), slug) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!demoHotkeys)
            {
                return;
            }

            if (Time.frameCount == m_LastKeyFrame)
            {
                return;
            }

            bool handled = true;
            switch (evt.keyCode)
            {
                case KeyCode.PageUp:
                    Step(-1);
                    break;
                case KeyCode.PageDown:
                    Step(1);
                    break;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    StepGrain(-GrainStep);
                    break;
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                case KeyCode.Equals:
                    StepGrain(GrainStep);
                    break;
                default:
                    handled = false;
                    break;
            }

            if (!handled && evt.keyCode == KeyCode.None)
            {
                if (evt.character == '-' || evt.character == '_')
                {
                    StepGrain(-GrainStep);
                    handled = true;
                }
                else if (evt.character == '+' || evt.character == '=')
                {
                    StepGrain(GrainStep);
                    handled = true;
                }
            }

            if (handled)
            {
                m_LastKeyFrame = Time.frameCount;
                evt.StopPropagation();
            }
        }

        void Step(int delta)
        {
            if (ScreenCount == 0)
            {
                return;
            }

            int next = (m_Index + delta) % ScreenCount;
            if (next < 0)
            {
                next += ScreenCount;
            }

            m_History.Clear();
            Show(next);
        }

        void StepGrain(float delta)
        {
            m_Grain = Mathf.Clamp(m_Grain + delta, 0f, 100f);
            if (m_Grain > 0f)
            {
                m_GrainOn = m_Grain;
            }

            HcfGrainOverlay.SetGrainAll(m_Grain);
        }

        public void SetGrainEnabled(bool on)
        {
            if (on)
            {
                m_Grain = m_GrainOn;
            }
            else
            {
                if (m_Grain > 0f)
                {
                    m_GrainOn = m_Grain;
                }

                m_Grain = 0f;
            }

            HcfGrainOverlay.SetGrainAll(m_Grain);
        }

        string SlugOf(int index)
        {
            string assetName = NameOf(index);
            int underscore = assetName.IndexOf('_');
            if (underscore >= 0 && underscore + 1 < assetName.Length)
            {
                return assetName.Substring(underscore + 1);
            }

            return assetName;
        }

        string NameOf(int index)
        {
            if (screens == null || index < 0 || index >= screens.Length || screens[index] == null)
            {
                return string.Empty;
            }

            return screens[index].name;
        }
    }
}
