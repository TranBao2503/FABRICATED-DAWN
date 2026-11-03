using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace FabricatedDawn
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class AdventureUI : MonoBehaviour
    {
        public AdventureDefinition content;
        public StyleSheet styleSheet;
        private AdventureSession session;
        private CheckpointStore saves;
        private VisualElement root, layout, stageHost, stage, inventory, modalLayer;
        private Label feedback, objective, roomTitle, selection;
        private bool inventoryVisible = true;
        private bool paused;
        private float previousTimeScale = 1;
        public AdventureSession Session => session;

        private void OnDisable()
        {
            if (paused) Time.timeScale = previousTimeScale;
            paused = false;
            root?.UnregisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || content == null) return;
            session = new AdventureSession(content);
            saves = new CheckpointStore(Path.Combine(Application.persistentDataPath, "toolkit-checkpoint.json"), content.contentId);
            Build();
            Refresh();
            SetFeedback("Quan sát căn phòng. Hãy thử nói chuyện, nhặt đồ và kiểm tra chiếc tủ.");
        }

        private void Build()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet)) root.styleSheets.Add(styleSheet);
            root.AddToClassList("app"); root.focusable = true;
            layout = Box(root, "layout");
            var header = Box(layout, "header");
            var brand = Box(header, "brand");
            Text(brand, "FABRICATED DAWN", "brand-title");
            Text(brand, "BỘ CÔNG CỤ PHÁT TRIỂN  /  01", "eyebrow");
            var actions = Box(header, "header-actions");
            ActionButton(actions, "Túi đồ", ToggleInventory, "subtle");
            ActionButton(actions, "Sổ manh mối", ShowJournal, "subtle");
            ActionButton(actions, "Tạm dừng", ShowPause, "subtle");
            var main = Box(layout, "main");
            var playArea = Box(main, "play-area");
            var caption = Box(playArea, "room-caption");
            roomTitle = Text(caption, "", "room-title");
            Text(caption, "CẢNH THỬ HỆ THỐNG · HÌNH TẠM", "badge");
            stageHost = Box(playArea, "stage-host");
            stage = Box(stageHost, "stage");
            stageHost.RegisterCallback<GeometryChangedEvent>(_ => FitStage());
            var side = new ScrollView(ScrollViewMode.Vertical);
            side.AddToClassList("sidebar");
            side.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            side.verticalScrollerVisibility = ScrollerVisibility.Auto;
            main.Add(side);
            Text(side, "ĐANG THỰC HIỆN", "eyebrow");
            objective = Text(side, "", "objective");
            Text(side, "CÁCH CHƠI", "eyebrow section-gap");
            Text(side, "01  Nhấn đồ vật để tương tác.\n\n02  Chọn vật phẩm trong túi, rồi nhấn mục tiêu.\n\n03  Manh mối đã đọc được giữ trong sổ.", "instructions");
            selection = Text(side, "", "selection");
            var saveActions = Box(side, "save-actions");
            ActionButton(saveActions, "Lưu checkpoint", Save, "primary");
            ActionButton(saveActions, "Tải checkpoint", Load, "subtle");
            Text(side, "Đây là nội dung thử toolkit.\nChưa phải Màn 1 chính thức.", "disclaimer");
            var status = Box(layout, "status");
            Text(status, "NAM", "speaker-tag");
            feedback = Text(status, "", "feedback");
            inventory = Box(layout, "inventory");
            var footer = Box(layout, "footer");
            Text(footer, "CHUỘT  tương tác     TAB  túi đồ     ESC  tạm dừng", "footer-note");
            Text(footer, "TOOLKIT 0.2  /  NỘI DUNG CẤU HÌNH BẰNG ASSET", "footer-note");
            root.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            root.schedule.Execute(() => root.Focus());
        }

        private void OnKey(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                if (session.ActiveDialogue != null) return;
                if (modalLayer != null) CloseModal(); else ShowPause();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Tab && modalLayer == null)
            {
                ToggleInventory(); root.focusController.IgnoreEvent(evt); evt.StopPropagation();
            }
        }

        private void Refresh()
        {
            var room = session.CurrentRoom;
            roomTitle.text = room.title;
            objective.text = session.Objective;
            stage.Clear(); stage.style.backgroundColor = room.placeholderColor;
            var background = session.Story.State.world == WorldPhase.Present ? room.presentBackground : room.pastBackground;
            if (background != null) stage.style.backgroundImage = new StyleBackground(background);
            else stage.style.backgroundImage = StyleKeyword.None;
            var trim = Box(stage, "wall-trim"); trim.pickingMode = PickingMode.Ignore;
            var floor = Box(stage, "floor"); floor.pickingMode = PickingMode.Ignore;
            Text(stage, room.subtitle, "stage-caption").pickingMode = PickingMode.Ignore;
            foreach (var h in room.hotspots)
            {
                if (!session.IsVisible(h)) continue;
                var local = h;
                var button = ActionButton(stage, h.label, () => Interact(local.id), "hotspot " + h.visualClass);
                button.name = h.id; button.tooltip = h.label;
                button.style.left = Length.Percent(h.bounds.x * 100);
                button.style.top = Length.Percent(h.bounds.y * 100);
                button.style.width = Length.Percent(h.bounds.width * 100);
                button.style.height = Length.Percent(h.bounds.height * 100);
                button.RegisterCallback<MouseEnterEvent>(_ => button.EnableInClassList("using-item", !string.IsNullOrEmpty(session.SelectedItemId)));
            }
            inventory.Clear();
            var heading = Box(inventory, "inventory-heading");
            Text(heading, "TÚI ĐỒ", "eyebrow");
            Text(heading, "Chọn một vật phẩm để sử dụng · nhấn lại để bỏ chọn", "muted");
            var slots = Box(inventory, "slots");
            if (session.Story.State.inventory.Count == 0) Text(slots, "Túi đang trống. Vật phẩm nhặt được sẽ xuất hiện tại đây.", "empty-inventory");
            foreach (var item in session.Story.State.inventory)
            {
                var local = item;
                var definition = content.Item(item.id);
                var button = ActionButton(slots, definition != null ? definition.displayName : item.id,
                    () => { session.SelectItem(local.id); Refresh(); }, "item-slot");
                button.name = "inventory-" + item.id;
                button.EnableInClassList("selected", session.SelectedItemId == item.id);
                if (definition != null && definition.icon != null)
                {
                    var icon = new VisualElement(); icon.AddToClassList("item-icon");
                    icon.style.backgroundImage = new StyleBackground(definition.icon); button.Insert(0, icon);
                }
            }
            inventory.style.display = inventoryVisible ? DisplayStyle.Flex : DisplayStyle.None;
            var selected = content.Item(session.SelectedItemId);
            selection.text = selected != null ? "ĐANG CHỌN\n" + selected.displayName : "Chưa chọn vật phẩm";
            FitStage();
        }

        private void FitStage()
        {
            float width = stageHost.contentRect.width, height = stageHost.contentRect.height;
            if (width <= 0 || height <= 0) return;
            float fitted = Mathf.Min(width, height * 16f / 9f);
            stage.style.width = fitted; stage.style.height = fitted * 9f / 16f;
        }

        public void Interact(string id)
        {
            if (modalLayer != null) return;
            var result = session.Interact(id);
            SetFeedback(result.message);
            Refresh();
            if (result.checkpoint) Save(false);
            if (session.ActiveDialogue != null) ShowDialogue();
            else if (!string.IsNullOrEmpty(result.clueId)) ShowClue(result.clueId);
        }

        private void SetFeedback(string text) => feedback.text = text;
        private void ToggleInventory() { if (modalLayer != null) return; inventoryVisible = !inventoryVisible; Refresh(); }

        private void ShowDialogue()
        {
            var dialogue = session.ActiveDialogue;
            if (dialogue == null) return;
            var card = OpenModal(dialogue.speaker, false);
            Text(card, dialogue.lines[session.DialogueLine], "modal-body");
            var row = Box(card, "modal-actions");
            Text(row, $"{session.DialogueLine + 1} / {dialogue.lines.Length}", "muted");
            ActionButton(row, session.DialogueLine + 1 == dialogue.lines.Length ? "Kết thúc" : "Tiếp tục", () =>
            {
                session.AdvanceDialogue(); CloseModal();
                if (session.ActiveDialogue != null) ShowDialogue();
            }, "primary");
        }

        private void ShowClue(string id)
        {
            var clue = content.Clue(id);
            if (clue == null) return;
            var card = OpenModal(clue.title);
            Text(card, clue.body, "modal-body");
            Text(card, "Đã ghi vào sổ manh mối. Bạn có thể đọc lại bất cứ lúc nào.", "muted");
        }

        private void ShowJournal()
        {
            if (modalLayer != null) return;
            var card = OpenModal("Sổ manh mối");
            if (session.Story.State.clues.Count == 0) Text(card, "Chưa ghi nhận manh mối nào.", "modal-body");
            var list = new ScrollView(); list.AddToClassList("journal-list"); card.Add(list);
            foreach (var id in session.Story.State.clues)
            {
                var clue = content.Clue(id); if (clue == null) continue;
                Text(list, clue.title, "journal-title"); Text(list, clue.body, "journal-body");
            }
        }

        private void ShowPause()
        {
            if (modalLayer != null) return;
            var card = OpenModal("Tạm dừng");
            PauseClock();
            Text(card, "Tiến độ và vật phẩm được lưu cùng nhau tại checkpoint.", "modal-body");
            ActionButton(card, "Lưu checkpoint", Save, "primary");
            ActionButton(card, "Tải checkpoint", () => { CloseModal(); Load(); }, "subtle");
            ActionButton(card, "Khôi phục bản lưu dự phòng", () =>
            {
                try { saves.LoadBackup(session); CloseModal(); Refresh(); SetFeedback("Đã phục hồi bản lưu dự phòng."); }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
                { SetFeedback("Không thể tải bản dự phòng: " + ex.Message); }
            }, "subtle");
            ActionButton(card, "Bắt đầu lại cảnh thử…", () =>
            {
                var confirm = OpenModal("Bắt đầu lại cảnh thử?");
                PauseClock();
                Text(confirm, "Tiến độ đang chơi sẽ đặt lại. Checkpoint trên đĩa vẫn được giữ cho tới lần lưu tiếp theo.", "modal-body");
                ActionButton(confirm, "Bắt đầu lại", () =>
                { session = new AdventureSession(content); CloseModal(); Refresh(); SetFeedback("Đã bắt đầu lại cảnh thử."); }, "primary");
            }, "subtle");
        }

        private void Save() => Save(true);
        private void Save(bool showFeedback)
        {
            try { saves.Save(session); if (showFeedback) SetFeedback("Đã lưu checkpoint."); }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            { SetFeedback("Chưa lưu được checkpoint: " + ex.Message); }
        }

        private void Load()
        {
            try { saves.Load(session); Refresh(); SetFeedback("Đã tải toàn bộ trạng thái tại checkpoint."); }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            { SetFeedback("Không thể tải checkpoint: " + ex.Message); }
        }

        private VisualElement OpenModal(string title, bool closeButton = true)
        {
            CloseModal(); modalLayer = Box(root, "modal-layer");
            var card = Box(modalLayer, "modal-card");
            var heading = Box(card, "modal-heading"); Text(heading, title, "modal-title");
            if (closeButton) ActionButton(heading, "Đóng", CloseModal, "subtle");
            return card;
        }

        private void PauseClock()
        {
            if (paused) return;
            previousTimeScale = Time.timeScale; paused = true; Time.timeScale = 0;
        }
        private void CloseModal()
        {
            modalLayer?.RemoveFromHierarchy(); modalLayer = null;
            if (paused) Time.timeScale = previousTimeScale;
            paused = false; root.Focus();
        }
        private static VisualElement Box(VisualElement parent, string classes)
        { var element = new VisualElement(); Classes(element, classes); parent.Add(element); return element; }
        private static Label Text(VisualElement parent, string text, string classes)
        { var element = new Label(text); Classes(element, classes); parent.Add(element); return element; }
        private static Button ActionButton(VisualElement parent, string text, Action action, string classes)
        { var button = new Button(action) { text = text }; Classes(button, classes); parent.Add(button); return button; }
        private static void Classes(VisualElement element, string names)
        { foreach (var name in names.Split(' ')) if (name.Length > 0) element.AddToClassList(name); }
    }
}
