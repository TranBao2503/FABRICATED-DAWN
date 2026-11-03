using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using HorrorCaseFile.UI;

namespace HorrorCaseFile.Demo
{
    /// <summary>Provides screen-specific behavior used by the demo scene.</summary>
    public static class HcfSpecimenDemo
    {
        const string MenuPrefix = "menu-item-";
        const string CellPrefix = "cell-";
        const string EmptyValue = "—";

        // Fallback wording for a screen where no slot ships empty; the markup wins when it
        // carries one.
        const string EmptySlotFallback = "— empty slot —";

        const string SlotLocationClass = "s03-slot__data-location";
        const string SlotDocumentsClass = "s03-slot__data-documents";
        const string SlotDeathsClass = "s03-slot__data-deaths";

        const string CellKickerClass = "s11-cell__data-kicker";
        const string CellTitleClass = "s11-cell__data-title";
        const string CellBodyClass = "s11-cell__data-body";
        const string CellConditionClass = "s11-cell__data-condition";
        const string CellWeightClass = "s11-cell__data-weight";
        const string CellNoiseClass = "s11-cell__data-noise";
        const string CellDropClass = "s11-cell__data-drop";

        const string CombineResultClass = "s11d-cell__data-result";
        const string CombineResultBodyClass = "s11d-cell__data-result-body";
        const string CombinePairClass = "s11d-cell__data-pair";
        const string SlotTitleClass = "s11d-slot__data-title";

        const string ItemClassPrefix = "hcf-item--";

        const string ShotClassPrefix = "s03-shot--";

        const long SkipDelayMs = 1000;
        const long SkipBlinkMs = 650;
        const float SkipLitOpacity = 0.55f;
        const float SkipDimOpacity = 0.16f;

        const long HoldTickMs = 16;
        const float HoldSeconds = 1.4f;

        const float StampCornerInset = 96f;

        static readonly ConditionalWeakTable<VisualElement, object> s_Wired =
            new ConditionalWeakTable<VisualElement, object>();

        public static void Wire(VisualElement root, string slug)
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

            switch (slug)
            {
                case "DifficultySelect":
                    WireDifficulty(root);
                    break;
                case "LoadSave":
                    WireSaves(root);
                    break;
                case "SaveOverwrite":
                    WireOverwriteScreen(root);
                    break;
                case "Loading":
                    WireSkipHint(root);
                    break;
                case "Prompts":
                    WireHold(root);
                    break;
                case "Inventory":
                    WireInventory(root);
                    break;
                case "Combine":
                    WireCombine(root);
                    break;
                case "Journal":
                    WireJournal(root);
                    break;
            }
        }

        static void WireDifficulty(VisualElement root)
        {
            HcfScreenInteractions.Selection cards = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            VisualElement stamp = root.Q<VisualElement>("stamp-selected");
            if (cards == null || stamp == null)
            {
                return;
            }

            VisualElement row = cards.current == null ? null : cards.current.parent;
            if (row == null)
            {
                return;
            }

            System.Action place = () =>
            {
                VisualElement card = cards.current;
                VisualElement host = stamp.parent;
                if (card == null || card.parent == null || host == null || card.layout.width <= 0f)
                {
                    return;
                }

                stamp.style.left = card.parent.worldBound.x + card.layout.x + card.layout.width
                    - StampCornerInset - host.worldBound.x;
            };

            cards.changed += (index, card) => place();
            row.RegisterCallback<GeometryChangedEvent>(evt => place());
            stamp.RegisterCallback<GeometryChangedEvent>(evt => place());
        }

        static void WireSaves(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            if (slots == null)
            {
                return;
            }

            Label location = root.Q<Label>("stat-location");
            Label documents = root.Q<Label>("stat-documents");
            Label deaths = root.Q<Label>("stat-deaths");
            VisualElement thumbnail = root.Q<VisualElement>("save-thumbnail");
            Button load = root.Q<Button>("btn-load");
            Button overwrite = root.Q<Button>("btn-overwrite");
            Button delete = root.Q<Button>("btn-delete");

            string[] authored =
            {
                location == null ? null : location.text,
                documents == null ? null : documents.text,
                deaths == null ? null : deaths.text,
            };

            System.Action refresh = () =>
            {
                VisualElement slot = slots.current;
                bool empty = IsEmptySlot(slot);
                SetText(location, empty ? EmptyValue : SlotData(slot, SlotLocationClass, authored[0]));
                SetText(documents, empty ? EmptyValue : SlotData(slot, SlotDocumentsClass, authored[1]));
                SetText(deaths, empty ? EmptyValue : SlotData(slot, SlotDeathsClass, authored[2]));
                if (thumbnail != null)
                {
                    thumbnail.style.opacity = empty ? 0.25f : 1f;

                    VisualElement shot = FindByClass(thumbnail, "s03-detail__hatch");
                    string shotClass = ClassWithPrefix(
                        FindByClass(slot, "s03-slot__data-shot"), ShotClassPrefix);
                    if (shot != null && !string.IsNullOrEmpty(shotClass))
                    {
                        SetPrefixedClass(shot, ShotClassPrefix, shotClass);
                    }
                }

                SetEnabled(load, !empty);
                SetEnabled(delete, !empty);
                SetEnabled(overwrite, slot != null);
            };

            slots.changed += (index, item) => refresh();

            if (delete != null)
            {
                delete.clicked += () => DeleteSelectedSave(root);
            }

            refresh();
            UpdateSlotCount(root);
        }

