using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using HorrorCaseFile.UI;

namespace HorrorCaseFile.Demo
{
    /// <summary>Connects demo screens and maintains state between transitions.</summary>
    public static class HcfDemoFlow
    {
        const string ActiveClass = "is-active";
        const string FlashClass = "hcf-flash";
        const string MenuPrefix = "menu-item-";
        const string CellPrefix = "cell-";
        const long FlashMs = 150;
        const float LoadingSeconds = 2.5f;
        const long LoadingTickMs = 33;

        const string OptionsOffClass = "s10b-options--off";
        const string NoteOffClass = "s10b-tabs-note--off";

        const int QuickSlots = 3;

        static readonly ConditionalWeakTable<VisualElement, object> s_Wired =
            new ConditionalWeakTable<VisualElement, object>();

        static readonly string[] SettingsTabs =
        {
            "tab-video", "SettingsVideo",
            "tab-audio", "SettingsAudio",
            "tab-controls", "SettingsControls",
            "tab-gameplay", "SettingsGameplay",
        };

        static readonly string[] PauseTabs =
        {
            "tab-video", "options", "RESOLUTION LOCKED IN GAME",
            "tab-audio", "options-audio", "HEADPHONES RECOMMENDED",
            "tab-controls", "options-controls", "",
            "tab-gameplay", "options-gameplay", "",
        };

        static readonly string[] MapFloors =
        {
            "tab-b1", "MapB1",
            "tab-1f", "Map1F",
            "tab-2f", "Map",
        };

        static readonly string[] FloorOrder = { "MapB1", "Map1F", "Map" };

        static readonly string[] HudStates = { "Hud", "HudCritical", "HudHiding", "HudPursuit" };

        static readonly string[] ChapterWords =
        {
            "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX",
            "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN", "TWELVE",
        };

        static string s_LoadKicker;
        static string s_LoadTitle;

        static bool s_NewGame;

        static string s_DiffName;
        static string s_DiffBody;
        static string s_DiffThreat;
        static int s_DiffFilled = -1;

        static string s_OverwriteKicker;
        static string s_OverwriteTitle;
        static string s_OverwriteMeta;
        static int s_OverwriteSlot = -1;

        static bool s_OverwriteFresh;

        static int s_PendingWrite = -1;

        static string s_CombineClass;
        static string s_CombineTitle;

        static readonly string[] s_QuickItems = new string[QuickSlots];
        static int s_QuickSlot;

        const string DifficultyWarning = "this cannot be lowered later.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_LoadKicker = null;
            s_LoadTitle = null;
            s_NewGame = false;
            s_DiffName = null;
            s_DiffBody = null;
            s_DiffThreat = null;
            s_DiffFilled = -1;
            s_OverwriteKicker = null;
            s_OverwriteTitle = null;
            s_OverwriteMeta = null;
            s_OverwriteSlot = -1;
            s_OverwriteFresh = false;
            s_PendingWrite = -1;
            s_CombineClass = null;
            s_CombineTitle = null;
            System.Array.Clear(s_QuickItems, 0, s_QuickItems.Length);
            s_QuickSlot = 0;
            s_WalkInstalled = false;
            s_WalkStopped = false;
            s_WalkStep = 0;
            s_WalkExpect = null;
        }

