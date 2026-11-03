using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using HorrorCaseFile.UI;

namespace HorrorCaseFile.Demo
{
    /// <summary>Wires settings previews, defaults and the unsaved indicator.</summary>
    public static class HcfSettingsControls
    {
        // The two ends of the calibration veil come from USS so a repainted palette
        // reaches them; the constants are only the fallback for a screen that does not
        // declare the properties.
        static readonly CustomStyleProperty<Color> s_InkProperty =
            new CustomStyleProperty<Color>("--hcf-preview-ink");

        static readonly CustomStyleProperty<Color> s_LiftProperty =
            new CustomStyleProperty<Color>("--hcf-preview-lift");

        static readonly Color PreviewInk = (Color)new Color32(13, 12, 11, 255);

        static readonly Color PreviewLift = (Color)new Color32(236, 230, 218, 255);

        const float MaxCrush = 0.97f;
        const float MaxLift = 0.18f;

        static readonly ConditionalWeakTable<VisualElement, object> s_Wired =
            new ConditionalWeakTable<VisualElement, object>();

        public static void Wire(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            object marker;
            if (s_Wired.TryGetValue(root, out marker))
            {
                return;
            }

            s_Wired.Add(root, new object());

            Label unsaved = root.Q<Label>("unsaved");
            Defaults defaults = new Defaults(root);

            WireDirty(root, unsaved, defaults);
            WireCalibration(root);
            WireGrainSwitch(root);
        }

        static void WireGrainSwitch(VisualElement root)
        {
            Toggle grain = root.Q<Toggle>("switch-film-grain");
            if (grain == null)
            {
                return;
            }

            // The screen is rebuilt from UXML on every visit, so the toggle starts at the
            // value the markup carries, and the director applies the kept grain only after
            // the screen is wired. The state is therefore read back one panel update later,
            // from the overlays that show it. Grain hotkeys are handled above this screen,
            // so a key press schedules the same read.
            System.Action sync = () =>
                grain.schedule.Execute(() => grain.SetValueWithoutNotify(GrainIsOn()));

            sync();
            root.RegisterCallback<KeyDownEvent>(evt => sync());

            grain.RegisterValueChangedCallback(evt =>
            {
                HcfDemoDirector director = HcfDemoDirector.Instance;
                if (director != null)
                {
                    director.SetGrainEnabled(evt.newValue);
                }
            });
        }

        static bool GrainIsOn()
        {
            IReadOnlyList<HcfGrainOverlay> live = HcfGrainOverlay.live;
            for (int i = 0; i < live.Count; i++)
            {
                if (live[i].grain > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        static void WireCalibration(VisualElement root)
        {
            Slider brightness = root.Q<Slider>("slider-brightness");
            VisualElement veil = root.Q<VisualElement>("calibration-veil");
            if (brightness == null || veil == null)
            {
                return;
            }

            float pivot = brightness.value;
            float low = brightness.lowValue;
            float high = brightness.highValue;

            System.Action<float> paint = value =>
            {
                Color ink;
                Color lift;
                if (!veil.customStyle.TryGetValue(s_InkProperty, out ink)) { ink = PreviewInk; }
                if (!veil.customStyle.TryGetValue(s_LiftProperty, out lift)) { lift = PreviewLift; }
                veil.style.backgroundColor = VeilColor(value, pivot, low, high, ink, lift);
            };

            paint(brightness.value);
            brightness.RegisterValueChangedCallback(evt => paint(evt.newValue));
            veil.RegisterCallback<CustomStyleResolvedEvent>(evt => paint(brightness.value));
        }

        static Color VeilColor(float value, float pivot, float low, float high, Color inkColor, Color liftColor)
        {
            if (value < pivot)
            {
                float t = pivot > low ? Mathf.InverseLerp(low, pivot, value) : 1f;
                Color ink = inkColor;
                ink.a = Mathf.Lerp(MaxCrush, 0f, t);
                return ink;
            }

            float u = high > pivot ? Mathf.InverseLerp(pivot, high, value) : 0f;
            Color lift = liftColor;
            lift.a = Mathf.Lerp(0f, MaxLift, u);
            return lift;
        }

        static void WireDirty(VisualElement root, Label unsaved, Defaults defaults)
        {
            System.Action<bool> setDirty = dirty =>
            {
                if (unsaved != null)
                {
                    unsaved.style.visibility = dirty ? Visibility.Visible : Visibility.Hidden;
                }
            };

            // User edits show the indicator; Apply, Defaults and Reset clear it.
            root.RegisterCallback<ChangeEvent<float>>(evt => setDirty(true));
            root.RegisterCallback<ChangeEvent<int>>(evt => setDirty(true));
            root.RegisterCallback<ChangeEvent<bool>>(evt => setDirty(true));
            root.RegisterCallback<ChangeEvent<string>>(evt => setDirty(true));

            HcfDemoFlow.OnClick(root, "btn-apply", () => setDirty(false));
            HcfDemoFlow.OnClick(root, "btn-defaults", () =>
            {
                defaults.Restore();
                setDirty(false);
            });
            HcfDemoFlow.OnClick(root, "btn-reset-bindings", () =>
            {
                HcfRebind.RestoreDefaults(root);
                setDirty(false);
            });
        }

        sealed class Defaults
        {
            readonly List<Slider> m_Sliders = new List<Slider>();
            readonly List<float> m_SliderValues = new List<float>();
            readonly List<Toggle> m_Toggles = new List<Toggle>();
            readonly List<bool> m_ToggleValues = new List<bool>();
            readonly List<RadioButtonGroup> m_Groups = new List<RadioButtonGroup>();
            readonly List<int> m_GroupValues = new List<int>();
            readonly List<DropdownField> m_Dropdowns = new List<DropdownField>();
            readonly List<int> m_DropdownValues = new List<int>();

            public Defaults(VisualElement root)
            {
                root.Query<Slider>().ForEach(slider =>
                {
                    m_Sliders.Add(slider);
                    m_SliderValues.Add(slider.value);
                });
                root.Query<Toggle>().ForEach(toggle =>
                {
                    m_Toggles.Add(toggle);
                    m_ToggleValues.Add(toggle.value);
                });
                root.Query<RadioButtonGroup>().ForEach(group =>
                {
                    m_Groups.Add(group);
                    m_GroupValues.Add(group.value);
                });
                root.Query<DropdownField>().ForEach(dropdown =>
                {
                    m_Dropdowns.Add(dropdown);
                    m_DropdownValues.Add(dropdown.index);
                });
            }

            public void Restore()
            {
                for (int i = 0; i < m_Sliders.Count; i++)
                {
                    m_Sliders[i].value = m_SliderValues[i];
                }

                for (int i = 0; i < m_Toggles.Count; i++)
                {
                    m_Toggles[i].value = m_ToggleValues[i];
                }

                for (int i = 0; i < m_Groups.Count; i++)
                {
                    m_Groups[i].value = m_GroupValues[i];
                }

                for (int i = 0; i < m_Dropdowns.Count; i++)
                {
                    m_Dropdowns[i].index = m_DropdownValues[i];
                }
            }
        }
    }
}
