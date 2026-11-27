using System;
using System.IO;
using FabricatedDawn;
using HorrorCaseFile.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace FabricatedDawn.CaseFile
{
    public enum CaseFileScreen { MainMenu, Hud, Inventory, Journal, Pause, Ending }

    [RequireComponent(typeof(UIDocument))]
    public sealed class CaseFileAdventureUI : MonoBehaviour
    {
        public AdventureDefinition content;
        public VisualTreeAsset mainMenu, hud, inventory, journal, pause;
        public StyleSheet overrides;
        public Texture2D keyIcon;
        private AdventureSession session;
        private CheckpointStore saves;
        private VisualElement root, canvas, screen, overlay;
        private string feedback = "Khám phá căn phòng và thử tương tác với đồ vật.";
        private string inspectedItem;
        private bool paused;
        private float previousTimeScale = 1;
        private Font bodyFont;
        public CaseFileScreen CurrentScreen { get; private set; }
        public AdventureSession Session => session;

        private void OnEnable()
        {
            if (!Application.isPlaying || content == null) return;
            session = new AdventureSession(content);
            saves = new CheckpointStore(Path.Combine(Application.persistentDataPath, content.isStoryDemo ? Uri.EscapeDataString(content.contentId) + "-checkpoint.json" : "toolkit-checkpoint.json"), content.contentId);
            bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear(); root.style.backgroundColor = new Color(.025f, .02f, .02f); root.focusable = true;
            root.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            root.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            Show(CaseFileScreen.MainMenu);
        }

        private void OnDisable()
        {
            SetPaused(false);
            root?.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            root?.UnregisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
        }

        public void Show(CaseFileScreen page)
        {
            if (session.Story.State.ending != StoryEnding.None && page != CaseFileScreen.MainMenu) page = CaseFileScreen.Ending;
            CloseOverlay(); SetPaused(page == CaseFileScreen.Pause || page == CaseFileScreen.Ending);
            CurrentScreen = page; canvas?.RemoveFromHierarchy();
            canvas = new VisualElement { name = "fd-casefile-canvas" };
            canvas.style.position = Position.Absolute; canvas.style.width = 1920; canvas.style.height = 1080;
            canvas.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0), 0);
            root.Add(canvas);
            Template(page).CloneTree(canvas);
            screen = canvas.Q<VisualElement>("screen");
            if (screen == null) throw new InvalidOperationException("Case File template has no screen root.");
            screen.AddToClassList("fd-ui");
            if (overrides != null) screen.styleSheets.Add(overrides);
            if (content.contentId.StartsWith("fabricated-dawn-rush-four")) screen.AddToClassList("fd-rush");
            // Film grain is a visual option, not part of the game state or clue system.
            Hide("grain");
            switch (page)
            {
                case CaseFileScreen.MainMenu: BuildMainMenu(); break;
                case CaseFileScreen.Hud: BuildHud(); break;
                case CaseFileScreen.Inventory: BuildInventory(); break;
                case CaseFileScreen.Journal: BuildJournal(); break;
                case CaseFileScreen.Pause: BuildPause(); break;
                case CaseFileScreen.Ending: BuildEnding(); break;
            }
            HcfScreenInteractions.Wire(screen);
            Fit(); root.schedule.Execute(() => root.Focus());
        }

        private VisualTreeAsset Template(CaseFileScreen page)
        {
            switch (page)
            {
                case CaseFileScreen.MainMenu: return mainMenu;
                case CaseFileScreen.Inventory: return inventory;
                case CaseFileScreen.Journal: return journal;
                case CaseFileScreen.Pause: return pause;
                default: return hud;
            }
        }

        private void OnGeometry(GeometryChangedEvent evt) => Fit();
        private void Fit()
        {
            if (canvas == null || root.contentRect.width <= 0 || root.contentRect.height <= 0) return;
            float scale = Mathf.Min(root.contentRect.width / 1920f, root.contentRect.height / 1080f);
            canvas.style.scale = new Scale(new Vector3(scale, scale, 1));
            canvas.style.left = (root.contentRect.width - 1920 * scale) * .5f;
            canvas.style.top = (root.contentRect.height - 1080 * scale) * .5f;
        }

        private void OnKey(KeyDownEvent evt)
        {
            if (CurrentScreen == CaseFileScreen.Ending) return;
            if (evt.keyCode == KeyCode.Escape)
            {
                if (overlay != null && session.ActiveDialogue == null) CloseOverlay();
                else if (overlay == null && CurrentScreen != CaseFileScreen.MainMenu)
                    Show(CurrentScreen == CaseFileScreen.Hud ? CaseFileScreen.Pause : CaseFileScreen.Hud);
                evt.StopPropagation(); root.focusController.IgnoreEvent(evt);
            }
            else if (evt.keyCode == KeyCode.Tab && overlay == null && CurrentScreen != CaseFileScreen.MainMenu)
            {
                Show(CurrentScreen == CaseFileScreen.Inventory ? CaseFileScreen.Hud : CaseFileScreen.Inventory);
                evt.StopPropagation(); root.focusController.IgnoreEvent(evt);
            }
        }

        private void BuildMainMenu()
        {
            var lines = screen.Query<Label>(className: "s01-title__line").ToList();
            for (int i = 0; i < lines.Count; i++) lines[i].text = i % 2 == 0 ? "FABRICATED" : "DAWN";
            SetClass("s01-header__code", "FABRICATED DAWN / 4KOQ");
            SetClass("s01-header__date", content.isStoryDemo ? "Demo cốt truyện · Hà Nội 2004" : "Bản thử hệ thống");
            if (content.contentId.StartsWith("fabricated-dawn-rush-four")) {
                var preview = screen.Q<VisualElement>("key-art");
                var picture = content.Room("M1_LIVING")?.presentBackground;
                if (preview != null && picture != null) preview.style.backgroundImage = new StyleBackground(picture);
                HideClass("s01-keyart__hatch"); HideClass("s01-keyart__vignette");
            }
            SetClass("s01-fig", content.contentId.StartsWith("fabricated-dawn-rush-four") ? "Nhà trọ · Hà Nội, 2004" : "Hình minh họa đi kèm gói UI · chưa phải bối cảnh chính thức");
            SetClass("s01-version", content.isStoryDemo ? content.contentId.StartsWith("fabricated-dawn-chapter-one") ? "MÀN 1 · NHỮNG NGÀY Ở TRỌ / DEMO 8 MÀN" : "PROLOGUE / DEMO 8 MÀN / ĐANG PHÁT TRIỂN" : "TOOLKIT 0.2 / CASE FILE UI");
            HideClass("s01-menu-item__meta"); HideClass("s01-hints");
            Menu(1, "TIẾP TỤC", () => { if (Load()) Show(CaseFileScreen.Hud); });
            screen.Q<Button>("menu-item-01").SetEnabled(saves.Exists);
            if (!saves.Exists)
            {
                screen.Q<Button>("menu-item-01").RemoveFromClassList("is-active");
                screen.Q<Button>("menu-item-02").AddToClassList("is-active");
            }
            Menu(2, "BẮT ĐẦU", () => { session = new AdventureSession(content); Show(CaseFileScreen.Hud); });
            Menu(3, "HƯỚNG DẪN", () => Message("Cách chơi", content.isStoryDemo ? "Khám phá nhà, nhặt đồ và đọc manh mối. Học đường tới phòng thờ trước khi chơi Vỗ Vai Ma.\n\nTrong đoạn thoát thân, chọn sai đường dẫn tới BAD END. Tới phòng thờ là mốc hoàn thành Màn 2. Demo dự kiến đi hết tám màn, kết thúc thường ở Màn 8.\n\nTAB: túi đồ. ESC: tạm dừng. Có thể tải checkpoint để thử lại. Màn 1: quan sát đồ vật, chọn chìa trong túi để mở tủ. Các tiếng động có phụ đề; một số hình và âm thanh đang được bổ sung." : "Nhấn đồ vật để tương tác. Chọn vật phẩm trong túi rồi nhấn mục tiêu.\n\nTAB: túi đồ. ESC: tạm dừng. Checkpoint lưu toàn bộ tiến độ.\n\nĐây là cảnh thử toolkit, chưa phải Màn 1 chính thức."));
            Menu(4, "VỀ BẢN THỬ", () => Message("Fabricated Dawn", "Giao diện sử dụng Horror GUI Kit – Case File UI Toolkit.\n\nInventory, manh mối, tương tác và checkpoint được nối với hệ thống của Fabricated Dawn. Màn 1 được dựng theo mục 5.2 của tài liệu. Hội thoại triển khai và hình sinh bổ sung đang chờ nhóm duyệt."));
            if (content.contentId.StartsWith("fabricated-dawn-rush-four")) {
                SetClass("s01-version", "BẢN CHƠI MÀN 1–4 · NHÂN VẬT TẠM · KHÔNG ÂM THANH");
                Menu(4, "VỀ BẢN THỬ", () => Message("Fabricated Dawn", "Hà Nội, năm 2004. Bốn người bạn thuê chung một căn nhà để tiện đi học. Những điều lạ dần len vào cuộc sống thường ngày.\n\nBản chơi hiện tại kết thúc ở cuối Màn 4. Câu chuyện dự kiến kéo dài tám màn; hình và thoại vẫn đang được hoàn thiện."));
                Menu(3, "CÁCH CHƠI", () => Message("Cách chơi", "Nhấn đồ vật và nhân vật để quan sát hoặc nói chuyện. Chọn vật trong túi rồi nhấn nơi cần dùng.\n\nSổ giữ các lời kể và dấu vết. Khi đối chiếu, chọn những điều đã có chứng cứ. Màn 4: tìm dấu mốc không đổi, đừng chỉ tin số phòng.\n\nTab: túi đồ. Esc: tạm dừng. Bản này chơi liên tục đến hết Màn 4; phần sau tiếp tục được phát triển."));
            }
            Hide("menu-item-05");
        }

        private void BuildHud()
        {
            Hide("vitals"); Hide("ammo"); Hide("torch"); Hide("crosshair"); Hide("notifications");
            Hide("hints"); Hide("gear"); Hide("vignette");
            SetClass("s07-objective__kicker", session.CurrentRoom.title.ToUpperInvariant());
            Set("objective-title", session.Objective);
            var stage = screen.Q<VisualElement>("gameplay-view");
            stage.RemoveFromClassList("hcf-placeholder"); stage.AddToClassList("fd-room");
            stage.style.backgroundImage = StyleKeyword.None;
            stage.style.backgroundColor = session.CurrentRoom.placeholderColor;
            var background = session.Story.State.world == WorldPhase.Present ? session.CurrentRoom.presentBackground : session.CurrentRoom.pastBackground;
            if (background != null) stage.style.backgroundImage = new StyleBackground(background);
            if (session.CurrentRoom.nightScene) {
                var shade = new VisualElement { pickingMode = PickingMode.Ignore };
                shade.AddToClassList("fd-night-shade"); stage.Add(shade);
            }
            var floor = new VisualElement(); floor.AddToClassList("fd-floor"); stage.Add(floor);
            foreach (var actor in session.CurrentRoom.hotspots)
            {
                if (!session.IsVisible(actor) || (actor.artwork == null && !actor.stickFigure) || actor.artworkBounds.width <= 0) continue;
                VisualElement image = actor.stickFigure ? new StickFigureElement(actor.characterTag) : new VisualElement();
                image.pickingMode = PickingMode.Ignore;
                image.AddToClassList("fd-scene-image");
                if (!actor.stickFigure) image.style.backgroundImage = new StyleBackground(actor.artwork);
                image.style.left = Length.Percent(actor.artworkBounds.x * 100); image.style.top = Length.Percent(actor.artworkBounds.y * 100);
                image.style.width = Length.Percent(actor.artworkBounds.width * 100); image.style.height = Length.Percent(actor.artworkBounds.height * 100);
                stage.Add(image);
            }
            foreach (var h in session.CurrentRoom.hotspots)
            {
                if (!session.IsVisible(h)) continue;
                var local = h; var button = new Button(() => Interact(local.id)) { name = h.id, text = h.label };
                button.AddToClassList("fd-hotspot"); button.AddToClassList("fd-" + h.visualClass);
                button.style.left = Length.Percent(h.bounds.x * 100); button.style.top = Length.Percent(h.bounds.y * 100);
                button.style.width = Length.Percent(h.bounds.width * 100); button.style.height = Length.Percent(h.bounds.height * 100);
                var artwork = h.artwork;
                if (artwork == null && h.kind == InteractionKind.Pickup && h.item != null) artwork = h.item.icon;
                if (h.artworkBounds.width > 0)
                {
                    artwork = null; button.text = "";
                    Text(button, h.label, "fd-scene-label");
                }
                if (h.sceneHotspot)
                {
                    button.tooltip = h.label;
                    if (h.kind == InteractionKind.Travel)
                    {
                        button.text = "";
                        Text(button, h.label, "fd-exit-label");
                        button.AddToClassList("fd-scene-travel");
                    }
                    button.AddToClassList("fd-scene-hotspot");
                    if (h.kind != InteractionKind.Travel) button.AddToClassList("fd-scene-object");
                    if (artwork != null)
                    {
                        button.text = "";
                        AddArtwork(button, artwork, "fd-scene-image");
                        Text(button, h.label, "fd-scene-label");
                    }
                }
                else if (artwork != null)
                {
                    button.text = "";
                    button.AddToClassList("fd-hotspot-with-art");
                    AddArtwork(button, artwork, "fd-hotspot-image");
                    Text(button, h.label, "fd-hotspot-caption");
                }
                Vietnamese(button); stage.Add(button);
            }
            if (!string.IsNullOrWhiteSpace(session.CurrentRoom.narration))
            {
                var narration = Text(stage, session.CurrentRoom.narration, "fd-room-narration");
                narration.pickingMode = PickingMode.Ignore;
                narration.EnableInClassList("fd-title-card", session.CurrentRoom.isTitleCard);
            }
            floor.style.display = session.CurrentRoom.isTitleCard || background != null ? DisplayStyle.None : DisplayStyle.Flex;
            if (session.CurrentRoom.introCard) {
                stage.AddToClassList("fd-intro-scene");
                return;
            }
            Navigation();
            var tray = new VisualElement { pickingMode = PickingMode.Ignore }; tray.AddToClassList("fd-quick-tray"); screen.Add(tray);
            Text(tray, session.SelectedItemId == null ? "VẬT PHẨM" : "ĐANG CHỌN: " + content.Item(session.SelectedItemId)?.displayName, "fd-caption");
            var quickScroll = new ScrollView(ScrollViewMode.Horizontal);
            quickScroll.AddToClassList("fd-quick-scroll");
            quickScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            quickScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            quickScroll.pickingMode = PickingMode.Ignore;
            quickScroll.contentContainer.pickingMode = PickingMode.Ignore;
            quickScroll.contentViewport.pickingMode = PickingMode.Ignore;
            // Empty scroll-view containers must let scene clicks through. Item
            // buttons are added afterward and keep their normal picking mode.
            quickScroll.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
            quickScroll.horizontalScroller.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Position);
            tray.Add(quickScroll);
            var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.AddToClassList("fd-row"); quickScroll.Add(row);
            foreach (var entry in session.Story.State.inventory)
            {
                var local = entry; var definition = content.Item(entry.id);
                var button = Button(row, definition.displayName, () => { session.SelectItem(local.id); Show(CaseFileScreen.Hud); }, "fd-quick");
                if (definition.icon != null)
                {
                    button.text = "";
                    button.AddToClassList("fd-quick-with-art");
                    AddArtwork(button, definition.icon, "fd-quick-image");
                    Text(button, definition.displayName, "fd-quick-name");
                }
                button.EnableInClassList("is-selected", session.SelectedItemId == entry.id);
            }
            Text(screen, feedback, "fd-feedback");
            Text(screen, content.isStoryDemo ? session.CurrentRoom.subtitle + (background == null ? " · NỀN TẠM" : "") : "CẢNH THỬ TOOLKIT · ASSET VAULT", "fd-room-note");
        }

        public void Interact(string id)
        {
            if (overlay != null || CurrentScreen != CaseFileScreen.Hud) return;
            var result = session.Interact(id); feedback = result.message;
            Show(CaseFileScreen.Hud);
            if (result.checkpoint) Save(false);
            if (session.ActiveDialogue != null) Dialogue();
            else if (result.succeeded && id == "M4_OPEN_DOOR")
            {
                var card = Card("Một bước qua cửa");
                overlay.AddToClassList("fd-door-transition");
                Text(card, "Cả nhóm bước ra.\n\nNhưng dưới chân lại là nền gạch trước cửa nhà.\n\nChỉ trong một thoáng, mọi căn phòng và đồ đạc đã đổi. Phía sau lưng, lối ra khép lại.", "fd-dialogue-body");
                Button(card, "Nhìn lại căn nhà ›", CloseOverlay, "fd-nav-button");
            }
            else if (!string.IsNullOrEmpty(result.clueId))
            {
                var clue = content.Clue(result.clueId); Message(clue.title, clue.body, result.examinedArtwork);
            }
        }

        private void BuildInventory()
        {
            SetClass("s11-title", "TÚI ĐỒ"); Set("inv-meta", session.Story.State.inventory.Count + " vật phẩm");
            HideClass("s11-hints"); Hide("stats"); Hide("btn-combine"); Hide("btn-drop");
            var grid = screen.Q<VisualElement>("grid"); grid.Clear(); grid.pickingMode = PickingMode.Position;
            grid.style.height = 650;
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1;
            var cells = new VisualElement(); cells.AddToClassList("fd-inventory-grid"); scroll.Add(cells); grid.Add(scroll);
            foreach (var entry in session.Story.State.inventory)
            {
                var local = entry; var item = content.Item(entry.id);
                var button = new Button(() => { inspectedItem = local.id; UpdateInventoryDetail(); }) { name = "fd-item-" + entry.id };
                button.AddToClassList("s11-cell"); button.AddToClassList("s11-cell--full"); button.AddToClassList("fd-item");
                var image = new VisualElement(); image.AddToClassList("fd-item-image");
                if (item.icon != null) image.style.backgroundImage = new StyleBackground(item.icon);
                else if (item.id == "ITEM_SANDBOX_KEY" && keyIcon != null) image.style.backgroundImage = new StyleBackground(keyIcon);
                button.Add(image); Text(button, item.displayName, "fd-item-name"); cells.Add(button);
            }
            if (session.Story.State.inventory.Count == 0) Text(cells, "Túi đang trống.\nVật phẩm nhặt được sẽ xuất hiện ở đây.", "fd-empty");
            if (!session.Story.State.inventory.Exists(x => x.id == inspectedItem))
                inspectedItem = session.Story.State.inventory.Count > 0 ? session.Story.State.inventory[0].id : null;
            BindButton("btn-equip", "CHỌN DÙNG", () => { if (inspectedItem != null) session.SelectItem(inspectedItem); Show(CaseFileScreen.Hud); });
            BindButton("btn-examine", "XEM KỸ", () => { var item = content.Item(inspectedItem); if (item != null) Message(item.displayName, item.purpose); });
            Navigation(true); UpdateInventoryDetail();
        }

        private void UpdateInventoryDetail()
        {
            var item = content.Item(inspectedItem);
            Set("detail-kicker", item == null ? "CHƯA CHỌN VẬT PHẨM" : "VẬT PHẨM ĐANG XEM");
            Set("detail-title", item?.displayName ?? "Túi trống");
            Set("detail-body", item?.purpose ?? "Hãy khám phá căn phòng để tìm vật phẩm.");
            var preview = screen.Q<VisualElement>("fd-item-preview");
            if (preview == null)
            {
                preview = new VisualElement { name = "fd-item-preview", pickingMode = PickingMode.Ignore };
                preview.AddToClassList("fd-detail-image");
                var body = screen.Q<Label>("detail-body");
                body.parent.Insert(body.parent.IndexOf(body), preview);
            }
            preview.style.backgroundImage = item != null && item.icon != null
                ? new StyleBackground(item.icon) : new StyleBackground(StyleKeyword.None);
            preview.style.display = item != null && item.icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            screen.Q<Button>("btn-equip").SetEnabled(item != null);
            screen.Q<Button>("btn-examine").SetEnabled(item != null);
            screen.Query<Button>(className: "fd-item").ForEach(b => b.EnableInClassList("is-active", b.name == "fd-item-" + inspectedItem));
        }

        private void BuildJournal()
        {
            SetClass("s13-head__title", "SỔ MANH MỐI"); SetClass("s13-head__count", session.Story.State.clues.Count + " ĐÃ GHI");
            Hide("hints"); HideClass("s13-page__meta"); HideClass("s13-page__sign"); HideClass("s13-page__stamp");
            Set("page-form", "FABRICATED DAWN / GHI CHÉP"); Hide("page-title-2");
            var list = screen.Q<VisualElement>("doc-list"); list.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.height = 600; list.Add(scroll);
            foreach (var id in session.Story.State.clues)
            {
                var clue = content.Clue(id); if (clue == null) continue;
                var button = Button(scroll, clue.title, () => JournalPage(clue), "fd-document-button"); button.name = "fd-clue-" + id;
            }
            if (session.Story.State.clues.Count == 0)
            {
                Text(scroll, "Chưa có manh mối.\nCác tài liệu đã đọc sẽ được giữ trong sổ.", "fd-empty");
                Set("page-title-1", "Chưa có ghi chép"); screen.Q<ScrollView>("page-scroll").Clear();
            }
            else JournalPage(content.Clue(session.Story.State.clues[0]));
            Navigation(true);
        }

        private void JournalPage(ClueDefinition clue)
        {
            Set("page-title-1", clue.title); Set("journal-page-num", "ĐÃ LƯU TRONG SỔ");
            var page = screen.Q<ScrollView>("page-scroll"); page.Clear();
            Text(page, clue.body, "fd-journal-body");
            screen.Query<Label>(className: "s13-page__foot-item").ForEach(l => { if (l.name != "journal-page-num") l.text = "CUỘN ĐỂ ĐỌC"; });
        }

        private void BuildEnding()
        {
            screen.Clear();
            var card = new VisualElement(); card.AddToClassList("fd-ending-card"); screen.Add(card);
            bool failed = session.Story.State.ending == StoryEnding.Bad;
            Text(card, failed ? "BAD END" : "KẾT THÚC THƯỜNG", "fd-ending-title");
            Text(card, session.CurrentRoom.title, "fd-dialogue-title");
            Text(card, session.CurrentRoom.description, "fd-dialogue-body");
            Text(card, "FABRICATED DAWN · DEMO 8 MÀN", "fd-caption");
            var retry = Button(card, "THỬ LẠI TỪ CHECKPOINT", () => { if (Load()) Show(CaseFileScreen.Hud); }, "fd-nav-button");
            retry.SetEnabled(saves.Exists);
            Button(card, "CHƠI LẠI TỪ ĐẦU", () => { session = new AdventureSession(content); feedback = "Bắt đầu hành trình của Nam."; Show(CaseFileScreen.Hud); }, "fd-nav-button");
            Button(card, "VỀ MENU CHÍNH", () => Show(CaseFileScreen.MainMenu), "fd-nav-button");
        }

        private void BuildPause()
        {
            var room = session.CurrentRoom;
            var background = session.Story.State.world == WorldPhase.Present ? room.presentBackground : room.pastBackground;
            var frozen = screen.Q<VisualElement>("frozen-view");
            frozen.style.backgroundImage = background != null ? new StyleBackground(background) : new StyleBackground(StyleKeyword.None);
            frozen.style.backgroundColor = room.placeholderColor;
            var preview = screen.Q<VisualElement>(className: "s10-still-art");
            if (preview != null)
            {
                preview.style.backgroundImage = background != null ? new StyleBackground(background) : new StyleBackground(StyleKeyword.None);
                preview.style.backgroundColor = room.placeholderColor;
                if (background == null) Text(preview, room.title + "\nHình tạm", "fd-preview-label");
            }
            SetClass("s10-title-line", "TẠM DỪNG"); SetClass("s10-file", "FABRICATED DAWN / TẠM DỪNG");
            SetClass("s10-elapsed", "Tiến độ đang được giữ nguyên");
            SetClass("s10-note", "Thời gian game đang dừng."); HideClass("s10-hints");
            var values = screen.Query<Label>(className: "s10-meta-value").ToList();
            string[] text = { session.CurrentRoom.title, "CẢNH THỬ", saves.Exists ? "ĐÃ CÓ CHECKPOINT" : "CHƯA LƯU" };
            for (int i = 0; i < values.Count && i < text.Length; i++) { values[i].text = text[i]; Vietnamese(values[i]); }
            var keys = screen.Query<Label>(className: "s10-meta-key").ToList();
            string[] labels = { "VỊ TRÍ", "CHẾ ĐỘ", "CHECKPOINT" };
            for (int i = 0; i < keys.Count && i < labels.Length; i++) { keys[i].text = labels[i]; Vietnamese(keys[i]); }
            Menu(1, "TIẾP TỤC", () => Show(CaseFileScreen.Hud));
            Menu(2, "LƯU CHECKPOINT", () => Save(true));
            Menu(3, "TẢI CHECKPOINT", () => { if (Load()) Show(CaseFileScreen.Hud); });
            Menu(4, "SỔ MANH MỐI", () => Show(CaseFileScreen.Journal));
            Menu(5, "VỀ MENU CHÍNH", () => Show(CaseFileScreen.MainMenu));
            Navigation(true);
            Button(screen.Q<VisualElement>(className: "fd-nav"), "Tải bản dự phòng", () =>
            {
                try { saves.LoadBackup(session); feedback = "Đã phục hồi checkpoint dự phòng."; Show(CaseFileScreen.Hud); }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
                { Message("Chưa tải được", ex.Message); }
            }, "fd-nav-button");
        }

        private void Navigation(bool close = false)
        {
            var nav = new VisualElement(); nav.AddToClassList("fd-nav"); screen.Add(nav);
            if (close) Button(nav, "Quay lại", () => Show(CaseFileScreen.Hud), "fd-nav-button");
            else
            {
                Button(nav, "Túi đồ", () => Show(CaseFileScreen.Inventory), "fd-nav-button");
                Button(nav, "Sổ manh mối", () => Show(CaseFileScreen.Journal), "fd-nav-button");
                Button(nav, "Tạm dừng", () => Show(CaseFileScreen.Pause), "fd-nav-button");
            }
        }

        private void Dialogue()
        {
            var dialogue = session.ActiveDialogue;
            var card = Card(dialogue.speaker);
            overlay.AddToClassList("fd-talk-overlay"); card.AddToClassList("fd-talk-card");
            Text(card, (session.DialogueLine + 1) + " / " + dialogue.lines.Length, "fd-talk-progress");
            Text(card, dialogue.lines[session.DialogueLine], "fd-dialogue-body");
            Button(card, session.DialogueLine + 1 == dialogue.lines.Length ? "Khép lại câu chuyện" : "Nói tiếp ›", () =>
            { session.AdvanceDialogue(); CloseOverlay(); if (session.ActiveDialogue != null) Dialogue(); else { Show(CaseFileScreen.Hud); Save(false); } }, "fd-nav-button");
        }

        private void Message(string title, string body, Sprite artwork = null)
        {
            var card = Card(title); if (artwork != null) AddArtwork(card, artwork, "fd-detail-image"); Text(card, body, "fd-dialogue-body");
            Button(card, "Đóng", CloseOverlay, "fd-nav-button");
        }

        private VisualElement Card(string title)
        {
            CloseOverlay(); overlay = new VisualElement(); overlay.AddToClassList("fd-overlay"); screen.Add(overlay);
            var card = new VisualElement(); card.AddToClassList("fd-card"); overlay.Add(card);
            Text(card, title, "fd-dialogue-title"); return card;
        }

        private void CloseOverlay() { overlay?.RemoveFromHierarchy(); overlay = null; }

        private void Save(bool notify)
        {
            try { saves.Save(session); if (notify) Message("Checkpoint", "Đã lưu toàn bộ tiến độ."); }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            { Message("Chưa lưu được", ex.Message); }
        }
        private bool Load()
        {
            try { saves.Load(session); feedback = "Đã tải toàn bộ trạng thái tại checkpoint."; return true; }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            { Message("Chưa tải được", ex.Message); return false; }
        }
        private void SetPaused(bool value)
        {
            if (value == paused) return;
            if (value) { previousTimeScale = Time.timeScale; Time.timeScale = 0; }
            else Time.timeScale = previousTimeScale;
            paused = value;
        }
        private void Hide(string name) { var e = screen.Q<VisualElement>(name); if (e != null) e.style.display = DisplayStyle.None; }
        private void HideClass(string cls) => screen.Query<VisualElement>(className: cls).ForEach(e => e.style.display = DisplayStyle.None);
        private void Set(string name, string value) { var l = screen.Q<Label>(name); if (l != null) { l.text = value; Vietnamese(l); } }
        private void SetClass(string cls, string value) => screen.Query<Label>(className: cls).ForEach(l => { l.text = value; Vietnamese(l); });
        private void Vietnamese(VisualElement e) => e.style.unityFontDefinition = FontDefinition.FromFont(bodyFont);
        private void AddArtwork(VisualElement parent, Sprite sprite, string cls)
        {
            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList(cls);
            image.style.backgroundImage = new StyleBackground(sprite);
            parent.Add(image);
        }
        private Label Text(VisualElement parent, string text, string cls)
        { var l = new Label(text) { pickingMode = PickingMode.Ignore }; l.AddToClassList(cls); Vietnamese(l); parent.Add(l); return l; }
        private Button Button(VisualElement parent, string text, Action action, string cls)
        { var b = new Button(action) { text = text }; b.AddToClassList(cls); Vietnamese(b); parent.Add(b); return b; }
        private void Menu(int index, string text, Action action)
        {
            var button = screen.Q<Button>("menu-item-" + index.ToString("00"));
            if (button == null) return;
            var label = button.Q<Label>(className: CurrentScreen == CaseFileScreen.MainMenu ? "s01-menu-item__label" : "s10-menu-label");
            if (label != null) { label.text = text; Vietnamese(label); }
            button.clicked += action;
        }
        private void BindButton(string name, string text, Action action)
        {
            var button = screen.Q<Button>(name); if (button == null) return;
            var label = button.Q<Label>(className: "hcf-btn__label");
            if (label != null) { label.text = text; Vietnamese(label); }
            button.clicked += action;
        }
    }
}