        public static void Wire(VisualElement root, string slug, HcfDemoDirector director)
        {
            if (root == null || director == null)
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
                case "BootSplash":
                    WireBootSplash(root, director);
                    break;
                case "MainMenu":
                    WireMainMenu(root, director);
                    break;
                case "DifficultySelect":
                    WireDifficultySelect(root, director);
                    break;
                case "LoadSave":
                    WireLoadSave(root, director);
                    break;
                case "LoadEmpty":
                    WireLoadEmpty(root, director);
                    break;
                case "SaveOverwrite":
                    WireSaveOverwrite(root, director);
                    break;
                case "SettingsVideo":
                case "SettingsAudio":
                case "SettingsControls":
                case "SettingsGameplay":
                    WireSettings(root, director, slug);
                    break;
                case "Loading":
                    WireLoading(root, director);
                    break;
                case "Hud":
                case "HudCritical":
                case "HudHiding":
                case "HudPursuit":
                    WireHud(root, director, slug);
                    break;
                case "Prompts":
                    WirePrompts(root, director);
                    break;
                case "Objectives":
                    WireObjectives(root, director);
                    break;
                case "PauseMenu":
                    WirePauseMenu(root, director);
                    break;
                case "PauseSettings":
                    WirePauseSettings(root, director);
                    break;
                case "Inventory":
                    WireInventory(root, director);
                    break;
                case "InventoryEmpty":
                    WireInventoryEmpty(root, director);
                    break;
                case "Combine":
                    WireCombine(root, director);
                    break;
                case "Examine":
                    WireExamine(root, director);
                    break;
                case "Journal":
                    WireJournal(root, director);
                    break;
                case "JournalEmpty":
                    WireJournalEmpty(root, director);
                    break;
                case "Map":
                case "Map1F":
                case "MapB1":
                    WireMap(root, director, slug);
                    break;
                case "GameOver":
                    WireGameOver(root, director);
                    break;
                case "ConfirmDialog":
                    WireConfirmDialog(root, director);
                    break;
                case "QuitDialog":
                    WireQuitDialog(root, director);
                    break;
                case "DifficultyDialog":
                    WireDifficultyDialog(root, director);
                    break;
                case "OverwriteDialog":
                    WireOverwriteDialog(root, director);
                    break;
            }
        }

        static void WireBootSplash(VisualElement root, HcfDemoDirector director)
        {
            bool leaving = false;
            System.Action go = () =>
            {
                if (leaving)
                {
                    return;
                }

                leaving = true;
                ToMainMenu(director);
            };

            root.RegisterCallback<ClickEvent>(evt => go());
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (IsDirectorKey(director, evt.keyCode))
                {
                    return;
                }

                go();
                evt.StopPropagation();
            });
        }

        static void WireMainMenu(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            System.Action quit = () => director.GoTo("QuitDialog");
            WireMenu(root, keys,
                () => director.GoTo("Loading"),
                () => director.GoTo("DifficultySelect"),
                () => director.GoTo("LoadSave"),
                () => director.GoTo("SettingsVideo"),
                quit);
            keys.Bind(KeyCode.Escape, quit);

            keys.Bind(KeyCode.B, () => director.GoTo("BootSplash"));
        }

        static void WireDifficultySelect(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            List<VisualElement> cards = HcfScreenInteractions.Collect(root, MenuPrefix);
            System.Action[] actions = new System.Action[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                VisualElement card = cards[i];
                actions[i] = () =>
                {
                    SetDifficultyFromCard(card);
                    director.GoTo("DifficultyDialog");
                };
            }

            WireMenu(root, keys, actions);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireLoadSave(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            System.Action load = () =>
            {
                if (HcfSpecimenDemo.SelectedSaveIsEmpty(root))
                {
                    Flash(root.Q<VisualElement>("btn-load"));
                    return;
                }

                SetLoadingFromSave(HcfSpecimenDemo.SelectedSaveTitle(root));
                director.GoTo("Loading");
            };

            OnClick(root, "btn-load", load);
            BindSubmit(keys, root, load, MenuPrefix);
            keys.Bind(KeyCode.Delete, () => HcfSpecimenDemo.DeleteSelectedSave(root));
            keys.Bind(KeyCode.Backspace, () => HcfSpecimenDemo.DeleteSelectedSave(root));
            keys.Bind(KeyCode.Escape, director.Back);

            OnClick(root, "btn-overwrite", () => director.GoTo("SaveOverwrite"));

            keys.Bind(KeyCode.E, () => director.GoTo("LoadEmpty"));
            keys.Bind(KeyCode.S, () => director.GoTo("SaveOverwrite"));
        }

        static void WireLoadEmpty(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            OnClick(root, "btn-new-game", () => director.GoTo("DifficultySelect"));
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireSaveOverwrite(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);

            if (s_PendingWrite >= 0)
            {
                int slot = s_PendingWrite;
                s_PendingWrite = -1;
                HcfSpecimenDemo.WriteSlotByIndex(root, slot);
            }

            System.Action ask = () =>
            {
                if (!HcfSpecimenDemo.AskBeforeWrite(root))
                {
                    HcfSpecimenDemo.WriteSelectedSave(root);
                    return;
                }

                director.GoTo("OverwriteDialog");
            };

            OnClick(root, "btn-overwrite", ask);
            BindSubmit(keys, root, ask, MenuPrefix);
            OnClick(root, "btn-cancel", director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireQuitDialog(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            System.Action quit = () =>
            {
                Flash(root.Q<VisualElement>("btn-quit-confirm"));
                Application.Quit();
            };

            OnClick(root, "btn-quit-confirm", quit);
            OnClick(root, "btn-quit-cancel", director.Back);
            BindSubmit(keys, root, quit);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireDifficultyDialog(VisualElement root, HcfDemoDirector director)
        {
            ApplyDifficulty(root);

            KeyBinder keys = new KeyBinder(root);
            System.Action begin = () =>
            {
                s_NewGame = true;
                director.GoTo("Loading");
            };

            OnClick(root, "btn-diff-begin", begin);
            OnClick(root, "btn-diff-back", director.Back);
            BindSubmit(keys, root, begin);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireOverwriteDialog(VisualElement root, HcfDemoDirector director)
        {
            ApplyOverwriteTarget(root);

            KeyBinder keys = new KeyBinder(root);
            System.Action confirm = () =>
            {
                if (s_OverwriteSlot >= 0)
                {
                    s_PendingWrite = s_OverwriteSlot;
                }

                ClearOverwriteTarget();
                director.Back();
            };

            System.Action cancel = () =>
            {
                ClearOverwriteTarget();
                director.Back();
            };

            OnClick(root, "btn-overwrite-confirm", confirm);
            OnClick(root, "btn-overwrite-cancel", cancel);
            BindSubmit(keys, root, confirm);
            keys.Bind(KeyCode.Escape, cancel);
        }

        static void WireSettings(VisualElement root, HcfDemoDirector director, string self)
        {
            KeyBinder keys = new KeyBinder(root);
            WireScreenTabs(root, director, self, SettingsTabs);
            OnClick(root, "btn-back", director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireLoading(VisualElement root, HcfDemoDirector director)
        {
            TakeNewGameChapter(root);
            ApplyLoadingTitle(root);

            ProgressBar bar = root.Q<ProgressBar>("progress-loading");
            if (bar != null)
            {
                bar.value = bar.lowValue;
            }

            float start = Time.time;
            IVisualElementScheduledItem tick = null;

            System.Action finish = () =>
            {
                if (tick == null)
                {
                    return;
                }

                tick.Pause();
                tick = null;
                // Loading starts the session, so the menu stack it came from must not
                // survive it - otherwise Escape on a gameplay screen walks back into a dialog.
                ToHud(director);
            };

            tick = root.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.time - start) / LoadingSeconds);
                if (bar != null)
                {
                    bar.value = Mathf.Lerp(bar.lowValue, bar.highValue, t);
                }

                if (t >= 1f)
                {
                    finish();
                }
            }).Every(LoadingTickMs);

            root.RegisterCallback<ClickEvent>(evt => finish());

            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (IsDirectorKey(director, evt.keyCode))
                {
                    return;
                }

                finish();
                evt.StopPropagation();
            });
        }

        static void WireHud(VisualElement root, HcfDemoDirector director, string self)
        {
            KeyBinder keys = new KeyBinder(root);
            keys.Bind(KeyCode.J, () => director.GoTo("Journal"));
            keys.Bind(KeyCode.M, () => director.GoTo("Map"));
            keys.Bind(KeyCode.I, () => director.GoTo("Inventory"));
            keys.Bind(KeyCode.O, () => director.GoTo("Objectives"));

            ApplyQuickSlots(root);

            if (string.CompareOrdinal(self, "Hud") == 0)
            {
                WireNotifications(root);
            }

            InstallStateWalk(root, director);

            HcfScreenInteractions.Selection cells = HcfScreenInteractions.GetSelection(root, CellPrefix);

            // The HUD is rebuilt from UXML on every show, so the highlighted slot is whichever
            // cell the markup marks active. Follow it instead of keeping the slot a previous
            // screen left behind: otherwise EQUIP writes into a cell that is not the lit one.
            if (cells != null && cells.index >= 0)
            {
                s_QuickSlot = cells.index;
            }

            for (int i = 0; i < QuickSlots; i++)
            {
                int slot = i;
                keys.Bind(KeyCode.Alpha1 + i, () => SelectQuickSlot(cells, slot));
                keys.Bind(KeyCode.Keypad1 + i, () => SelectQuickSlot(cells, slot));
            }

            for (int i = 0; i < HudStates.Length; i++)
            {
                string state = HudStates[i];
                if (string.CompareOrdinal(state, self) == 0)
                {
                    continue;
                }

                keys.Bind(KeyCode.F1 + i, () => director.GoToReplace(state));
            }

            if (string.CompareOrdinal(self, "HudHiding") == 0)
            {
                keys.Bind(KeyCode.E, () => director.GoToReplace("Hud"));
            }

            keys.Bind(KeyCode.Escape, () => director.GoTo("PauseMenu"));
        }

        static void SelectQuickSlot(HcfScreenInteractions.Selection cells, int index)
        {
            if (cells == null || index < 0 || index >= cells.items.Count)
            {
                return;
            }

            s_QuickSlot = index;
            cells.Select(index);
            cells.items[index].Focus();
        }

        static void EquipToQuickSlot(string itemClass)
        {
            if (string.IsNullOrEmpty(itemClass))
            {
                return;
            }

            for (int i = 0; i < s_QuickItems.Length; i++)
            {
                if (string.CompareOrdinal(s_QuickItems[i], itemClass) == 0)
                {
                    s_QuickItems[i] = null;
                }
            }

            int slot = s_QuickSlot < 0 || s_QuickSlot >= s_QuickItems.Length ? 0 : s_QuickSlot;
            s_QuickItems[slot] = itemClass;
        }

        static void ApplyQuickSlots(VisualElement root)
        {
            for (int i = 0; i < s_QuickItems.Length; i++)
            {
                VisualElement slot = root.Q<VisualElement>(CellPrefix + (i + 1).ToString("00"));
                VisualElement art = root.Q<VisualElement>("art-" + (i + 1).ToString("00"));
                if (art == null)
                {
                    continue;
                }

                string itemClass = s_QuickItems[i];
                bool equipped = !string.IsNullOrEmpty(itemClass);
                HcfSpecimenDemo.SetItemClass(art, itemClass);
                art.EnableInClassList("s07-slot-art--on", equipped);
                if (slot != null)
                {
                    slot.EnableInClassList("s07-slot--equipped", equipped);
                }
            }
        }

        static readonly string[] WalkSlugs =
        {
            "Hud", "Prompts", "HudPursuit", "HudCritical", "HudHiding",
        };

        static readonly long[] WalkHoldMs = { 26000, 9000, 5000, 5000, 5000 };

        const long WalkTickMs = 250;

        static bool s_WalkInstalled;

        static bool s_WalkStopped;
        static int s_WalkStep;

        static string s_WalkExpect;

        static readonly string[] NotifyItems =
        {
            "notify-pickup",
            "notify-document",
            "notify-autosave",
            "notify-objective",
            "notify-chapter",
        };

        static readonly string[][] NotifyScenes =
        {
            new[] { "notify-pickup" },
            new[] { "notify-pickup", "notify-document" },
            new[] { "notify-pickup", "notify-document" },
            new string[0],
            new[] { "notify-autosave" },
            new[] { "notify-autosave" },
            new string[0],
            new[] { "notify-objective" },
            new[] { "notify-objective" },
            new string[0],
            new[] { "notify-chapter" },
            new[] { "notify-chapter" },
            new[] { "notify-chapter" },
            new string[0],
        };

        const string NotifyOnClass = "s07-notify-item--on";

        const long NotifyStepMs = 1600;

        static void InstallStateWalk(VisualElement root, HcfDemoDirector director)
        {
            if (s_WalkInstalled || s_WalkStopped || !director.demoStateWalk)
            {
                return;
            }

            IPanel panel = root.panel;
            VisualElement host = panel == null ? null : panel.visualTree;
            if (host == null)
            {
                return;
            }

            s_WalkInstalled = true;
            s_WalkStep = 0;
            s_WalkExpect = director.CurrentSlug;

            float due = Time.realtimeSinceStartup + WalkHoldMs[0] / 1000f;
            IVisualElementScheduledItem tick = null;
            System.Action stop = null;
            tick = host.schedule.Execute(() =>
            {
                if (s_WalkStopped
                    || string.CompareOrdinal(director.CurrentSlug, s_WalkExpect) != 0)
                {
                    stop();
                    return;
                }

                if (Time.realtimeSinceStartup < due)
                {
                    return;
                }

                s_WalkStep = (s_WalkStep + 1) % WalkSlugs.Length;
                s_WalkExpect = WalkSlugs[s_WalkStep];
                due = Time.realtimeSinceStartup + WalkHoldMs[s_WalkStep] / 1000f;

                director.GoToReplace(s_WalkExpect);
            }).Every(WalkTickMs);

            EventCallback<PointerDownEvent> onPointerDown = null;
            EventCallback<KeyDownEvent> onKeyDown = null;
            EventCallback<NavigationMoveEvent> onNavigationMove = null;
            EventCallback<NavigationSubmitEvent> onNavigationSubmit = null;
            System.Action unhook = () =>
            {
                host.UnregisterCallback(onPointerDown, TrickleDown.TrickleDown);
                host.UnregisterCallback(onKeyDown, TrickleDown.TrickleDown);
                host.UnregisterCallback(onNavigationMove, TrickleDown.TrickleDown);
                host.UnregisterCallback(onNavigationSubmit, TrickleDown.TrickleDown);
            };

            stop = () =>
            {
                s_WalkStopped = true;
                tick.Pause();
                unhook();
            };

            int installFrame = Time.frameCount;
            System.Action stopFromInput = () =>
            {
                if (Time.frameCount != installFrame)
                {
                    stop();
                }
            };

            onPointerDown = evt => stopFromInput();
            onKeyDown = evt => stopFromInput();
            onNavigationMove = evt => stopFromInput();
            onNavigationSubmit = evt => stopFromInput();

            host.RegisterCallback(onPointerDown, TrickleDown.TrickleDown);
            host.RegisterCallback(onKeyDown, TrickleDown.TrickleDown);
            host.RegisterCallback(onNavigationMove, TrickleDown.TrickleDown);
            host.RegisterCallback(onNavigationSubmit, TrickleDown.TrickleDown);

            EventCallback<DetachFromPanelEvent> detach = null;
            detach = evt =>
            {
                host.UnregisterCallback(detach);
                tick.Pause();
                unhook();
                s_WalkInstalled = false;
            };

            host.RegisterCallback(detach);
        }

        static void WireNotifications(VisualElement root)
        {
            VisualElement layer = root.Q<VisualElement>("notifications");
            if (layer == null)
            {
                return;
            }

            int step = 0;
            root.schedule.Execute(() =>
            {
                string[] scene = NotifyScenes[step % NotifyScenes.Length];
                for (int i = 0; i < NotifyItems.Length; i++)
                {
                    VisualElement item = layer.Q<VisualElement>(NotifyItems[i]);
                    if (item == null)
                    {
                        continue;
                    }

                    bool show = false;
                    for (int j = 0; j < scene.Length; j++)
                    {
                        if (string.CompareOrdinal(scene[j], NotifyItems[i]) == 0)
                        {
                            show = true;
                            break;
                        }
                    }

                    item.EnableInClassList(NotifyOnClass, show);
                }

                step++;
            }).Every(NotifyStepMs).StartingIn(NotifyStepMs);
        }

        static void SetCombineFirst(string itemClass, string title)
        {
            s_CombineClass = itemClass;
            s_CombineTitle = title;
        }

        public static bool TakeCombineFirst(out string itemClass, out string title)
        {
            itemClass = s_CombineClass;
            title = s_CombineTitle;
            s_CombineClass = null;
            s_CombineTitle = null;

            return !string.IsNullOrEmpty(itemClass);
        }

        static void WirePrompts(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            // Prompts is a gameplay screen reached without a push, so Escape opens the
            // pause menu here exactly as it does on the HUD instead of unwinding history.
            keys.Bind(KeyCode.Escape, () => director.GoTo("PauseMenu"));
        }

        static void WireObjectives(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            keys.Bind(KeyCode.J, () => director.GoTo("Journal"));
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WirePauseMenu(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            WireMenu(root, keys,
                director.Back,
                () => director.GoTo("SaveOverwrite"),
                () => director.GoTo("Journal"),
                () => director.GoTo("PauseSettings"),
                () => director.GoTo("ConfirmDialog"));
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WirePauseSettings(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            WireMenu(root, keys,
                () => ToHud(director),
                () => director.GoTo("SaveOverwrite"),
                () => director.GoTo("Journal"),
                null,
                () => director.GoTo("ConfirmDialog"));
            WirePauseTabs(root);
            OnClick(root, "btn-back", director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WirePauseTabs(VisualElement root)
        {
            Label note = root.Q<Label>("tabs-note");
            for (int i = 0; i + 2 < PauseTabs.Length; i += 3)
            {
                string group = PauseTabs[i + 1];
                string text = PauseTabs[i + 2];
                OnClick(root, PauseTabs[i], () => ShowPauseTab(root, note, group, text));
            }

            BindTabCycle(root);
        }

        static void ShowPauseTab(VisualElement root, Label note, string group, string text)
        {
            for (int i = 1; i < PauseTabs.Length; i += 3)
            {
                VisualElement options = root.Q<VisualElement>(PauseTabs[i]);
                if (options != null)
                {
                    options.EnableInClassList(OptionsOffClass,
                        string.CompareOrdinal(PauseTabs[i], group) != 0);
                }
            }

            if (note != null)
            {
                note.text = text;
                note.EnableInClassList(NoteOffClass, text.Length == 0);
            }
        }

        static void WireInventory(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            OnClick(root, "btn-examine", () => director.GoTo("Examine"));

            OnClick(root, "btn-combine", () =>
            {
                SetCombineFirst(HcfSpecimenDemo.SelectedItemClass(root),
                    HcfSpecimenDemo.SelectedItemTitle(root));
                director.GoTo("Combine");
            });

            OnClick(root, "btn-equip", () =>
                EquipToQuickSlot(HcfSpecimenDemo.SelectedItemClass(root)));

            keys.Bind(KeyCode.E, () => director.GoTo("InventoryEmpty"));
            keys.Bind(KeyCode.I, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireInventoryEmpty(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            keys.Bind(KeyCode.I, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireCombine(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            OnClick(root, "btn-cancel", director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireExamine(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            keys.Bind(KeyCode.Space, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireJournal(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            keys.Bind(KeyCode.Space, () => HcfSpecimenDemo.NextJournalPage(root));
            keys.Bind(KeyCode.E, () => director.GoTo("JournalEmpty"));
            keys.Bind(KeyCode.J, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireJournalEmpty(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            keys.Bind(KeyCode.J, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void WireMap(VisualElement root, HcfDemoDirector director, string self)
        {
            KeyBinder keys = new KeyBinder(root);
            WireScreenTabs(root, director, self, MapFloors);
            keys.Bind(KeyCode.Q, () => StepFloor(director, self, -1));
            keys.Bind(KeyCode.E, () => StepFloor(director, self, 1));
            keys.Bind(KeyCode.M, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void StepFloor(HcfDemoDirector director, string self, int delta)
        {
            int index = System.Array.IndexOf(FloorOrder, self);
            if (index < 0)
            {
                return;
            }

            int next = Mathf.Clamp(index + delta, 0, FloorOrder.Length - 1);
            if (next == index)
            {
                return;
            }

            director.GoToReplace(FloorOrder[next]);
        }

        static void WireGameOver(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            WireMenu(root, keys,
                () => director.GoTo("Loading"),
                () => director.GoTo("LoadSave"),
                () => ToMainMenu(director));
            keys.Bind(KeyCode.Escape, () => ToMainMenu(director));
        }

        static void WireConfirmDialog(VisualElement root, HcfDemoDirector director)
        {
            KeyBinder keys = new KeyBinder(root);
            OnClick(root, "btn-keep", director.Back);
            OnClick(root, "btn-abandon", () => director.GoTo("GameOver"));
            BindSubmit(keys, root, director.Back);
            keys.Bind(KeyCode.Escape, director.Back);
        }

        static void ToMainMenu(HcfDemoDirector director)
        {
            director.ClearHistory();
            director.GoToReplace("MainMenu");
        }

        static void ToHud(HcfDemoDirector director)
        {
            director.ClearHistory();
            director.GoToReplace("Hud");
        }

        static void TakeNewGameChapter(VisualElement root)
        {
            if (!s_NewGame)
            {
                return;
            }

            s_NewGame = false;
            Label kicker = root.Q<Label>(null, "s06-chapter__data-first-kicker");
            Label title = root.Q<Label>(null, "s06-chapter__data-first-title");
            if (kicker != null)
            {
                s_LoadKicker = kicker.text;
            }

            if (title != null)
            {
                s_LoadTitle = title.text;
            }
        }

        static void SetLoadingFromSave(string slotTitle)
        {
            if (string.IsNullOrEmpty(slotTitle))
            {
                return;
            }

            int dash = slotTitle.IndexOf('—');
            if (dash < 0)
            {
                s_LoadKicker = null;
                s_LoadTitle = slotTitle.Trim();
                return;
            }

            s_LoadKicker = SpellChapter(slotTitle.Substring(0, dash).Trim());
            s_LoadTitle = slotTitle.Substring(dash + 1).Trim();
        }

        static string SpellChapter(string chapter)
        {
            int start = -1;
            int end = -1;
            for (int i = 0; i < chapter.Length; i++)
            {
                if (char.IsDigit(chapter[i]))
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

            int number;
            if (start < 0
                || !int.TryParse(chapter.Substring(start, end - start + 1), out number)
                || number < 1 || number > ChapterWords.Length)
            {
                return chapter;
            }

            return chapter.Substring(0, start) + ChapterWords[number - 1] + chapter.Substring(end + 1);
        }

        static void ApplyLoadingTitle(VisualElement root)
        {
            string kicker = s_LoadKicker;
            string title = s_LoadTitle;
            s_LoadKicker = null;
            s_LoadTitle = null;

            Label kickerLabel = root.Q<Label>("chapter-kicker");
            if (kickerLabel != null && !string.IsNullOrEmpty(kicker))
            {
                kickerLabel.text = kicker;
            }

            VisualElement titleBlock = root.Q<VisualElement>("chapter-title");
            if (titleBlock == null || string.IsNullOrEmpty(title))
            {
                return;
            }

            titleBlock.Query<Label>().ForEach(line => line.text = title);
        }

        static void SetDifficultyFromCard(VisualElement card)
        {
            Label name = card.Q<Label>(null, "s02-card__name");
            Label desc = card.Q<Label>(null, "s02-card__desc");
            s_DiffName = name == null ? null : name.text;
            s_DiffBody = desc == null ? null : desc.text;

            List<Label> keys = card.Query<Label>(null, "s02-card__meter-key").ToList();
            s_DiffThreat = keys.Count > 1 ? keys[1].text : null;

            s_DiffFilled = 0;
            card.Query<VisualElement>(null, "s02-bar--on").ForEach(bar => s_DiffFilled++);
        }

        static void ApplyDifficulty(VisualElement root)
        {
            string name = s_DiffName;
            string body = s_DiffBody;
            string threat = s_DiffThreat;
            int filled = s_DiffFilled;
            s_DiffName = null;
            s_DiffBody = null;
            s_DiffThreat = null;
            s_DiffFilled = -1;

            SetLabel(root, "difficulty-name", string.IsNullOrEmpty(name) ? null : name + "?");

            SetLabel(root, "difficulty-body",
                string.IsNullOrEmpty(body) ? null : body + " " + DifficultyWarning);
            SetLabel(root, "threat-level", threat);

            UI.HcfSegmentedMeter meter = root.Q<UI.HcfSegmentedMeter>("meter-threat");
            if (meter != null && filled >= 0)
            {
                meter.filled = filled;
            }
        }

        public static void SetOverwriteTarget(int index, string slotNumber, string title, string meta)
        {
            s_OverwriteSlot = index;
            s_OverwriteKicker = string.IsNullOrEmpty(slotNumber) ? null : "OVERWRITE SLOT " + slotNumber;
            s_OverwriteTitle = title;
            s_OverwriteMeta = meta;
            s_OverwriteFresh = true;
        }

        static void ClearOverwriteTarget()
        {
            s_OverwriteSlot = -1;
            s_OverwriteKicker = null;
            s_OverwriteTitle = null;
            s_OverwriteMeta = null;
            s_OverwriteFresh = false;
        }

        static void ApplyOverwriteTarget(VisualElement root)
        {
            if (!s_OverwriteFresh)
            {
                ClearOverwriteTarget();
                return;
            }

            SetLabel(root, "overwrite-slot", s_OverwriteKicker);
            SetLabel(root, "file-title", s_OverwriteTitle);
            SetLabel(root, "file-meta", s_OverwriteMeta);
            s_OverwriteKicker = null;
            s_OverwriteTitle = null;
            s_OverwriteMeta = null;
            s_OverwriteFresh = false;
        }

        static void SetLabel(VisualElement root, string name, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Label label = root.Q<Label>(name);
            if (label != null)
            {
                label.text = text;
            }
        }

        static void WireMenu(VisualElement root, KeyBinder keys, params System.Action[] actions)
        {
            List<VisualElement> items = HcfScreenInteractions.Collect(root, MenuPrefix);
            Dictionary<VisualElement, System.Action> map = new Dictionary<VisualElement, System.Action>();

            if (items.Count != actions.Length)
            {
                Debug.LogWarning("[HCF] " + MenuPrefix + "*: " + items.Count
                    + " items in the markup, " + actions.Length
                    + " in the transition graph - the graph and the screen disagree.");
            }

            for (int i = 0; i < items.Count && i < actions.Length; i++)
            {
                System.Action action = actions[i];
                if (action == null)
                {
                    continue;
                }

                Button button = items[i] as Button;
                if (button == null)
                {
                    continue;
                }

                map[button] = action;
                button.clicked += () => action();
            }

            keys.Bind(KeyCode.Return, () => ActivateIfUnfocused(root, map));
            keys.Bind(KeyCode.KeypadEnter, () => ActivateIfUnfocused(root, map));
        }

        static void BindSubmit(KeyBinder keys, VisualElement root, System.Action action)
        {
            BindSubmit(keys, root, action, null);
        }

        // Enter belongs to the focused button, so the screen action only fires when no
        // button holds focus. Screens whose selection items are inert pass their prefix:
        // a save slot only picks, it has no click action, so Enter on it still acts.
        static void BindSubmit(KeyBinder keys, VisualElement root, System.Action action, string inertPrefix)
        {
            System.Action fallback = () =>
            {
                if (!IsSubmitButtonFocused(root, inertPrefix))
                {
                    action();
                }
            };

            keys.Bind(KeyCode.Return, fallback);
            keys.Bind(KeyCode.KeypadEnter, fallback);
        }

        static bool IsButtonFocused(VisualElement root)
        {
            FocusController focus = root.focusController;
            return focus != null && focus.focusedElement is Button;
        }

        static bool IsSubmitButtonFocused(VisualElement root, string inertPrefix)
        {
            FocusController focus = root.focusController;
            Button button = focus == null ? null : focus.focusedElement as Button;
            if (button == null)
            {
                return false;
            }

            return string.IsNullOrEmpty(inertPrefix)
                || string.IsNullOrEmpty(button.name)
                || !button.name.StartsWith(inertPrefix, System.StringComparison.Ordinal);
        }

        static void ActivateIfUnfocused(VisualElement root, Dictionary<VisualElement, System.Action> map)
        {
            if (IsButtonFocused(root))
            {
                return;
            }

            foreach (KeyValuePair<VisualElement, System.Action> pair in map)
            {
                if (pair.Key.ClassListContains(ActiveClass))
                {
                    pair.Value();
                    return;
                }
            }
        }

        static void WireScreenTabs(VisualElement root, HcfDemoDirector director, string self, string[] pairs)
        {
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                string slug = pairs[i + 1];
                if (string.CompareOrdinal(slug, self) == 0)
                {
                    continue;
                }

                OnClick(root, pairs[i], () => director.GoToReplace(slug));
            }

            BindTabCycle(root);
        }

        static void BindTabCycle(VisualElement root)
        {
            root.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction != NavigationMoveEvent.Direction.Next
                    && evt.direction != NavigationMoveEvent.Direction.Previous)
                {
                    return;
                }

                HcfScreenInteractions.CycleTabs(root);
                evt.StopPropagation();
                if (root.focusController != null)
                {
                    root.focusController.IgnoreEvent(evt);
                }
            }, TrickleDown.TrickleDown);
        }

        public static void OnClick(VisualElement root, string name, System.Action action)
        {
            Button button = root.Q<Button>(name);
            if (button == null)
            {
                return;
            }

            button.clicked += () => action();
        }

        public static void Flash(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.AddToClassList(FlashClass);
            element.schedule.Execute(() => element.RemoveFromClassList(FlashClass)).ExecuteLater(FlashMs);
        }

        // Keys the director claims for paging and grain. With demoHotkeys off the director
        // ignores them, so the screen controllers must take them like any other key.
        static bool IsDirectorKey(HcfDemoDirector director, KeyCode key)
        {
            if (!director.demoHotkeys)
            {
                return false;
            }

            switch (key)
            {
                case KeyCode.None:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                case KeyCode.Equals:
                    return true;
                default:
                    return false;
            }
        }

        sealed class KeyBinder
        {
            readonly Dictionary<KeyCode, System.Action> m_Actions = new Dictionary<KeyCode, System.Action>();
            int m_LastFrame = -1;

            public KeyBinder(VisualElement root)
            {
                root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }

            public void Bind(KeyCode key, System.Action action)
            {
                m_Actions[key] = action;
            }

            void OnKeyDown(KeyDownEvent evt)
            {
                System.Action action;
                if (!m_Actions.TryGetValue(evt.keyCode, out action) || Time.frameCount == m_LastFrame)
                {
                    return;
                }

                m_LastFrame = Time.frameCount;
                action();
                evt.StopPropagation();
            }
        }
    }
}