        static string SlotData(VisualElement slot, string className, string fallback)
        {
            Label field = FindByClass(slot, className) as Label;
            return field == null ? fallback : field.text;
        }

        public static string SelectedSaveTitle(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            VisualElement slot = slots == null ? null : slots.current;
            if (slot == null || IsEmptySlot(slot))
            {
                return null;
            }

            Label title = FindByClass(slot, "s03-slot__title") as Label;
            return title == null ? null : title.text;
        }

        static string ClassWithPrefix(VisualElement element, string prefix)
        {
            if (element == null)
            {
                return null;
            }

            foreach (string name in element.GetClasses())
            {
                if (name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return name;
                }
            }

            return null;
        }

        static void SetPrefixedClass(VisualElement element, string prefix, string value)
        {
            if (element == null)
            {
                return;
            }

            List<string> stale = new List<string>();
            foreach (string name in element.GetClasses())
            {
                if (name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    stale.Add(name);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                element.RemoveFromClassList(stale[i]);
            }

            if (!string.IsNullOrEmpty(value))
            {
                element.AddToClassList(value);
            }
        }

        public static string ItemClassOf(VisualElement owner)
        {
            VisualElement art = owner == null ? null : FindByClass(owner, "s11-cell-art");
            if (art == null)
            {
                art = owner;
            }

            if (art == null)
            {
                return null;
            }

            foreach (string name in art.GetClasses())
            {
                if (name.StartsWith(ItemClassPrefix, System.StringComparison.Ordinal))
                {
                    return name;
                }
            }

            return null;
        }

        public static void SetItemClass(VisualElement art, string itemClass)
        {
            if (art == null)
            {
                return;
            }

            List<string> stale = new List<string>();
            foreach (string name in art.GetClasses())
            {
                if (name.StartsWith(ItemClassPrefix, System.StringComparison.Ordinal))
                {
                    stale.Add(name);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                art.RemoveFromClassList(stale[i]);
            }

            if (!string.IsNullOrEmpty(itemClass))
            {
                art.AddToClassList(itemClass);
            }
        }

        public static string SelectedItemClass(VisualElement root)
        {
            HcfScreenInteractions.Selection cells = HcfScreenInteractions.GetSelection(root, CellPrefix);
            VisualElement cell = cells == null ? null : cells.current;
            return IsEmptyCell(cell) ? null : ItemClassOf(cell);
        }

        public static string SelectedItemTitle(VisualElement root)
        {
            Label title = root == null ? null : root.Q<Label>("detail-title");
            return title == null ? null : title.text;
        }

        public static bool SelectedSaveIsEmpty(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            return slots == null || IsEmptySlot(slots.current);
        }

        static bool IsEmptySlot(VisualElement slot)
        {
            if (slot == null)
            {
                return true;
            }

            if (!slot.enabledSelf)
            {
                return true;
            }

            return FindByClass(slot, "s03-slot__empty") != null;
        }

        public static void DeleteSelectedSave(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            VisualElement slot = slots == null ? null : slots.current;
            if (slot == null || IsEmptySlot(slot))
            {
                return;
            }

            VisualElement text = FindByClass(slot, "s03-slot__text");
            if (text != null)
            {
                text.style.display = DisplayStyle.None;
            }

            Label tag = FindByClass(slot, "s03-slot__tag") as Label;
            if (tag != null)
            {
                tag.style.display = DisplayStyle.None;
            }

            Label flag = FindByClass(slot, "s03-slot__flag") as Label;
            if (flag != null)
            {
                flag.style.display = DisplayStyle.None;
            }

            Label index = FindByClass(slot, "s03-slot__index") as Label;
            if (index != null)
            {
                index.AddToClassList("s03-slot__index--dim");
            }

            VisualElement row = FindByClass(slot, "s03-slot__row");
            if (row != null && FindByClass(slot, "s03-slot__empty") == null)
            {
                // The wording is copied from a slot that ships empty on the same screen.
                Label sample = FindByClass(root, "s03-slot__empty") as Label;
                Label empty = new Label(sample == null || string.IsNullOrEmpty(sample.text)
                    ? EmptySlotFallback
                    : sample.text);
                empty.AddToClassList("hcf-typed");
                empty.AddToClassList("s03-slot__empty");
                empty.pickingMode = PickingMode.Ignore;
                row.Add(empty);
            }

            slot.SetEnabled(false);
            UpdateSlotCount(root);

            slots.Apply(NextEnabled(slots));
        }

        static void WireOverwriteScreen(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            if (slots == null)
            {
                return;
            }

            slots.changed += (index, item) => RefreshOverwrite(root, slots);

            RefreshOverwrite(root, slots);
            UpdateFreeCount(root, slots);
        }

        static void RefreshOverwrite(VisualElement root, HcfScreenInteractions.Selection slots)
        {
            VisualElement slot = Target(slots);
            bool writable = slot != null && !IsFreeSlot(slot);

            VisualElement warn = FindByClass(root, "s03b-slot__flag--warn");
            if (warn != null)
            {
                VisualElement row = writable ? FindByClass(slot, "s03-slot__row") : null;
                if (row != null)
                {
                    row.Add(warn);
                }

                warn.style.display = writable ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Label note = FindByClass(root, "s03b-note") as Label;
            if (note != null)
            {
                note.style.visibility = writable ? Visibility.Visible : Visibility.Hidden;
                Label index = writable ? FindByClass(slot, "s03-slot__index") as Label : null;
                if (index != null)
                {
                    note.text = Renumber(note.text, index.text);
                }
            }

            Button overwrite = root.Q<Button>("btn-overwrite");
            Label caption = overwrite == null ? null : FindByClass(overwrite, "hcf-btn__label") as Label;
            if (caption != null)
            {
                caption.text = writable ? "OVERWRITE" : "WRITE";
            }
        }

        static VisualElement Target(HcfScreenInteractions.Selection slots)
        {
            VisualElement slot = slots.current;
            if (slot != null && slot.ClassListContains("s03b-slot--new"))
            {
                return FirstFree(slots);
            }

            return slot;
        }

        public static bool AskBeforeWrite(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            if (slots == null)
            {
                return false;
            }

            VisualElement slot = Target(slots);
            if (slot == null || IsFreeSlot(slot))
            {
                return false;
            }

            Label index = FindByClass(slot, "s03-slot__index") as Label;
            Label title = FindByClass(slot, "s03-slot__title") as Label;
            Label meta = FindByClass(slot, "s03-slot__meta") as Label;
            HcfDemoFlow.SetOverwriteTarget(
                slots.items.IndexOf(slot),
                index == null ? null : index.text,
                title == null ? null : title.text,
                meta == null ? null : meta.text);
            return true;
        }

        public static void WriteSlotByIndex(VisualElement root, int index)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            if (slots == null || index < 0 || index >= slots.items.Count)
            {
                return;
            }

            slots.Apply(index);
            WriteSelectedSave(root);
        }

        public static void WriteSelectedSave(VisualElement root)
        {
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            if (slots == null)
            {
                return;
            }

            VisualElement slot = Target(slots);
            if (slot == null)
            {
                HcfDemoFlow.Flash(root.Q<VisualElement>("btn-overwrite"));
                return;
            }

            WriteSlot(slot,
                TextByClass(root, "s03b-write-title"),
                TextByClass(root, "s03b-write-meta"));

            HcfDemoFlow.Flash(slot);
            UpdateFreeCount(root, slots);
            RefreshOverwrite(root, slots);
        }

        static void WriteSlot(VisualElement slot, string title, string meta)
        {
            VisualElement row = FindByClass(slot, "s03-slot__row");
            if (row == null)
            {
                return;
            }

            Label empty = FindByClass(slot, "s03-slot__empty") as Label;
            if (empty != null)
            {
                empty.RemoveFromHierarchy();
            }

            Label index = FindByClass(slot, "s03-slot__index") as Label;
            if (index != null)
            {
                index.RemoveFromClassList("s03-slot__index--dim");
            }

            VisualElement text = FindByClass(slot, "s03-slot__text");
            if (text == null)
            {
                text = new VisualElement();
                text.AddToClassList("s03-slot__text");
                text.pickingMode = PickingMode.Ignore;
                text.Add(NewSlotLabel("hcf-bs-700", "s03-slot__title"));
                text.Add(NewSlotLabel("hcf-mono", "s03-slot__meta"));

                row.Insert(index == null ? 0 : row.IndexOf(index) + 1, text);
            }

            text.style.display = DisplayStyle.Flex;
            SetText(FindByClass(text, "s03-slot__title") as Label, title);
            SetText(FindByClass(text, "s03-slot__meta") as Label, meta);

            Label tag = FindByClass(slot, "s03-slot__tag") as Label;
            if (tag != null)
            {
                tag.style.display = DisplayStyle.None;
            }
        }

        static Label NewSlotLabel(string fontClass, string roleClass)
        {
            Label label = new Label();
            label.AddToClassList(fontClass);
            label.AddToClassList(roleClass);
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        static bool IsFreeSlot(VisualElement slot)
        {
            return slot != null && FindByClass(slot, "s03-slot__empty") != null;
        }

        static VisualElement FirstFree(HcfScreenInteractions.Selection slots)
        {
            foreach (VisualElement slot in slots.items)
            {
                if (IsFreeSlot(slot))
                {
                    return slot;
                }
            }

            return null;
        }

        static void UpdateFreeCount(VisualElement root, HcfScreenInteractions.Selection slots)
        {
            Label free = FindByClass(root, "s03b-slot__free") as Label;
            if (free == null)
            {
                return;
            }

            int n = 0;
            foreach (VisualElement slot in slots.items)
            {
                if (IsFreeSlot(slot))
                {
                    n++;
                }
            }

            free.text = HcfScreenInteractions.Format(free.text, n);
        }

        static string Renumber(string text, string number)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(number))
            {
                return text;
            }

            int start = -1;
            int end = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    if (start < 0)
                    {
                        start = i;
                    }

                    end = i;
                }
                else if (start >= 0)
                {
                    break;
                }
            }

