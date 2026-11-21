using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // The menus (2026-10 redesign): one panel for every list, sub-menus that go back to where they came from
    // (Esc / "← 뒤로"), what fits the moment at the top of the main menu, things not open yet shown with the reason,
    // and the new pages: the photo album, our records, the settings.
    public partial class PeaceDirector
    {
        class MenuFrame { public string Title; public Action Reopen; }

        readonly List<MenuFrame> menuStack = new List<MenuFrame>();
        MenuFrame choosingFrom;      // set while a menu's choice runs: a menu opened from it is its sub-menu
        bool reopening;              // going back: the stack below stays
        bool refreshing;             // the same menu shown again (a setting changed): no entrance
        string shownMenu;            // the title of the menu last shown (callbacks that come later check it is still that one)
        List<string> lastMenuOpts;
        readonly Dictionary<string, int> lastPick = new Dictionary<string, int>();

        static bool IsExitOption(string o) => o == "닫기" || o == "그만두기" || o == "그냥 두기" || o == "그만할래" || o == "조금 더 있을래";

        [HideFromIl2Cpp] void ShowMenu(string title, List<string> opts, List<Action> acts)
            => ShowItems(title, opts.Select(o => new MenuItem(o) { Back = IsExitOption(o) }).ToList(), acts, null);

        // the menu; reopen = how to show it again when coming back to it (default: the same rows)
        [HideFromIl2Cpp] void ShowItems(string title, List<MenuItem> items, List<Action> acts, Action reopen)
        {
            int dir = choosingFrom != null ? 1 : reopening ? (refreshing ? 2 : -1) : 0;
            shownMenu = title;
            if (choosingFrom != null) menuStack.Add(choosingFrom);
            else if (!reopening) menuStack.Clear();
            choosingFrom = null;
            bool sub = menuStack.Count > 0;
            // the leave-it row: "← 뒤로" inside a sub-menu, its own words otherwise
            foreach (var it in items) if (it.Back && sub && (it.Label == "그만두기" || it.Label == "닫기")) it.Label = "뒤로";
            lastMenuOpts = items.Select(i => i.Label).ToList();
            pendingMenu = true;
            HoldControls(true);
            mitaLook?.LookOnPlayer();
            var frame = new MenuFrame { Title = title };
            frame.Reopen = reopen ?? (() => ShowItems(title, items, acts, null));
            string crumb = string.Join("  ›  ", menuStack.Select(f => Loc.T(f.Title)));
            ui.OpenPanel(Loc.T(title), crumb, items, i =>
            {
                pendingMenu = false;
                lastPick[title] = i;
                choosingFrom = frame;
                try { acts[i](); }
                finally { choosingFrom = null; }
                if (!ui.MenuOpen) menuStack.Clear();
            }, MenuBack, dir);
            // coming back to a menu (from a sub-menu, or a setting changed): the row you were on stays picked
            if (reopening && lastPick.TryGetValue(title, out var at)) ui.PanelSelect(at);
        }

        void MenuBack()
        {
            pendingMenu = false;
            if (menuStack.Count == 0) return;   // (closed)
            var parent = menuStack[menuStack.Count - 1];
            menuStack.RemoveAt(menuStack.Count - 1);
            reopening = true;
            try { parent.Reopen(); }
            finally { reopening = false; }
        }

        void CloseMenus()
        {
            ui.CloseMenu();
            ui.CloseCard();
            cardKind = CardKind.None;
            menuStack.Clear();
        }

        // ---------------------------------------------------------------- the main menu

        void OpenMainMenu()
        {
            if (activity == MitaActivity.Sleeping) { OpenSleepingMenu(); return; }
            if (RequestBack()) return;   // back from her favour: that comes first
            FaceYouForMenu();
            var items = new List<MenuItem>();
            var acts = new List<Action>();
            void Add(MenuItem it, Action a) { items.Add(it); acts.Add(a); }
            string now = Loc.T("지금");

            // what fits the moment, first
            if (OnSofaAlone) Add(new MenuItem("옆에 앉기") { Now = true, Hint = now }, JoinHerOnSofa);
            if (cooking) Add(new MenuItem("요리 그만하기") { Now = true, Hint = now }, StopCooking);
            else if (MealTime() && S.LastMeal != MealKey())
            {
                if (activity != MitaActivity.MealReady) Add(new MenuItem("같이 요리하기") { Now = true, Hint = Loc.T("식사 시간") }, DoCook);
                Add(new MenuItem("같이 밥 먹기") { Now = true, Hint = Loc.T("식사 시간") }, DoMeal);
            }
            if (IsNight()) Add(new MenuItem("이제 자자") { Now = true, Hint = Loc.T("밤") }, DoSleep);

            Add(new MenuItem("대화하기") { Sub = true }, OpenTalkMenu);
            Add(new MenuItem("교감하기") { Sub = true }, OpenTouchMenu);
            Add(new MenuItem("같이 놀자") { Sub = true }, OpenPlayMenu);
            Add(new MenuItem("함께 지내기") { Sub = true, Hint = following ? Loc.T("같이 다니는 중") : null }, OpenLifeMenu);
            bool momentsOpen = S.Affection >= 15;
            Add(new MenuItem("특별한 시간") { Sub = true, Enabled = momentsOpen, Hint = momentsOpen ? null : Loc.T("더 친해지면 열려요"), Badge = momentsOpen && !S.UiSeen.Contains("moments") ? "NEW" : null },
                () => { MarkSeen("moments"); OpenMomentsMenu(); });
            if (!string.IsNullOrEmpty(Pn.SpecialName)) Add(new MenuItem(Pn.SpecialName), DoSpecial);
            int newPhotos = NewPhotoCount();
            Add(new MenuItem("추억") { Sub = true, Badge = newPhotos > 0 ? newPhotos.ToString() : null }, OpenMemoriesMenu);
            Add(new MenuItem("설정") { Sub = true, Badge = S.UiSeen.Contains("settings") ? null : "NEW" }, () => { MarkSeen("settings"); OpenSettingsMenu(); });
            Add(new MenuItem("오늘은 여기까지…"), OpenLeaveMenu);
            Add(new MenuItem("닫기") { Back = true }, () => { });
            ShowItems(Pn.Short, items, acts, OpenMainMenu);
        }

        // you called her: she stops what she was doing (looking at a shelf, wandering) and turns to you while you choose
        void FaceYouForMenu()
        {
            if (!AttentionFree || following || busy || !PlayerNearMita(4f, 360f)) return;
            try
            {
                attentiveUntil = Time.time + 8f;
                if (!attentiveHold) { attentiveHold = true; HoldAddon(true); }
                mita.AiShraplyStop();
                mitaLook.Activation(true);
                mitaLook.LookOnPlayerAndRotate();
                var idle = (skin != null && !skin.OwnAnimator ? skin.IdleClip : null) ?? Clip("Mita Idle") ?? originalIdle;
                if (idle != null && mitaAnim != null && Time.time >= gestureUntil) mitaAnim.AnimationClipSimpleNext(idle);
            }
            catch (Exception e) { Plugin.Log.LogWarning("[menu] face you: " + e.Message); }
        }

        void OpenTouchMenu()
        {
            var items = new List<MenuItem> { new MenuItem("쓰다듬기"), new MenuItem("안아주기"), new MenuItem("칭찬하기") };
            var acts = new List<Action> { DoPat, DoHug, DoCompliment };
            if (glassesTemplate != null && Pn.Id != "mila" && Pn.Id != "chibi") { items.Add(new MenuItem(GlassesWorn ? "안경 벗기" : "안경 써 줘")); acts.Add(DoGlasses); }
            if (Pn.Id == "crazy" && skin == null) { items.Add(new MenuItem("옷 갈아입기") { Sub = true }); acts.Add(OpenClothesMenu); }
            else if (CanRecolor) { items.Add(new MenuItem("옷 갈아입기") { Sub = true }); acts.Add(OpenColorMenu); }   // (the others are their own models: new colours)
            items.Add(new MenuItem("선물하기") { Sub = true, Hint = GiftsLeftHint() }); acts.Add(OpenGiftMenu);
            items.Add(new MenuItem("그만두기") { Back = true }); acts.Add(() => { });
            ShowItems("교감하기", items, acts, OpenTouchMenu);
        }

        string GiftsLeftHint()
        {
            try { return S.GiftDay == S.Day && S.GiftsToday.Count > 0 ? Loc.F("오늘 {0}개", S.GiftsToday.Count) : null; } catch { return null; }
        }

        void OpenPlayMenu()
        {
            var items = new List<MenuItem>();
            var acts = new List<Action>();
            // Cappie's own games from the story come first with her
            if (DanceReady) { items.Add(new MenuItem("댄스 배틀") { Hint = Loc.T("원작 7장") }); acts.Add(StartDance); }
            if (HammerReady) { items.Add(new MenuItem("망치 게임") { Hint = Loc.T("원작 7장") }); acts.Add(StartHammer); }
            items.Add(new MenuItem("가위바위보") { Hint = S.RpsGames > 0 ? Loc.F("{0}승 {1}판", S.RpsWins, S.RpsGames) : null });
            acts.Add(() => Play(L.RpsStart, () => ShowMenu("가위바위보", new List<string> { "가위", "바위", "보" },
                new List<Action> { () => Rps(0), () => Rps(1), () => Rps(2) })));
            items.Add(new MenuItem("참참참")); acts.Add(StartCham);
            items.Add(new MenuItem("끝말잇기")); acts.Add(StartWordChain);
            items.Add(new MenuItem("넌센스 퀴즈")); acts.Add(StartRiddles);
            items.Add(new MenuItem("숨바꼭질")); acts.Add(StartHide);
            items.Add(new MenuItem("원작 미니게임") { Sub = true }); acts.Add(OpenStoryGamesMenu);
            items.Add(new MenuItem("같이 사진 찍기")); acts.Add(DoPhoto);
            items.Add(new MenuItem("그만두기") { Back = true }); acts.Add(() => { });
            ShowItems("같이 놀자", items, acts, OpenPlayMenu);
        }

        void OpenStoryGamesMenu()
        {
            var items = new List<MenuItem> { new MenuItem("카드 게임") { Hint = Loc.T("원작 4장") }, new MenuItem("TV 게임") { Hint = Loc.T("원작 4장") } };
            var acts = new List<Action> { () => Play(L.CardsInvite), () => Play(L.TvInvite) };
            items.Add(new MenuItem("그만두기") { Back = true }); acts.Add(() => { });
            ShowItems("원작 미니게임", items, acts, OpenStoryGamesMenu);
        }

        // her day with you: walking together, cooking, eating, the sofa, sleep
        void OpenLifeMenu()
        {
            var items = new List<MenuItem>();
            var acts = new List<Action>();
            items.Add(new MenuItem(following ? "여기서 기다려" : "같이 다니자")); acts.Add(() => { if (following) StopFollow(true); else StartFollow(); });
            bool meal = MealTime() && S.LastMeal != MealKey();
            string mealHint = Loc.T("아침 · 점심 · 저녁 시간에");
            if (cooking) { items.Add(new MenuItem("요리 그만하기") { Now = true }); acts.Add(StopCooking); }
            else { items.Add(new MenuItem("같이 요리하기") { Enabled = meal && activity != MitaActivity.MealReady, Hint = meal ? Loc.T("지금") : mealHint, Now = meal }); acts.Add(DoCook); }
            items.Add(new MenuItem("같이 밥 먹기") { Enabled = meal, Hint = meal ? Loc.T("지금") : (S.LastMeal == MealKey() ? Loc.T("이미 먹었어요") : mealHint), Now = meal }); acts.Add(DoMeal);
            if (OnSofaAlone) { items.Add(new MenuItem("옆에 앉기") { Now = true }); acts.Add(JoinHerOnSofa); }
            items.Add(new MenuItem("화분에 물 주기")
            {
                Now = !PlantWateredToday,
                Hint = PlantWateredToday ? Loc.T("오늘은 줬어요") : PlantWilted ? Loc.T("시들어 가요!") : S.PlantDays == 0 ? Loc.T("새로 키우기") : Loc.T(PlantStageNames[PlantStage]),
            });
            acts.Add(WaterPlant);
            if (IsNight()) { items.Add(new MenuItem("이제 자자") { Now = true, Hint = Loc.T("밤") }); acts.Add(DoSleep); }
            else if (Pn.NapAnytime) { items.Add(new MenuItem("같이 낮잠 자기")); acts.Add(DoSleep); }
            else { items.Add(new MenuItem("이제 자자") { Enabled = false, Hint = Loc.F("{0}시부터", Pn.NightHour) }); acts.Add(DoSleep); }
            items.Add(new MenuItem("그만두기") { Back = true }); acts.Add(() => { });
            ShowItems("함께 지내기", items, acts, OpenLifeMenu);
        }

        // ---------------------------------------------------------------- memories: the album, our records

        void OpenMemoriesMenu()
        {
            int n = PhotoFiles().Count, fresh = NewPhotoCount();
            var items = new List<MenuItem>
            {
                new MenuItem("사진 앨범") { Enabled = n > 0, Hint = n > 0 ? Loc.F("{0}장", n) : Loc.T("같이 사진을 찍어 보세요"), Badge = fresh > 0 ? Loc.F("새 사진 {0}", fresh) : null },
                new MenuItem("일기장") { Hint = Loc.F("{0}의 일기", ShortName) },
                new MenuItem("우리의 기록"),
            };
            var acts = new List<Action> { OpenAlbum, OpenDiary, OpenRecords };
            int goalsDone = Goals().Count(x => S.Goals.Contains(x.id)), goalsAll = Goals().Count;
            items.Add(new MenuItem("함께한 목표") { Hint = Loc.F("{0}/{1}", goalsDone, goalsAll), Badge = S.UiSeen.Contains("goals") ? null : Loc.T("NEW") });
            acts.Add(OpenGoals);
            if (AllGoalsDone) { items.Insert(0, new MenuItem(Loc.F("{0}의 편지", ShortName)) { Now = !S.Goals.Contains("#letter") }); acts.Insert(0, ReadLetter); }
            items.Add(new MenuItem("그만두기") { Back = true }); acts.Add(() => { });
            ShowItems("추억", items, acts, OpenMemoriesMenu);
        }

        enum CardKind { None, Album, Records, Diary }
        CardKind cardKind;
        List<string> albumFiles;
        int albumIndex;
        Texture2D albumTex;
        string albumRemark;

        static string PhotoDir => Path.Combine(Path.GetDirectoryName(BepInEx.Paths.GameRootPath + "/"), "PeaceModePhotos");

        void MarkSeen(string what) { if (S.UiSeen.Add(what)) S.Write(); }

        // photos taken since the album was last opened (the names sort by date)
        int NewPhotoCount()
        {
            var seen = S.AlbumSeen ?? "";
            return PhotoFiles().Count(f => string.CompareOrdinal(Path.GetFileName(f), seen) > 0);
        }

        static List<string> PhotoFiles()
        {
            try
            {
                if (!Directory.Exists(PhotoDir)) return new List<string>();
                return Directory.GetFiles(PhotoDir, "*.png").OrderByDescending(f => Path.GetFileName(f)).ToList();
            }
            catch { return new List<string>(); }
        }

        float uiErrorAt;
        MenuFrame cardReturn;   // the menu the album / the records were opened from

        void OpenAlbum()
        {
            cardReturn = choosingFrom ?? cardReturn;
            albumFiles = PhotoFiles();
            if (albumFiles.Count == 0) return;
            albumIndex = 0;
            S.AlbumSeen = Path.GetFileName(albumFiles[0]);   // (newest first)
            S.Write();
            cardKind = CardKind.Album;
            HoldControls(true);
            albumRemark = Pick(AlbumOpenRemarks());
            ShowAlbumPage();
        }

        string[] AlbumOpenRemarks() => new[] { "같이 찍은 사진들이에요. 하나씩 볼까요?", "우리 사진이 이만큼이나 쌓였네요!", "앨범 보는 거 좋아요. 그날 생각이 나거든요." };
        string[] AlbumPageRemarks() => new[] { "이날 기억나요!", "여기 저 잘 나왔죠?", "앗, 이 사진은 좀 부끄러운데…", "이 사진 제일 좋아요.", "다음에 또 찍어요!", "{0}님 표정이 궁금하네요. 사진엔 안 나오잖아요." };

        static string Pick(string[] a) => a[Random.Range(0, a.Length)];

        void ShowAlbumPage()
        {
            if (albumFiles == null || albumFiles.Count == 0) { CloseCardBack(); return; }
            albumIndex = (albumIndex % albumFiles.Count + albumFiles.Count) % albumFiles.Count;
            var file = albumFiles[albumIndex];
            if (albumTex != null) { Destroy(albumTex); albumTex = null; }
            try
            {
                var bytes = File.ReadAllBytes(file);
                albumTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                ImageConversion.LoadImage(albumTex, bytes);
                albumTex.wrapMode = TextureWrapMode.Clamp;
            }
            catch (Exception e) { Plugin.Log.LogWarning("[album] " + file + ": " + e.Message); }
            // 2026-10-14_15-04-32_crazy.png
            string name = Path.GetFileNameWithoutExtension(file), when = name, who = "";
            try
            {
                var parts = name.Split('_');
                var d = DateTime.ParseExact(parts[0] + " " + parts[1], "yyyy-MM-dd HH-mm-ss", null);
                when = d.ToString("yyyy.MM.dd  HH:mm");
                if (parts.Length > 2) who = Loc.T(Personas.Get(parts[2]).Short);
            }
            catch { }
            string caption = (who.Length > 0 ? Loc.F("{0}와 함께", who) + "  ·  " : "") + when;
            if (!string.IsNullOrEmpty(albumRemark)) caption = $"<color=#ff9fd0>{Loc.Display(ShortName)}: {Loc.Display(Loc.F(albumRemark, PlayerName()))}</color>\n" + caption;
            ui.ShowPhoto(Loc.T("사진 앨범"), albumTex, caption, $"{albumIndex + 1} / {albumFiles.Count}", Loc.T(PeacePad.InUse ? "LB RB 넘기기   ·   Ⓑ 닫기" : "← → 넘기기   ·   Esc 닫기"), albumTurn);
            albumTurn = 0;
        }

        int albumTurn;

        void TurnAlbum(int d)
        {
            albumTurn = d;
            albumIndex += d;
            albumRemark = Random.value < 0.4f ? Pick(AlbumPageRemarks()) : null;
            ShowAlbumPage();
        }

        void OpenRecords()
        {
            cardReturn = choosingFrom ?? cardReturn;
            cardKind = CardKind.Records;
            HoldControls(true);
            var rows = new List<(string, string)>();
            rows.Add((Loc.T("호감도"), TierText(S.Affection)));
            rows.Add((Loc.T("함께한 날 (게임)"), Loc.F("{0}일차", S.Day)));
            if (!string.IsNullOrEmpty(S.FirstDate))
            {
                string first = S.FirstDate.Replace('-', '.');
                try { var d0 = DateTime.ParseExact(S.FirstDate, "yyyy-MM-dd", null); first += "  " + Loc.F("({0}일째)", (int)(DateTime.Today - d0.Date).TotalDays + 1); } catch { }
                rows.Add((Loc.T("처음 만난 날"), first));
            }
            if (S.Streak > 1) rows.Add((Loc.T("연속으로 찾아온 날"), Loc.F("{0}일", S.Streak)));
            int mins = (int)(S.PlayedSec / 60f);
            rows.Add((Loc.T("같이 보낸 시간"), mins >= 60 ? Loc.F("{0}시간 {1}분", mins / 60, mins % 60) : Loc.F("{0}분", mins)));
            if (S.Sessions > 0) rows.Add((Loc.T("찾아온 횟수"), Loc.F("{0}번", S.Sessions)));
            rows.Add((Loc.T("나눈 이야기"), Loc.F("{0}번", S.Talks)));
            rows.Add((Loc.T("지금 기분"), MoodText()));
            if (S.PlantDays > 0) rows.Add((Loc.T("같이 키우는 화분"), Loc.T(PlantStageNames[PlantStage]) + (PlantWilted ? "  (" + Loc.T("시들어 가요!") + ")" : "") + "  · " + Loc.F("물 준 날 {0}일", S.PlantDays)));
            rows.Add((Loc.T("쓰다듬기 · 포옹 · 칭찬"), $"{S.Pats} · {S.Hugs} · {S.Compliments}"));
            if (S.RpsGames > 0) rows.Add((Loc.T("가위바위보"), Loc.F("{0}승 {1}판", S.RpsWins, S.RpsGames)));
            if (S.Gifts > 0) rows.Add((Loc.T("선물"), Loc.F("{0}개", S.Gifts)));
            int photos = PhotoFiles().Count(f => f.EndsWith("_" + Pn.Id + ".png"));
            if (photos > 0) rows.Add((Loc.T("같이 찍은 사진"), Loc.F("{0}장", photos)));
            if (S.AltF4Count > 0) rows.Add((Loc.T("Alt+F4로 끈 횟수"), Loc.F("{0}번", S.AltF4Count)));
            ui.ShowRows(Loc.F("{0}와 우리의 기록", ShortName), rows, Loc.T(PeacePad.InUse ? "Ⓑ 닫기" : "Esc 닫기"));
        }

        void CloseCardBack()
        {
            ui.CloseCard();
            cardKind = CardKind.None;
            if (albumTex != null) { Destroy(albumTex, 0.4f); albumTex = null; }   // (after the card has faded away)
            // back to the menu it was opened from (the stack under it is still there)
            var back = cardReturn;
            cardReturn = null;
            reopening = true;
            try { if (back != null) back.Reopen(); else OpenMemoriesMenu(); }
            finally { reopening = false; }
        }

        // keys while the album or the records are up (Update)
        void TickCard()
        {
            if (!ui.CardOpen) { if (cardKind != CardKind.None) cardKind = CardKind.None; return; }
            HoldControls(true);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(PeaceMode.InteractKey.Value)) { CloseCardBack(); return; }
            if (cardKind == CardKind.Diary)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.mouseScrollDelta.y < -0.1f || Input.GetMouseButtonDown(0)) TurnDiary(+1);
                else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.mouseScrollDelta.y > 0.1f) TurnDiary(-1);
            }
            if (cardKind == CardKind.Album)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.mouseScrollDelta.y < -0.1f || Input.GetMouseButtonDown(0)) TurnAlbum(+1);
                else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.mouseScrollDelta.y > 0.1f) TurnAlbum(-1);
            }
        }

        // ---------------------------------------------------------------- settings

        static readonly float[] TimeSpeeds = { 0.5f, 1f, 2f, 4f };
        static readonly string[] TimeSpeedNames = { "느리게", "보통", "빠르게", "아주 빠르게" };
        static readonly string[] Langs = { "auto", "ko", "en", "ja", "zh" };
        static readonly string[] LangNames = { "게임 언어", "한국어", "English", "日本語", "中文" };
        static readonly float[] Chatty = { 1.8f, 1f, 0.6f };
        static readonly string[] ChattyNames = { "가끔", "보통", "자주" };

        static readonly float[] UiScales = { 0.85f, 1f, 1.15f, 1.3f };
        static readonly string[] UiScaleNames = { "작게", "보통", "크게", "아주 크게" };
        bool keyWaiting;   // settings: the next key pressed becomes the interact key

        enum Typing { None, Name, Birthday, ApiKey }
        Typing typing;

        void OpenSettingsMenu()
        {
            int ts = Array.FindIndex(TimeSpeeds, v => Mathf.Approximately(v, PeaceMode.TimeScale.Value));
            int li = Math.Max(0, Array.IndexOf(Langs, (PeaceMode.Language.Value ?? "auto").Trim().ToLowerInvariant()));
            int ci = Array.FindIndex(Chatty, v => Mathf.Approximately(v, PeaceMode.Chattiness.Value));
            int si = Array.FindIndex(UiScales, v => Mathf.Approximately(v, PeaceMode.UiScale.Value));
            string name = PeaceMode.PlayerName.Value;
            string bday = PeaceMode.Birthday.Value;
            var items = new List<MenuItem>
            {
                new MenuItem("상태 표시") { Hint = Loc.T(PeaceMode.ShowHud.Value ? "켜짐" : "꺼짐") },
                new MenuItem("미타 위치 표시") { Hint = Loc.T(PeaceMode.MitaMarker.Value ? "켜짐" : "꺼짐") },
                new MenuItem("효과음") { Hint = Loc.T(PeaceMode.UiSounds.Value ? "켜짐" : "꺼짐") },
                new MenuItem("화면 크기") { Hint = si >= 0 ? Loc.T(UiScaleNames[si]) : $"×{PeaceMode.UiScale.Value:0.##}" },
                new MenuItem("교감 키") { Hint = keyWaiting ? Loc.T("누를 키를 눌러 주세요") : PeaceMode.InteractKey.Value.ToString(), Now = keyWaiting },
                new MenuItem("시간 흐름") { Hint = ts >= 0 ? Loc.T(TimeSpeedNames[ts]) : $"×{PeaceMode.TimeScale.Value:0.##}" },
                new MenuItem("미타가 먼저 말 걸기") { Hint = ci >= 0 ? Loc.T(ChattyNames[ci]) : $"×{PeaceMode.Chattiness.Value:0.##}" },
                new MenuItem("언어") { Hint = LangNames[li] == "게임 언어" ? Loc.T("게임 언어") : LangNames[li] },
                new MenuItem("내 이름") { Hint = string.IsNullOrWhiteSpace(name) ? PlayerName() : name.Trim() },
                new MenuItem("내 생일") { Hint = string.IsNullOrWhiteSpace(bday) ? Loc.T("없음") : bday.Trim() },
                new MenuItem("AI 대화") { Sub = true, Hint = Loc.T(PeaceMode.AiEnabled.Value ? "켜짐" : "꺼짐") },
                new MenuItem("그만두기") { Back = true },
            };
            var acts = new List<Action>
            {
                () => { PeaceMode.ShowHud.Value = !PeaceMode.ShowHud.Value; ReopenSettings(); },
                () => { PeaceMode.MitaMarker.Value = !PeaceMode.MitaMarker.Value; ReopenSettings(); },
                () => { PeaceMode.UiSounds.Value = !PeaceMode.UiSounds.Value; ReopenSettings(); },
                () => { PeaceMode.UiScale.Value = UiScales[(si + 1) % UiScales.Length]; ui.SetScale(PeaceMode.UiScale.Value); ReopenSettings(); },
                () => { keyWaiting = true; ReopenSettingsQuiet(); },
                () => { PeaceMode.TimeScale.Value = TimeSpeeds[(ts + 1) % TimeSpeeds.Length]; ReopenSettings(); },
                () => { PeaceMode.Chattiness.Value = Chatty[(ci + 1) % Chatty.Length]; ReopenSettings(); },
                () => { SwitchLanguage(Langs[(li + 1) % Langs.Length]); ReopenSettings(); },
                () => StartTyping(Typing.Name),
                () => StartTyping(Typing.Birthday),
                OpenAiMenu,
                () => { },
            };
            ShowItems("설정", items, acts, OpenSettingsMenu);
        }

        // a setting changed: the same menu again, in the same place in the stack
        void ReopenSettings()
        {
            choosingFrom = null;
            reopening = refreshing = true;
            try { OpenSettingsMenu(); } finally { reopening = refreshing = false; }
            ui.Toast(Loc.T("저장했어요"));
        }

        void SwitchLanguage(string code)
        {
            PeaceMode.Language.Value = code;
            try { Loc.UseFont(false); Loc.Init(code); Loc.UseFont(true); }
            catch (Exception e) { Plugin.Log.LogWarning("[settings] language: " + e.Message); }
            Plugin.Log.LogInfo($"[settings] language {code} -> {Loc.Lang}");
        }

        void StartTyping(Typing what)
        {
            typing = what;
            HoldControls(true);
            ui.OpenInput(what == Typing.Name ? Loc.T("미타가 부를 이름을 쓰고 Enter  ·  비워 두면 원래 이름")
                         : what == Typing.ApiKey ? Loc.T("API 키를 붙여 넣고 Enter  ·  비워 두면 없음 (이 PC의 설정 파일에만 저장)")
                         : Loc.T("생일을 월-일로 (예: 03-14) 쓰고 Enter  ·  비워 두면 없음"), what == Typing.ApiKey);
            ui.SetInputText(what == Typing.Name ? PeaceMode.PlayerName.Value : what == Typing.ApiKey ? PeaceMode.AiApiKey.Value : PeaceMode.Birthday.Value);
        }

        void TickTyping()
        {
            if (typing == Typing.None) return;
            HoldControls(true);
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelTyping(); return; }
            if (!(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) return;
            var text = (ui.InputText ?? "").Trim();
            if (typing == Typing.Name) PeaceMode.PlayerName.Value = text.Length > 24 ? text.Substring(0, 24) : text;
            else if (typing == Typing.ApiKey)
            {
                PeaceMode.AiApiKey.Value = text;
                ui.CloseInput();
                typing = Typing.None;
                RefreshAi();
                ui.Toast(Loc.T("저장했어요"));
                return;
            }
            else
            {
                if (text.Length > 0 && !DateTime.TryParseExact(text, new[] { "MM-dd", "M-d", "MM/dd", "M/d", "MMdd" }, null, System.Globalization.DateTimeStyles.None, out var d))
                {
                    ui.Toast(Loc.T("월-일로 써 주세요 (예: 03-14)"), true);
                    return;
                }
                PeaceMode.Birthday.Value = text.Length == 0 ? "" : DateTime.ParseExact(text, new[] { "MM-dd", "M-d", "MM/dd", "M/d", "MMdd" }, null, System.Globalization.DateTimeStyles.None).ToString("MM-dd");
            }
            ui.CloseInput();
            typing = Typing.None;
            ReopenSettings();
        }

        void CancelTyping()
        {
            var was = typing;
            ui.CloseInput();
            typing = Typing.None;
            if (was == Typing.ApiKey) RefreshAi(); else ReopenSettingsQuiet();
        }

        void ReopenSettingsQuiet()
        {
            choosingFrom = null;
            reopening = refreshing = true;
            try { OpenSettingsMenu(); } finally { reopening = refreshing = false; }
        }

        // ---------------------------------------------------------------- keys (from Update)

        // true while one of our menus, cards or inputs has the keys
        static KeyCode[] allKeys;

        bool TickMenus()
        {
            // a pose for the camera ends when the photo menu is left without taking the picture
            if (motion != null && motion.Hold && photoStage == 0 && !devKeepPose && !(ui.PanelOpen && shownMenu == "같이 사진 찍기"))
            {
                EndMotion();
                try { mita?.FaceEmotion("smile"); } catch { }
            }
            if (typing != Typing.None) { TickTyping(); return true; }
            if (keyWaiting) { TickKeyCapture(); return true; }
            if (TickPadMenus()) { HoldControls(true); return true; }
            if (ui.CardOpen || cardKind != CardKind.None) { TickCard(); return true; }
            if (!ui.PanelOpen) return false;
            HoldControls(true);
            for (int i = 0; i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { Choose(i); return true; }
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) { Choose(9); return true; }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) ui.PanelMove(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) ui.PanelMove(+1);
            else if (Input.mouseScrollDelta.y > 0.1f) ui.PanelMove(-1);
            else if (Input.mouseScrollDelta.y < -0.1f) ui.PanelMove(+1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) ui.PanelPickSelected();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(PeaceMode.InteractKey.Value)) ui.PanelBack();
            return true;
        }

        // the interact key: any key but the ones the menus and walking use already
        void TickKeyCapture()
        {
            HoldControls(true);
            if (!ui.PanelOpen) { keyWaiting = false; return; }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { keyWaiting = false; ReopenSettingsQuiet(); return; }
            if (!Input.anyKeyDown) return;
            allKeys ??= (KeyCode[])Enum.GetValues(typeof(KeyCode));
            foreach (var k in allKeys)
            {
                if (k == KeyCode.None || k >= KeyCode.Mouse0 || !Input.GetKeyDown(k)) continue;
                bool taken = k == KeyCode.Return || k == KeyCode.KeypadEnter || k == KeyCode.Space || k == KeyCode.Escape
                             || k == KeyCode.W || k == KeyCode.A || k == KeyCode.S || k == KeyCode.D
                             || k == KeyCode.UpArrow || k == KeyCode.DownArrow || k == KeyCode.LeftArrow || k == KeyCode.RightArrow
                             || (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) || (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9)
                             || k == KeyCode.LeftShift || k == KeyCode.LeftControl || k == KeyCode.Tab;
                if (taken) { ui.Toast(Loc.F("{0} 키는 이미 쓰이고 있어요", k), true); return; }
                SetInteractKey(k);
                return;
            }
        }

        void SetInteractKey(KeyCode k)
        {
            PeaceMode.InteractKey.Value = k;
            keyWaiting = false;
            ReopenSettings();
        }

        // ---------------------------------------------------------------- where she is (the marker)

        float markerAt, unseenSince = -1f;
        Camera markerCam;

        void TickMarker()
        {
            if (Time.time < markerAt) return;
            markerAt = Time.time + 0.15f;
            bool want = PeaceMode.MitaMarker.Value && freeMode && !MenuOpen && fadeTarget <= 0f && fadeVal <= 0.001f && !cardGame && !GameRunning
                        && hideStage == 0 && photoStage == 0 && !following && pm != null && pm.mainCamera != null && mitaBody != null && !pm.animationRun;
            if (!want) { unseenSince = -1f; ui.Marker.Hide(); return; }
            if (markerCam == null || !markerCam.isActiveAndEnabled) markerCam = pm.mainCamera.GetComponent<Camera>() ?? Camera.main;
            if (markerCam == null) return;
            var head = HeadPoint;
            var eye = pm.mainCamera.position;
            var flat = head - eye; flat.y = 0f;
            float dist = flat.magnitude;
            var vp = markerCam.WorldToViewportPoint(head + Vector3.up * 0.25f);
            bool onScreen = vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
            bool seen = onScreen && CanSee(head, eye);
            // (in sight, close by, or only just gone: nothing)
            if (seen || dist < 3f) { unseenSince = -1f; ui.Marker.Hide(); return; }
            if (unseenSince < 0f) unseenSince = Time.time;
            if (Time.time - unseenSince < 4f) { ui.Marker.Hide(); return; }

            var size = ui.CanvasSize;
            string label = $"{ShortName}  {dist:0}m";
            if (onScreen)
            {
                // behind a wall but in front of you: over her head
                ui.Marker.Set(new Vector2(vp.x * size.x, vp.y * size.y + 24f), Vector2.zero, label, ui.Font);
                return;
            }
            var d = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
            if (vp.z < 0f) d = -d;   // behind you: the projection is mirrored
            if (d.sqrMagnitude < 1f) d = Vector2.down;
            float mx = size.x * 0.5f - 120f, my = size.y * 0.5f - 70f;
            float k = Mathf.Min(mx / Mathf.Max(0.001f, Mathf.Abs(d.x)), my / Mathf.Max(0.001f, Mathf.Abs(d.y)));
            var pos = size * 0.5f + d * k;
            // (not under the status card at the top right)
            if (pos.x > size.x - 480f && pos.y > size.y - 200f) pos.y = size.y - 200f;
            ui.Marker.Set(pos, d, label, ui.Font);
        }

        // ---------------------------------------------------------------- dev

        public bool DevMenuHas(string what)
        {
            if ((!pendingMenu && !MenuOpen) || lastMenuOpts == null) return false;
            if (what.StartsWith("#")) return int.TryParse(what.Substring(1), out var n) && lastMenuOpts.Count > n;
            return lastMenuOpts.Exists(o => o == what || o.StartsWith(what));
        }

        public void DevPick(string text)
        {
            int i = lastMenuOpts != null ? lastMenuOpts.FindIndex(o => o == text || o.StartsWith(text)) : -1;
            if (i < 0) { Plugin.Log.LogWarning($"[dev] pick: no '{text}' in [{(lastMenuOpts != null ? string.Join(", ", lastMenuOpts) : "-")}]"); return; }
            ui.PickRow(i);
        }

        public void DevPickAt(int i) { if (lastMenuOpts != null && i >= 0 && i < lastMenuOpts.Count) ui.PickRow(i); else Plugin.Log.LogWarning($"[dev] pickat {i}: no such row"); }
        public void DevAlbum(int turn) { if (cardKind == CardKind.Album) TurnAlbum(turn); else OpenAlbum(); }
        public void DevRecords() => OpenRecords();
        public void DevCloseCard() => CloseCardBack();
        public void DevMenuBack() => ui.PanelBack();
        public void DevKey(string k) { if (Enum.TryParse<KeyCode>(k, true, out var kc)) SetInteractKey(kc); }
        public void DevToast(string t) => ui.Toast(t, t.StartsWith("♥"));
    }
}
