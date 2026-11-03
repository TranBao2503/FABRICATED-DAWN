using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Adds pointer, wheel and keyboard motion to item and map viewports.</summary>
    /// <remarks>
    /// Unitless USS custom properties on each viewport tune the interaction. Wheel input
    /// changes zoom in fixed steps, pinch zoom is continuous, item arrows rotate, and map
    /// arrows pan.
    /// </remarks>
    public static class HcfDragZoom
    {
        /// <summary>Name of the rotating item viewport (S12).</summary>
        public const string ItemViewportName = "item-viewport";

        /// <summary>Name of the panning map viewport (S14, S14b, S14c).</summary>
        public const string MapViewportName = "map-viewport";

        const string ItemArtName = "item-art";
        const string MapBoardName = "map-board";

        const float Follow = 0.25f;

        // The interpolation factor is tuned for this scheduler interval.
        const long TickMs = 16;

        const float KeyStep = 48f;

        // Shared recognition limits keep small contact movement from starting a pinch.
        const float PinchSlop = 12f;

        // Ignore ratios while the two contacts are too close for a stable span.
        const float PinchMinSpan = 8f;

        // Limit a single sample when pointer tracking resumes with a discontinuity.
        const float PinchMaxRatio = 2f;

        static readonly CustomStyleProperty<float> s_ZoomMinProperty =
            new CustomStyleProperty<float>("--hcf-zoom-min");

        static readonly CustomStyleProperty<float> s_ZoomMaxProperty =
            new CustomStyleProperty<float>("--hcf-zoom-max");

        static readonly CustomStyleProperty<float> s_ZoomStepProperty =
            new CustomStyleProperty<float>("--hcf-zoom-step");

        static readonly CustomStyleProperty<float> s_PanKeyStepProperty =
            new CustomStyleProperty<float>("--hcf-pan-key-step");

        static readonly CustomStyleProperty<float> s_PanOverhangProperty =
            new CustomStyleProperty<float>("--hcf-pan-overhang");

        static readonly CustomStyleProperty<float> s_RotateMaxProperty =
            new CustomStyleProperty<float>("--hcf-rotate-max");

        static readonly CustomStyleProperty<float> s_RotateSpeedProperty =
            new CustomStyleProperty<float>("--hcf-rotate-speed");

        static readonly CustomStyleProperty<float> s_DragFollowProperty =
            new CustomStyleProperty<float>("--hcf-drag-follow");

        static readonly ConditionalWeakTable<VisualElement, object> s_Wired =
            new ConditionalWeakTable<VisualElement, object>();

        /// <summary>True for a supported viewport, including one not wired yet.</summary>
        public static bool IsViewport(VisualElement element)
        {
            string name = element == null ? null : element.name;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return string.CompareOrdinal(name, ItemViewportName) == 0
                || string.CompareOrdinal(name, MapViewportName) == 0;
        }

        /// <summary>Wires supported viewports under a populated screen root once.</summary>
        public static void Wire(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            VisualElement itemViewport = root.Q<VisualElement>(ItemViewportName);
            VisualElement itemArt = itemViewport == null ? null : itemViewport.Q<VisualElement>(ItemArtName);
            bool item = itemViewport != null && itemArt != null;

            VisualElement mapViewport = root.Q<VisualElement>(MapViewportName);
            VisualElement mapBoard = mapViewport == null ? null : mapViewport.Q<VisualElement>(MapBoardName);
            bool map = mapViewport != null && mapBoard != null;

            if (!item && !map)
            {
                return;
            }

            object marker;
            if (s_Wired.TryGetValue(root, out marker))
            {
                return;
            }

            s_Wired.Add(root, new object());

            if (item)
            {
                new ItemInspect(itemViewport, itemArt);
            }

            if (map)
            {
                new MapPan(mapViewport, mapBoard);
            }
        }

        static void Wake(IVisualElementScheduledItem ticker)
        {
            if (!ticker.isActive)
            {
                ticker.Resume();
            }
        }

        static float Read(ICustomStyle style, CustomStyleProperty<float> property, float fallback)
        {
            float value;
            return style.TryGetValue(property, out value) ? value : fallback;
        }

        static float Approach(float current, float target, float snap, float follow)
        {
            float next = Mathf.Lerp(current, target, follow);
            return Mathf.Abs(target - next) <= snap ? target : next;
        }

        static float ZoomDelta(float wheelY, float step)
        {
            if (Mathf.Approximately(wheelY, 0f))
            {
                return 0f;
            }

            // Wheel normalization across mouse and trackpad, not a per-screen setting.
            float notch = Mathf.Clamp(wheelY, -3f, 3f);
            if (Mathf.Abs(notch) < 0.25f)
            {
                notch = notch < 0f ? -0.25f : 0.25f;
            }

            return -notch * step;
        }

        // Track pointer IDs and positions in arrival order.
        sealed class PointerSet
        {
            // Gesture handling is suspended above two contacts. Ignore contacts beyond
            // this fixed storage limit rather than allocating during input.
            const int Capacity = 8;

            readonly int[] m_Ids = new int[Capacity];
            readonly Vector2[] m_Positions = new Vector2[Capacity];

            int m_Count;

            public int Count
            {
                get { return m_Count; }
            }

            public int IdAt(int index)
            {
                return m_Ids[index];
            }

            public Vector2 PositionAt(int index)
            {
                return m_Positions[index];
            }

            public void Down(int pointerId, Vector2 position)
            {
                int at = IndexOf(pointerId);
                if (at >= 0)
                {
                    // Same id down twice without an up in between: a mouse does this on a
                    // second button. Refresh it, do not count it twice.
                    m_Positions[at] = position;
                    return;
                }

                if (m_Count == Capacity)
                {
                    return;
                }

                m_Ids[m_Count] = pointerId;
                m_Positions[m_Count] = position;
                m_Count++;
            }

            public void Move(int pointerId, Vector2 position)
            {
                int at = IndexOf(pointerId);
                if (at >= 0)
                {
                    m_Positions[at] = position;
                }
            }

            public bool Up(int pointerId)
            {
                int at = IndexOf(pointerId);
                if (at < 0)
                {
                    return false;
                }

                m_Count--;
                for (int i = at; i < m_Count; i++)
                {
                    m_Ids[i] = m_Ids[i + 1];
                    m_Positions[i] = m_Positions[i + 1];
                }

                return true;
            }

            public float Span()
            {
                return (m_Positions[0] - m_Positions[1]).magnitude;
            }

            public Vector2 Mid()
            {
                return (m_Positions[0] + m_Positions[1]) * 0.5f;
            }

            int IndexOf(int pointerId)
            {
                for (int i = 0; i < m_Count; i++)
                {
                    if (m_Ids[i] == pointerId)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        sealed class ItemInspect
        {
            const float MaxAngle = 14f;
            const float DegPerPixel = 0.08f;
            const float MinZoom = 1f;
            const float MaxZoom = 1.6f;
            const float ZoomStep = 0.08f;

            readonly VisualElement m_Viewport;
            readonly VisualElement m_Art;
            readonly IVisualElementScheduledItem m_Ticker;
            readonly PointerSet m_Pointers = new PointerSet();

            float m_MaxAngle = MaxAngle;
            float m_DegPerPixel = DegPerPixel;
            float m_MinZoom = MinZoom;
            float m_MaxZoom = MaxZoom;
            float m_ZoomStep = ZoomStep;
            float m_KeyStep = KeyStep;
            float m_Follow = Follow;

            float m_Angle;
            float m_TargetAngle;
            float m_Zoom = 1f;
            float m_TargetZoom = 1f;
            float m_GrabX;
            float m_GrabAngle;
            bool m_Pinching;
            float m_PinchSpan;

            public ItemInspect(VisualElement viewport, VisualElement art)
            {
                m_Viewport = viewport;
                m_Art = art;

                viewport.RegisterCallback<PointerDownEvent>(OnDown);
                viewport.RegisterCallback<PointerMoveEvent>(OnMove);
                viewport.RegisterCallback<PointerUpEvent>(OnUp);
                viewport.RegisterCallback<PointerCancelEvent>(OnCancel);
                viewport.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                viewport.RegisterCallback<WheelEvent>(OnWheel);
                viewport.RegisterCallback<NavigationMoveEvent>(OnNavigate);
                viewport.focusable = true;

                m_Ticker = viewport.schedule.Execute(Tick).Every(TickMs);
                m_Ticker.Pause();

                ReadStyle(viewport.customStyle);
                ClampToLimits();
                viewport.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }

            void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
            {
                ReadStyle(evt.customStyle);
                ClampToLimits();
            }

            void ReadStyle(ICustomStyle style)
            {
                m_MinZoom = Mathf.Max(0.01f, Read(style, s_ZoomMinProperty, MinZoom));
                m_MaxZoom = Mathf.Max(m_MinZoom, Read(style, s_ZoomMaxProperty, MaxZoom));
                m_ZoomStep = Mathf.Max(0f, Read(style, s_ZoomStepProperty, ZoomStep));
                m_KeyStep = Read(style, s_PanKeyStepProperty, KeyStep);
                m_MaxAngle = Mathf.Max(0f, Read(style, s_RotateMaxProperty, MaxAngle));
                m_DegPerPixel = Read(style, s_RotateSpeedProperty, DegPerPixel);
                m_Follow = Mathf.Clamp(Read(style, s_DragFollowProperty, Follow), 0.01f, 1f);
            }

            void ClampToLimits()
            {
                float angle = m_TargetAngle;
                float zoom = m_TargetZoom;

                m_TargetAngle = Mathf.Clamp(m_TargetAngle, -m_MaxAngle, m_MaxAngle);
                m_TargetZoom = Mathf.Clamp(m_TargetZoom, m_MinZoom, m_MaxZoom);

                if (m_TargetAngle != angle || m_TargetZoom != zoom)
                {
                    Wake(m_Ticker);
                }
            }

            void OnNavigate(NavigationMoveEvent evt)
            {
                float dx = 0f;
                if (evt.direction == NavigationMoveEvent.Direction.Left)
                {
                    dx = -m_KeyStep;
                }
                else if (evt.direction == NavigationMoveEvent.Direction.Right)
                {
                    dx = m_KeyStep;
                }

                if (dx == 0f)
                {
                    return;
                }

                m_TargetAngle = Mathf.Clamp(m_TargetAngle + dx * m_DegPerPixel, -m_MaxAngle, m_MaxAngle);
                Wake(m_Ticker);
                evt.StopPropagation();
                if (m_Viewport.focusController != null)
                {
                    m_Viewport.focusController.IgnoreEvent(evt);
                }
            }

            void OnDown(PointerDownEvent evt)
            {
                m_Viewport.CapturePointer(evt.pointerId);
                m_Pointers.Down(evt.pointerId, evt.position);

                if (m_Pointers.Count == 2)
                {
                    ArmPinch();
                }
                else if (m_Pointers.Count == 1)
                {
                    Grab(m_Pointers.PositionAt(0));
                }
            }

            void OnMove(PointerMoveEvent evt)
            {
                if (!m_Viewport.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                m_Pointers.Move(evt.pointerId, evt.position);

                if (m_Pointers.Count == 2)
                {
                    Pinch();
                    evt.StopPropagation();
                    return;
                }

                // Rotate only for one tracked pointer. Pinch and extra contacts suspend rotation.
                if (m_Pointers.Count != 1 || m_Pointers.IdAt(0) != evt.pointerId)
                {
                    evt.StopPropagation();
                    return;
                }

                float dx = evt.position.x - m_GrabX;
                m_TargetAngle = Mathf.Clamp(m_GrabAngle + dx * m_DegPerPixel, -m_MaxAngle, m_MaxAngle);
                Wake(m_Ticker);
                evt.StopPropagation();
            }

            void OnUp(PointerUpEvent evt)
            {
                if (!m_Viewport.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                m_Viewport.ReleasePointer(evt.pointerId);
                Lift(evt.pointerId);
                evt.StopPropagation();
            }

            // Cancellation must clear pointer state even when no PointerUpEvent arrives.
            void OnCancel(PointerCancelEvent evt)
            {
                if (m_Viewport.HasPointerCapture(evt.pointerId))
                {
                    m_Viewport.ReleasePointer(evt.pointerId);
                }

                Lift(evt.pointerId);
            }

            // Losing capture also clears pointer state.
            void OnCaptureOut(PointerCaptureOutEvent evt)
            {
                Lift(evt.pointerId);
            }

            void OnWheel(WheelEvent evt)
            {
                float next = Mathf.Clamp(m_TargetZoom + ZoomDelta(evt.delta.y, m_ZoomStep), m_MinZoom, m_MaxZoom);
                if (Mathf.Approximately(next, m_TargetZoom))
                {
                    return;
                }

                m_TargetZoom = next;
                Wake(m_Ticker);
                evt.StopPropagation();
            }

            // Taking the reference point from the pointer that is still down is what keeps
            // the one-finger gesture from jumping when the other finger of a pinch lifts.
            void Grab(Vector2 position)
            {
                m_GrabX = position.x;
                m_GrabAngle = m_TargetAngle;
            }

            void ArmPinch()
            {
                m_Pinching = false;
                m_PinchSpan = m_Pointers.Span();
            }

            // A two-pointer item gesture changes zoom only.
            void Pinch()
            {
                float span = m_Pointers.Span();
                if (span < PinchMinSpan)
                {
                    m_Pinching = false;
                    m_PinchSpan = span;
                    return;
                }

                if (!m_Pinching)
                {
                    if (Mathf.Abs(span - m_PinchSpan) < PinchSlop)
                    {
                        return;
                    }

                    // Re-base on the span that passed the slop, so the zoom starts where
                    // the fingers are instead of stepping by the slop.
                    m_Pinching = true;
                    m_PinchSpan = span;
                    return;
                }

                float ratio = Mathf.Clamp(span / m_PinchSpan, 1f / PinchMaxRatio, PinchMaxRatio);
                m_PinchSpan = span;

                // Scale about the middle of the art, which is where the wheel scales it:
                // one anchor for both inputs.
                float next = Mathf.Clamp(m_TargetZoom * ratio, m_MinZoom, m_MaxZoom);
                if (next == m_TargetZoom)
                {
                    return;
                }

                m_TargetZoom = next;
                Wake(m_Ticker);
            }

            void Lift(int pointerId)
            {
                if (!m_Pointers.Up(pointerId))
                {
                    return;
                }

                if (m_Pointers.Count == 2)
                {
                    ArmPinch();
                }
                else if (m_Pointers.Count == 1)
                {
                    m_Pinching = false;
                    Grab(m_Pointers.PositionAt(0));
                }
                else
                {
                    m_Pinching = false;
                }
            }

            void Tick(TimerState state)
            {
                m_Angle = Approach(m_Angle, m_TargetAngle, 0.02f, m_Follow);
                m_Zoom = Approach(m_Zoom, m_TargetZoom, 0.001f, m_Follow);

                m_Art.style.rotate = new Rotate(Angle.Degrees(m_Angle));
                m_Art.style.scale = new Scale(new Vector2(m_Zoom, m_Zoom));

                if (m_Angle == m_TargetAngle && m_Zoom == m_TargetZoom)
                {
                    m_Ticker.Pause();
                }
            }
        }

        sealed class MapPan
        {
            const float MinZoom = 1f;
            const float MaxZoom = 1.5f;
            const float ZoomStep = 0.07f;
            const float Overhang = 0.25f;

            readonly VisualElement m_Viewport;
            readonly VisualElement m_Board;
            readonly IVisualElementScheduledItem m_Ticker;
            readonly PointerSet m_Pointers = new PointerSet();

            float m_MinZoom = MinZoom;
            float m_MaxZoom = MaxZoom;
            float m_ZoomStep = ZoomStep;
            float m_Overhang = Overhang;
            float m_KeyStep = KeyStep;
            float m_Follow = Follow;

            float m_X;
            float m_Y;
            float m_TargetX;
            float m_TargetY;
            float m_Zoom = 1f;
            float m_TargetZoom = 1f;
            float m_GrabPointerX;
            float m_GrabPointerY;
            float m_GrabX;
            float m_GrabY;
            bool m_Pinching;
            float m_PinchSpan;
            Vector2 m_PinchMid;

            public MapPan(VisualElement viewport, VisualElement board)
            {
                m_Viewport = viewport;
                m_Board = board;

                viewport.RegisterCallback<PointerDownEvent>(OnDown);
                viewport.RegisterCallback<PointerMoveEvent>(OnMove);
                viewport.RegisterCallback<PointerUpEvent>(OnUp);
                viewport.RegisterCallback<PointerCancelEvent>(OnCancel);
                viewport.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                viewport.RegisterCallback<WheelEvent>(OnWheel);
                viewport.RegisterCallback<NavigationMoveEvent>(OnNavigate);
                viewport.focusable = true;

                m_Ticker = viewport.schedule.Execute(Tick).Every(TickMs);
                m_Ticker.Pause();

                ReadStyle(viewport.customStyle);
                ClampToLimits();
                viewport.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }

            void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
            {
                ReadStyle(evt.customStyle);
                ClampToLimits();
            }

            void ReadStyle(ICustomStyle style)
            {
                m_MinZoom = Mathf.Max(0.01f, Read(style, s_ZoomMinProperty, MinZoom));
                m_MaxZoom = Mathf.Max(m_MinZoom, Read(style, s_ZoomMaxProperty, MaxZoom));
                m_ZoomStep = Mathf.Max(0f, Read(style, s_ZoomStepProperty, ZoomStep));
                m_KeyStep = Read(style, s_PanKeyStepProperty, KeyStep);
                m_Overhang = Mathf.Max(0f, Read(style, s_PanOverhangProperty, Overhang));
                m_Follow = Mathf.Clamp(Read(style, s_DragFollowProperty, Follow), 0.01f, 1f);
            }

            // New limits reach the screen already on show, not only the next one.
            void ClampToLimits()
            {
                float x = m_TargetX;
                float y = m_TargetY;
                float zoom = m_TargetZoom;

                m_TargetZoom = Mathf.Clamp(m_TargetZoom, m_MinZoom, m_MaxZoom);
                ClampPan();

                if (m_TargetX != x || m_TargetY != y || m_TargetZoom != zoom)
                {
                    Wake(m_Ticker);
                }
            }

            void OnNavigate(NavigationMoveEvent evt)
            {
                float dx = 0f;
                float dy = 0f;
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Left: dx = m_KeyStep; break;
                    case NavigationMoveEvent.Direction.Right: dx = -m_KeyStep; break;
                    case NavigationMoveEvent.Direction.Up: dy = m_KeyStep; break;
                    case NavigationMoveEvent.Direction.Down: dy = -m_KeyStep; break;
                    default: return;
                }

                m_TargetX += dx;
                m_TargetY += dy;
                ClampPan();
                Wake(m_Ticker);
                evt.StopPropagation();
                if (m_Viewport.focusController != null)
                {
                    m_Viewport.focusController.IgnoreEvent(evt);
                }
            }

            void OnDown(PointerDownEvent evt)
            {
                m_Viewport.CapturePointer(evt.pointerId);
                m_Pointers.Down(evt.pointerId, evt.position);

                if (m_Pointers.Count == 2)
                {
                    ArmPinch();
                }
                else if (m_Pointers.Count == 1)
                {
                    Grab(m_Pointers.PositionAt(0));
                }
            }

            void OnMove(PointerMoveEvent evt)
            {
                if (!m_Viewport.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                m_Pointers.Move(evt.pointerId, evt.position);

                if (m_Pointers.Count == 2)
                {
                    Pinch();
                    evt.StopPropagation();
                    return;
                }

                // One pointer pans, and only the one that set the reference point. A third
                // contact suspends the gesture rather than dragging the board with it.
                if (m_Pointers.Count != 1 || m_Pointers.IdAt(0) != evt.pointerId)
                {
                    evt.StopPropagation();
                    return;
                }

                m_TargetX = m_GrabX + (evt.position.x - m_GrabPointerX);
                m_TargetY = m_GrabY + (evt.position.y - m_GrabPointerY);
                ClampPan();
                Wake(m_Ticker);
                evt.StopPropagation();
            }

            void OnUp(PointerUpEvent evt)
            {
                if (!m_Viewport.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                m_Viewport.ReleasePointer(evt.pointerId);
                Lift(evt.pointerId);
                evt.StopPropagation();
            }

            void OnCancel(PointerCancelEvent evt)
            {
                if (m_Viewport.HasPointerCapture(evt.pointerId))
                {
                    m_Viewport.ReleasePointer(evt.pointerId);
                }

                Lift(evt.pointerId);
            }

            void OnCaptureOut(PointerCaptureOutEvent evt)
            {
                Lift(evt.pointerId);
            }

            void OnWheel(WheelEvent evt)
            {
                float next = Mathf.Clamp(m_TargetZoom + ZoomDelta(evt.delta.y, m_ZoomStep), m_MinZoom, m_MaxZoom);
                if (Mathf.Approximately(next, m_TargetZoom))
                {
                    return;
                }

                m_TargetZoom = next;
                ClampPan();
                Wake(m_Ticker);
                evt.StopPropagation();
            }

            void Grab(Vector2 position)
            {
                m_GrabPointerX = position.x;
                m_GrabPointerY = position.y;
                m_GrabX = m_TargetX;
                m_GrabY = m_TargetY;
            }

            void ArmPinch()
            {
                m_Pinching = false;
                m_PinchSpan = m_Pointers.Span();
                m_PinchMid = m_Pointers.Mid();
            }

            void Pinch()
            {
                Vector2 mid = m_Pointers.Mid();
                float span = m_Pointers.Span();

                // Pan from the current midpoint so either pointer can lead the gesture.
                m_TargetX += mid.x - m_PinchMid.x;
                m_TargetY += mid.y - m_PinchMid.y;
                m_PinchMid = mid;

                if (span < PinchMinSpan)
                {
                    m_Pinching = false;
                    m_PinchSpan = span;
                }
                else if (!m_Pinching)
                {
                    if (Mathf.Abs(span - m_PinchSpan) >= PinchSlop)
                    {
                        m_Pinching = true;
                        m_PinchSpan = span;
                    }
                }
                else
                {
                    float ratio = Mathf.Clamp(span / m_PinchSpan, 1f / PinchMaxRatio, PinchMaxRatio);
                    m_PinchSpan = span;

                    // Zoom about the board center; midpoint movement above provides pan.
                    m_TargetZoom = Mathf.Clamp(m_TargetZoom * ratio, m_MinZoom, m_MaxZoom);
                }

                ClampPan();
                Wake(m_Ticker);
            }

            void Lift(int pointerId)
            {
                if (!m_Pointers.Up(pointerId))
                {
                    return;
                }

                if (m_Pointers.Count == 2)
                {
                    ArmPinch();
                }
                else if (m_Pointers.Count == 1)
                {
                    m_Pinching = false;
                    Grab(m_Pointers.PositionAt(0));
                }
                else
                {
                    m_Pinching = false;
                }
            }

            void ClampPan()
            {
                float width = m_Viewport.resolvedStyle.width;
                float height = m_Viewport.resolvedStyle.height;
                float grow = (m_TargetZoom - 1f) * 0.5f;

                if (width > 0f)
                {
                    // Max keeps the clamp upright when a zoom-min below 1 makes grow negative.
                    float limit = Mathf.Max(0f, width * (m_Overhang + grow));
                    m_TargetX = Mathf.Clamp(m_TargetX, -limit, limit);
                }

                if (height > 0f)
                {
                    float limit = Mathf.Max(0f, height * (m_Overhang + grow));
                    m_TargetY = Mathf.Clamp(m_TargetY, -limit, limit);
                }
            }

            void Tick(TimerState state)
            {
                m_X = Approach(m_X, m_TargetX, 0.2f, m_Follow);
                m_Y = Approach(m_Y, m_TargetY, 0.2f, m_Follow);
                m_Zoom = Approach(m_Zoom, m_TargetZoom, 0.001f, m_Follow);

                m_Board.style.translate = new Translate(m_X, m_Y);
                m_Board.style.scale = new Scale(new Vector2(m_Zoom, m_Zoom));

                if (m_X == m_TargetX && m_Y == m_TargetY && m_Zoom == m_TargetZoom)
                {
                    m_Ticker.Pause();
                }
            }
        }
    }
}