            if (start < 0)
            {
                return text;
            }

            return text.Substring(0, start) + number + text.Substring(end + 1);
        }

        static string TextByClass(VisualElement owner, string className)
        {
            Label label = FindByClass(owner, className) as Label;
            return label == null ? null : label.text;
        }

        static int NextEnabled(HcfScreenInteractions.Selection group)
        {
            for (int i = 0; i < group.items.Count; i++)
            {
                if (group.items[i].enabledSelf)
                {
                    return i;
                }
            }

            return -1;
        }

        static void UpdateSlotCount(VisualElement root)
        {
            Label count = FindByClass(root, "s03-header__count") as Label;
            HcfScreenInteractions.Selection slots = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            if (count == null || slots == null)
            {
                return;
            }

            int used = 0;
            foreach (VisualElement slot in slots.items)
            {
                if (!IsEmptySlot(slot))
                {
                    used++;
                }
            }

            count.text = slots.items.Count + " SLOTS / " + used + " USED";
        }

        static void WireSkipHint(VisualElement root)
        {
            VisualElement hint = root.Q<VisualElement>("skip-hint");
            if (hint == null)
            {
                return;
            }

            bool lit = false;
            hint.style.opacity = 0f;
            hint.schedule.Execute(() =>
            {
                lit = !lit;
                hint.style.opacity = lit ? SkipLitOpacity : SkipDimOpacity;
            }).Every(SkipBlinkMs).StartingIn(SkipDelayMs);
        }

        static void WireHold(VisualElement root)
        {
            ProgressBar bar = root.Q<ProgressBar>("progress-hold");
            if (bar == null)
            {
                return;
            }

            bool holding = false;
            float span = bar.highValue - bar.lowValue;
            float t = span > 0f ? Mathf.Clamp01((bar.value - bar.lowValue) / span) : 0f;

            IVisualElementScheduledItem tick = null;
            tick = root.schedule.Execute(() =>
            {
                float step = Time.deltaTime / HoldSeconds;
                t = Mathf.Clamp01(holding ? t + step : t - step * 2f);
                bar.value = Mathf.Lerp(bar.lowValue, bar.highValue, t);
                if (!holding && t <= 0f)
                {
                    tick.Pause();
                }
            }).Every(HoldTickMs);
            tick.Pause();

            System.Action<bool> setHold = value =>
            {
                if (holding == value)
                {
                    return;
                }

                holding = value;
                if (!tick.isActive)
                {
                    tick.Resume();
                }
            };

            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.E)
                {
                    setHold(true);
                }
            });
            root.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.E)
                {
                    setHold(false);
                }
            });

            // Keep the pointer target and progress bar on the same prompt card.
            VisualElement prompt = root.Q<VisualElement>("prompt-hold");
            if (prompt != null)
            {
                prompt.RegisterCallback<PointerDownEvent>(evt => setHold(true));
                prompt.RegisterCallback<PointerUpEvent>(evt => setHold(false));
                prompt.RegisterCallback<PointerLeaveEvent>(evt => setHold(false));
            }
        }

        static void WireInventory(VisualElement root)
        {
            HcfScreenInteractions.Selection cells = HcfScreenInteractions.GetSelection(root, CellPrefix);
            Label meta = root.Q<Label>("inv-meta");

            Label kicker = root.Q<Label>("detail-kicker");
            Label title = root.Q<Label>("detail-title");
            Label body = root.Q<Label>("detail-body");
            Label condition = root.Q<Label>("stat-condition");
            Label weight = root.Q<Label>("stat-weight");
            UI.HcfSegmentedMeter noise = root.Q<UI.HcfSegmentedMeter>("meter-noise");
            Button equip = root.Q<Button>("btn-equip");
            Button examine = root.Q<Button>("btn-examine");
            Button combine = root.Q<Button>("btn-combine");
            Button drop = root.Q<Button>("btn-drop");

            string[] authored =
            {
                kicker == null ? null : kicker.text,
                title == null ? null : title.text,
                body == null ? null : body.text,
                condition == null ? null : condition.text,
                weight == null ? null : weight.text,
            };

            int authoredNoise = noise == null ? 0 : noise.filled;

            System.Action refresh = () =>
            {
                VisualElement cell = cells == null ? null : cells.current;
                bool empty = IsEmptyCell(cell);
                SetText(kicker, empty ? "SELECTED · EMPTY" : SlotData(cell, CellKickerClass, authored[0]));
                SetText(title, empty ? "— EMPTY SLOT —" : SlotData(cell, CellTitleClass, authored[1]));

                string description = empty ? string.Empty : SlotData(cell, CellBodyClass, authored[2]);
                SetText(body, description);
                if (body != null)
                {
                    body.style.display = string.IsNullOrEmpty(description)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
                }

                SetText(condition, empty ? EmptyValue : SlotData(cell, CellConditionClass, authored[3]));
                SetText(weight, empty ? EmptyValue : SlotData(cell, CellWeightClass, authored[4]));

                if (noise != null)
                {
                    int filled;
                    string level = empty ? null : SlotData(cell, CellNoiseClass, null);
                    if (empty)
                    {
                        noise.filled = 0;
                    }
                    else if (!string.IsNullOrEmpty(level) && int.TryParse(level, out filled))
                    {
                        noise.filled = filled;
                    }
                    else
                    {
                        noise.filled = authoredNoise;
                    }
                }

                SetEnabled(equip, !empty);
                SetEnabled(examine, !empty);
                SetEnabled(combine, !empty);

                SetEnabled(drop, !empty && SlotData(cell, CellDropClass, "yes") != "no");
            };

            InventoryMeta counter = new InventoryMeta(meta, cells);

            if (cells != null)
            {
                cells.changed += (index, item) => refresh();
            }

            if (equip != null)
            {
                equip.clicked += () =>
                {
                    if (kicker == null)
                    {
                        return;
                    }

                    int dot = kicker.text == null ? -1 : kicker.text.IndexOf('·');
                    string category = dot < 0 ? string.Empty : kicker.text.Substring(dot + 1).Trim();
                    SetText(kicker, category.Length == 0 ? "EQUIPPED" : "EQUIPPED · " + category);
                };
            }

            if (drop != null && cells != null)
            {
                drop.clicked += () =>
                {
                    if (EmptyCell(cells.current))
                    {
                        counter.Refresh();
                        refresh();
                    }
                };
            }

            refresh();
            counter.Refresh();
        }

        static bool IsEmptyCell(VisualElement cell)
        {
            return cell == null || cell.ClassListContains("s11-cell--empty");
        }

        static void WireCombine(VisualElement root)
        {
            HcfScreenInteractions.Selection cells = HcfScreenInteractions.GetSelection(root, CellPrefix);
            if (cells == null)
            {
                return;
            }

            Label hint = root.Q<Label>("combine-hint");
            VisualElement slotFirst = root.Q<VisualElement>("slot-first");
            VisualElement slotSecond = root.Q<VisualElement>("slot-second");
            VisualElement artSecond = root.Q<VisualElement>("art-second");
            VisualElement artFirst = root.Q<VisualElement>("art-first");
            VisualElement artResult = root.Q<VisualElement>("art-result");
            VisualElement slotResult = root.Q<VisualElement>("slot-result");
            Label resultKicker = root.Q<Label>("result-kicker");
            Label resultTitle = root.Q<Label>("result-title");
            Label resultBody = root.Q<Label>("result-body");
            Button combine = root.Q<Button>("btn-combine");

            bool done = false;

            string firstClass;
            string firstTitle;
            if (HcfDemoFlow.TakeCombineFirst(out firstClass, out firstTitle))
            {
                SetItemClass(artFirst, firstClass);
                if (!string.IsNullOrEmpty(firstTitle))
                {
                    SetText(FindByClass(slotFirst, SlotTitleClass) as Label, firstTitle);
                }
            }

            string firstName = TextByClass(slotFirst, SlotTitleClass);

            if (cells != null)
            {
                for (int i = 0; i < cells.items.Count; i++)
                {
                    VisualElement item = cells.items[i];
                    string itemPair = SlotData(item, CombinePairClass, null);
                    bool fits = !string.IsNullOrEmpty(itemPair)
                        && !string.IsNullOrEmpty(firstName)
                        && !string.IsNullOrEmpty(SlotData(item, CombineResultClass, null))
                        && string.Equals(itemPair.Trim(), firstName.Trim(),
                            System.StringComparison.OrdinalIgnoreCase);
                    item.EnableInClassList("s11d-cell--rust", fits);
                }
            }

            System.Action refresh = () =>
            {
                if (done)
                {
                    return;
                }

                VisualElement cell = cells.current;
                string result = SlotData(cell, CombineResultClass, null);
                string pair = SlotData(cell, CombinePairClass, null);

                bool pairs = !string.IsNullOrEmpty(result)
                    && !string.IsNullOrEmpty(pair)
                    && !string.IsNullOrEmpty(firstName)
                    && string.Equals(pair.Trim(), firstName.Trim(),
                        System.StringComparison.OrdinalIgnoreCase);

                SetItemClass(artSecond, ItemClassOf(cell));

                SetText(resultKicker, pairs ? "RESULT" : "NO RESULT");
                SetText(resultTitle, pairs ? result : EmptyValue);
                SetText(resultBody, pairs
                    ? SlotData(cell, CombineResultBodyClass, string.Empty)
                    : "these two do not go together. pick something else, or back out.");
                SetEnabled(combine, pairs);
                if (slotResult != null)
                {
                    slotResult.style.opacity = pairs ? 1f : 0.25f;
                }

                // The frame of the second slot means "this one fits", the same flag that
                // lights the result and the button.
                if (slotSecond != null)
                {
                    slotSecond.EnableInClassList("s11d-slot--rust", pairs);
                }
            };

            cells.changed += (index, item) => refresh();

            if (combine != null)
            {
                combine.clicked += () =>
                {
                    VisualElement cell = cells.current;
                    string result = SlotData(cell, CombineResultClass, null);
                    if (done || string.IsNullOrEmpty(result))
                    {
                        return;
                    }

                    string spent = SlotData(cell, CellTitleClass, null);
                    done = true;
                    EmptyCell(cell);
                    cell.RemoveFromClassList("s11d-cell--rust");
                    SetItemClass(artFirst, ItemClassOf(artResult));
                    SetItemClass(artSecond, null);
                    if (slotSecond != null)
                    {
                        slotSecond.RemoveFromClassList("s11d-slot--rust");
                    }

                    if (slotResult != null)
                    {
                        slotResult.style.opacity = 0.25f;
                    }

                    SetText(hint, string.IsNullOrEmpty(spent)
                        ? "combined · the second item is used up"
                        : "combined · the " + spent.ToLowerInvariant() + " is used up");
                    SetText(resultKicker, "COMBINED");
                    SetEnabled(combine, false);
                };
            }

            refresh();
        }

        static bool EmptyCell(VisualElement cell)
        {
            if (cell == null || IsEmptyCell(cell))
            {
                return false;
            }

            cell.RemoveFromClassList("s11-cell--full");
            cell.AddToClassList("s11-cell--empty");

            VisualElement art = FindByClass(cell, "s11-cell-art");
            if (art != null)
            {
                art.style.display = DisplayStyle.None;
            }

            Label count = FindByClass(cell, "s11-cell-count") as Label;
            if (count != null)
            {
                count.style.display = DisplayStyle.None;
            }

            Label tag = FindByClass(cell, "s11-cell-new") as Label;
            if (tag != null)
            {
                tag.style.display = DisplayStyle.None;
            }

            if (FindByClass(cell, "s11-cell-frame") == null)
            {
                UI.HcfDashedFrame frame = new UI.HcfDashedFrame();
                frame.AddToClassList("s11-cell-frame");

                // The stroke is copied from a cell that ships empty in the same grid; the
                // numbers are the fallback for a screen without one.
                UI.HcfDashedFrame sample = cell.parent == null
                    ? null
                    : cell.parent.Q<UI.HcfDashedFrame>(null, "s11-cell-frame");
                frame.lineWidth = sample == null ? 1f : sample.lineWidth;
                frame.dash = sample == null ? 3f : sample.dash;
                frame.gap = sample == null ? 3f : sample.gap;
                cell.Insert(0, frame);
            }

            return true;
        }

        sealed class InventoryMeta
        {
            readonly Label m_Label;
            readonly HcfScreenInteractions.Selection m_Cells;
            readonly float m_KgPerItem;
            readonly bool m_Ready;

            public InventoryMeta(Label label, HcfScreenInteractions.Selection cells)
            {
                m_Label = label;
                m_Cells = cells;
                if (label == null || cells == null)
                {
                    return;
                }

                float kg = FirstFloat(label.text, 2);
                int filled = Filled();
                m_KgPerItem = filled > 0 && kg > 0f ? kg / filled : 0f;
                m_Ready = m_KgPerItem > 0f;
            }

            public void Refresh()
            {
                if (m_Label == null || m_Cells == null)
                {
                    return;
                }

                int filled = Filled();
                int total = m_Cells.items.Count;
                string kg = m_Ready
                    ? (filled * m_KgPerItem).ToString("0.0", CultureInfo.InvariantCulture)
                    : "0.0";
                m_Label.text = "carrying " + filled + " of " + total + " · " + kg + " kg";
            }

            int Filled()
            {
                int n = 0;
                foreach (VisualElement cell in m_Cells.items)
                {
                    if (!IsEmptyCell(cell))
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        static float FirstFloat(string text, int skip)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            int seen = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (!char.IsDigit(text[i]))
                {
                    i++;
                    continue;
                }

                int start = i;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
                {
                    i++;
                }

                seen++;
                if (seen > skip)
                {
                    float value;
                    if (float.TryParse(text.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        return value;
                    }

                    return 0f;
                }
            }

            return 0f;
        }

        static void WireJournal(VisualElement root)
        {
            HcfScreenInteractions.Selection docs = HcfScreenInteractions.GetSelection(root, MenuPrefix);
            ScrollView page = root.Q<ScrollView>("page-scroll");
            Label form = root.Q<Label>("page-form");
            Label titleA = root.Q<Label>("page-title-1");
            Label titleB = root.Q<Label>("page-title-2");

            if (docs == null)
            {
                return;
            }

            List<VisualElement> authoredSheets = page == null
                ? new List<VisualElement>()
                : Sheets(page);
            List<Label> metaLines = root.Query<Label>(null, "s13-page__meta-line").ToList();
            Label metaFile = metaLines.Count > 0 ? metaLines[0] : null;
            Label metaDate = metaLines.Count > 1 ? metaLines[1] : null;
            Label sign = FindByClass(root, "s13-page__sign") as Label;
            Label stamp = FindByClass(root, "s13-page__stamp-label") as Label;
            string authoredForm = form == null ? null : form.text;
            string authoredFile = metaFile == null ? null : metaFile.text;
            string authoredDate = metaDate == null ? null : metaDate.text;
            string authoredSign = sign == null ? null : sign.text;
            string authoredStamp = stamp == null ? null : stamp.text;

            docs.changed += (index, item) =>
            {
                if (item == null)
                {
                    return;
                }

                Label rowTitle = FindByClass(item, "s13-doc__title") as Label;
                Label rowMeta = FindByClass(item, "s13-doc__meta") as Label;

                if (rowTitle != null)
                {
                    string text = rowTitle.text;
                    int dash = text.IndexOf('—');
                    if (dash > 0 && titleB != null)
                    {
                        SetText(titleA, text.Substring(0, dash).TrimEnd());
                        SetText(titleB, text.Substring(dash).Trim());
                    }
                    else
                    {
                        SetText(titleA, text);
                        SetText(titleB, string.Empty);
                    }
                }

                if (form != null)
                {
                    // Keep the authored form for rows without sheet data. Sheet-backed rows use
                    // explicit form metadata first, then their row metadata.
                    string named = SlotData(item, "s13-doc__data-form", null);
                    if (named == null && rowMeta != null
                        && FindByClass(item, "s13-doc__data-sheet") != null)
                    {
                        named = rowMeta.text.ToUpperInvariant();
                    }

                    SetText(form, named ?? authoredForm);
                }

                SetText(metaFile, SlotData(item, "s13-doc__data-file", authoredFile));
                SetText(metaDate, SlotData(item, "s13-doc__data-date", authoredDate));
                SetText(sign, SlotData(item, "s13-doc__data-sign", authoredSign));
                SetText(stamp, SlotData(item, "s13-doc__data-stamp", authoredStamp));

                if (page != null)
                {
                    List<VisualElement> sheets = DocumentSheets(item);
                    if (sheets.Count == 0)
                    {
                        sheets = authoredSheets;
                    }

                    page.Clear();
                    for (int i = 0; i < sheets.Count; i++)
                    {
                        page.Add(sheets[i]);
                    }
                }

                ShowJournalSheet(root, page, 0);
            };
        }

        static List<VisualElement> DocumentSheets(VisualElement row)
        {
            List<VisualElement> sheets = new List<VisualElement>();
            if (row == null)
            {
                return sheets;
            }

            List<VisualElement> blocks = row.Query<VisualElement>(null, "s13-doc__data-sheet").ToList();
            for (int i = 0; i < blocks.Count; i++)
            {
                VisualElement sheet = new VisualElement();
                List<Label> paragraphs = blocks[i].Query<Label>(null, "s13-doc__data-para").ToList();
                for (int j = 0; j < paragraphs.Count; j++)
                {
                    Label line = new Label(paragraphs[j].text);
                    line.AddToClassList("hcf-typed");
                    line.AddToClassList("s13-page__para");
                    if (j == 0)
                    {
                        line.AddToClassList("s13-page__para--first");
                    }

                    if (j == paragraphs.Count - 1)
                    {
                        line.AddToClassList("s13-page__para--last");
                    }

                    sheet.Add(line);
                }

                sheets.Add(sheet);
            }

            return sheets;
        }

        public static void NextJournalPage(VisualElement root)
        {
            ScrollView page = root == null ? null : root.Q<ScrollView>("page-scroll");
            if (page == null)
            {
                return;
            }

            float high = page.verticalScroller.highValue;
            if (high > 0f && page.scrollOffset.y < high - 1f)
            {
                float step = page.contentViewport.layout.height;
                float next = step > 0f ? Mathf.Min(page.scrollOffset.y + step, high) : high;
                page.scrollOffset = new Vector2(page.scrollOffset.x, next);
                return;
            }

            ShowJournalSheet(root, page, CurrentSheet(page) + 1);
        }

        static void ShowJournalSheet(VisualElement root, ScrollView page, int index)
        {
            List<VisualElement> sheets = Sheets(page);
            if (sheets.Count == 0)
            {
                return;
            }

            int target = ((index % sheets.Count) + sheets.Count) % sheets.Count;
            for (int i = 0; i < sheets.Count; i++)
            {
                sheets[i].style.display = i == target ? DisplayStyle.Flex : DisplayStyle.None;
            }

            page.scrollOffset = Vector2.zero;

            Label number = root == null ? null : root.Q<Label>("journal-page-num");
            if (number != null)
            {
                number.text = Retitle(number.text, target + 1, sheets.Count);
            }
        }

        static int CurrentSheet(ScrollView page)
        {
            List<VisualElement> sheets = Sheets(page);
            for (int i = 0; i < sheets.Count; i++)
            {
                if (sheets[i].resolvedStyle.display != DisplayStyle.None)
                {
                    return i;
                }
            }

            return 0;
        }

        static List<VisualElement> Sheets(ScrollView page)
        {
            List<VisualElement> sheets = new List<VisualElement>();
            if (page == null)
            {
                return sheets;
            }

            foreach (VisualElement child in page.contentContainer.Children())
            {
                sheets.Add(child);
            }

            return sheets;
        }

        static string Retitle(string sample, int current, int total)
        {
            if (string.IsNullOrEmpty(sample))
            {
                return current + " / " + total;
            }

            return "PAGE " + current + " OF " + total;
        }

        static void SetText(Label label, string text)
        {
            if (label != null && text != null)
            {
                label.text = text;
            }
        }

        static void SetEnabled(VisualElement element, bool enabled)
        {
            if (element != null)
            {
                element.SetEnabled(enabled);
            }
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
