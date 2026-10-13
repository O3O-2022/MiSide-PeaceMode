using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // Main menu: always show the "평화 모드" entry. It opens the game's own (normally "coming soon")
    // peace-mode page, which we fill with one button per Mita, styled like every other menu button.
    [HarmonyPatch]
    public static class MenuPatch
    {
        public static Menu CurrentMenu;
        public static readonly List<GameObject> PersonaButtons = new List<GameObject>();
        static Text description;
        static GameObject page;
        static MenuNextLocation peaceNav;
        static float openCheckAt = -1f;

        const string DefaultDescription = "미타마다 날짜와 호감도가 따로 저장돼요.";

        [HarmonyPatch(typeof(Menu), nameof(Menu.Start)), HarmonyPostfix]
        static void MenuStart(Menu __instance)
        {
            PeaceMode.Active = false;
            Loc.UseFont(false);   // (the menu is the game's again)
            CurrentMenu = __instance;
            Loc.Init(PeaceMode.Language.Value);   // (the game's language may have just been changed in its options)
            try { Setup(__instance); }
            catch (Exception e) { Plugin.Log.LogError("[peace] menu setup failed: " + e); }
        }

        // If the button's own navigation didn't open the page, open it ourselves.
        [HarmonyPatch(typeof(Menu), nameof(Menu.Update)), HarmonyPostfix]
        static void MenuUpdate()
        {
            try { OverlayGuard.Tick(false); } catch (Exception e) { Plugin.Log.LogError("[peace] overlay guard: " + e); }
            if (openCheckAt < 0 || Time.unscaledTime < openCheckAt) return;
            openCheckAt = -1f;
            if (page != null && !page.activeInHierarchy && CurrentMenu != null)
            {
                Plugin.Log.LogInfo("[peace] peace page did not open by itself, navigating");
                try { CurrentMenu.OpenNextLocation(page); } catch (Exception e) { Plugin.Log.LogWarning("[peace] navigate failed: " + e.Message); }
            }
        }

        static void Setup(Menu menu)
        {
            PersonaButtons.Clear();
            description = null;
            page = null;
            peaceNav = null;

            GameObject button = null;
            var cases = menu.peaceModeCases;
            if (cases != null)
                foreach (var go in cases)
                    if (go != null && go.name.StartsWith("Button") && go.name.Contains("PeaceMode")) { button = go; break; }
            if (button == null) button = Util.FindDeepAll("Button PeaceMode")?.gameObject;
            if (button == null) { Plugin.Log.LogWarning("[peace] no peace mode button in the menu"); return; }
            button.SetActive(true);

            var menuRef = menu;
            var click = button.GetComponent<ButtonMouseClick>();
            peaceNav = button.GetComponent<MenuNextLocation>();
            var loc = Util.FindDeepAll("Location PeaceMode");
            if (click != null)
            {
                Plugin.Log.LogInfo($"[peace] peace button calls: {SceneDumper.EventString(click.eventClick)} nav={(peaceNav != null && peaceNav.nextLocation != null ? peaceNav.nextLocation.name : "-")}");
                click.eventClick.m_PersistentCalls?.Clear();
                click.eventClick.DirtyPersistentCalls();
            }

            if (loc == null || !BuildPage(menu, loc.gameObject))
            {
                // fallback: no page to fill, start with the original Mita right away
                if (peaceNav != null) peaceNav.nextLocation = null;
                click?.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => PeaceMode.Launch(menuRef, Personas.Default))));
                return;
            }
            page = loc.gameObject;
            if (peaceNav != null) peaceNav.nextLocation = page;
            click?.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => openCheckAt = Time.unscaledTime + 0.6f)));
        }

        static void NoLocalization(GameObject go)
        {
            foreach (var l in go.GetComponentsInChildren<Localization_UIText>(true)) UnityEngine.Object.DestroyImmediate(l);
        }

        static bool BuildPage(Menu menu, GameObject loc)
        {
            var exit = loc.transform.Find("Button Exit");
            var t1 = loc.transform.Find("Text 1");
            var t2 = loc.transform.Find("Text 2");
            if (exit == null) { Plugin.Log.LogWarning("[peace] peace page has no Button Exit"); return false; }
            var exitRt = exit.GetComponent<RectTransform>();
            // (the game words its own button in the game's language; ours may be another one)
            if (Loc.Lang != "ko")
            {
                NoLocalization(exit.gameObject);
                var et = exit.GetComponentInChildren<Text>(true);
                if (et != null) et.text = Loc.T("돌아가기");
            }
            foreach (var t in new[] { t1, t2, exit })
                if (t != null)
                {
                    var r = t.GetComponent<RectTransform>();
                    Plugin.Log.LogInfo($"[peace] page item {t.name}: pos={r.anchoredPosition} size={r.sizeDelta} anchor={r.anchorMin}-{r.anchorMax} pivot={r.pivot}");
                }

            if (t1 != null)
            {
                NoLocalization(t1.gameObject);
                var tx = t1.GetComponent<Text>();
                if (tx != null) tx.text = Loc.T("누구와 함께 지낼까요?");
            }
            if (t2 != null)
            {
                NoLocalization(t2.gameObject);
                description = t2.GetComponent<Text>();
                if (description != null) description.text = Loc.T(DefaultDescription);
            }
            // the description sits between the title and the Mita buttons (it used to cover the upper buttons)
            float stepY = Mathf.Max(exitRt.sizeDelta.y, 40f) + 6f;
            float firstButtonTop = exitRt.anchoredPosition.y + Personas.All.Count * stepY;
            if (t1 != null && t2 != null)
            {
                var r1 = t1.GetComponent<RectTransform>();
                var r2 = t2.GetComponent<RectTransform>();
                float titleTop = r1.anchoredPosition.y;
                r1.sizeDelta = new Vector2(r1.sizeDelta.x, 70f);
                float descTop = titleTop - 80f;
                r2.anchoredPosition = new Vector2(r2.anchoredPosition.x, descTop);
                r2.sizeDelta = new Vector2(r2.sizeDelta.x, Mathf.Max(60f, descTop - firstButtonTop - 20f));
                if (description != null)
                {
                    description.verticalOverflow = VerticalWrapMode.Truncate;
                    description.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                Plugin.Log.LogInfo($"[peace] page layout: title {titleTop}, description {descTop}..{descTop - r2.sizeDelta.y}, buttons from {firstButtonTop}");
            }

            var menuRef = menu;
            var locComp = loc.GetComponent<MenuLocation>();
            float step = Mathf.Max(exitRt.sizeDelta.y, 40f) + 6f;
            var all = Personas.All;
            ButtonMouseClick prev = null;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                var go = UnityEngine.Object.Instantiate(exit.gameObject, exit.parent, false);
                go.name = "Button Peace " + p.Id;
                foreach (var n in go.GetComponentsInChildren<MenuNextLocation>(true)) UnityEngine.Object.DestroyImmediate(n);
                NoLocalization(go);
                var txt = go.GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = Loc.T(p.Name);
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = exitRt.anchoredPosition + new Vector2(0, (all.Count - i) * step);

                var b = go.GetComponent<ButtonMouseClick>();
                if (b != null)
                {
                    b.eventClick = new UnityEvent();
                    b.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => PeaceMode.Launch(menuRef, p))));
                    b.eventEnter = new UnityEvent();
                    b.eventEnter.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => Describe(p))));
                    b.eventExit = new UnityEvent();
                    b.eventExit.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => { if (description != null) description.text = Loc.T(DefaultDescription); })));
                    if (prev != null) { prev.changeDown = b; b.changeUp = prev; }
                    prev = b;
                }
                if (locComp != null && locComp.objects != null)
                {
                    int at = locComp.objects.IndexOf(exitRt);
                    if (at < 0) locComp.objects.Add(rt); else locComp.objects.Insert(at, rt);
                }
                PersonaButtons.Add(go);
            }
            var exitClick = exit.GetComponent<ButtonMouseClick>();
            if (prev != null && exitClick != null) { prev.changeDown = exitClick; exitClick.changeUp = prev; }
            Plugin.Log.LogInfo($"[peace] peace page filled with {PersonaButtons.Count} Mitas");
            return true;
        }

        public static void Describe(Persona p)
        {
            if (description == null) return;
            var save = PeaceSave.Load(p.Id);
            var progress = save.Day > 1 || save.Affection != 0 ? "  " + Loc.F("({0}일차 · 호감도 {1})", save.Day, PeaceDirector.TierText(save.Affection)) : "  " + Loc.T("(처음 만나요)");
            description.text = Loc.T(p.Blurb) + progress;
        }
    }
}
