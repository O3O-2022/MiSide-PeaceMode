using System;
using System.Collections.Generic;
using System.Text;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // The story's own choice list (DialogueChanger, as in Scene 3's talks with Mita and the official peace mode's
    // interactions): borrowed from Scene 3 with the rest (see ImportCooking) and filled with our options.
    public partial class PeaceDirector
    {
        GameObject choiceTemplate;

        void BorrowChoices(Scene s, Scene here)
        {
            DialogueChanger dc = null;
            foreach (var r in s.GetRootGameObjects())
            {
                // (the scene never started: the one with its button template under the canvas)
                foreach (var d in r.GetComponentsInChildren<DialogueChanger>(true))
                    if (d != null && Util.FindDeep(d.transform, "Case") != null) { dc = d; break; }
                if (dc != null) break;
            }
            if (dc == null) { Plugin.Log.LogWarning("[choice] no DialogueChanger in Scene 3"); return; }
            if (DevRunner.Active) Plugin.Log.LogInfo("[choice] " + Describe(dc));
            SceneImport.StripOutside(dc.gameObject, s);
            dc.gameObject.SetActive(false);
            var t = dc.transform;
            SceneImport.Take(t, here);
            t.SetParent(templates.transform, true);
            // (lists are made from a copy, as the cooking corner: see ImportCooking)
            dc.gameObject.name = "PeaceChoice Original";
            choiceTemplate = Instantiate(dc.gameObject, templates.transform);
            choiceTemplate.name = "PeaceChoice Template";
            Plugin.Log.LogInfo("[choice] choice list borrowed");
        }

        static string Describe(DialogueChanger dc)
        {
            var sb = new StringBuilder();
            sb.Append($"{Util.PathOf(dc.transform)} file='{dc.fileQuest}' style={dc.style} pos={Util.PathOf(dc.positionDialogue)} example={Util.PathOf(dc.exampleButton != null ? dc.exampleButton.transform : null)} " +
                      $"frame={Util.PathOf(dc.frameDialogue)} canvas={(dc.canvas != null ? dc.canvas.name : "-")} timeClose={dc.timeClose} interactive={Util.PathOf(dc.objectInteractive != null ? dc.objectInteractive.transform : null)}");
            var cv = dc.canvas != null ? dc.canvas.GetComponent<Canvas>() : null;
            if (cv != null) sb.Append($" mode={cv.renderMode} scale={dc.canvas.transform.lossyScale}");
            if (dc.buttons != null)
                for (int i = 0; i < dc.buttons.Length; i++)
                {
                    var b = dc.buttons[i];
                    if (b == null) continue;
                    string text0 = "?", text1 = "?";
                    try { text0 = GlobalLanguage.GetString(dc.fileQuest, b.stringFile); } catch { }
                    try { text1 = GlobalLanguage.GetString(dc.fileQuest, b.stringFile - 1); } catch { }
                    sb.Append($"\n   case {i}: string {b.stringFile} ('{text0}' / -1 '{text1}') icon={(b.iconButton != null ? b.iconButton.name : "-")} close={b.closeClick} exit={b.exitButton} oneTime={b.oneTime} oneTimeUse={b.oneTimeUse} isClose={b.isClose} button={(b.button != null ? b.button.name : "-")} click={SceneDumper.EventString(b.eventClick)}");
                }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- the list

        GameObject choiceGo;
        DialogueChanger choice;
        Action<int> choiceCb;
        int choiceCount;
        bool choiceOpen;
        int menuFrame = -1;   // a list was opened or answered in this frame: its key press is used up

        bool MenuOpen => ui.MenuOpen || choiceOpen || chatOpen;

        static bool IsExitOption(string o) => o == "닫기" || o == "그만두기" || o == "그냥 두기" || o == "그만할래" || o == "조금 더 있을래";

        // a fresh copy of the story's list next to Mita's head; false = not borrowed (yet): the caller uses our own menu
        [HideFromIl2Cpp] bool OpenChoices(IList<string> opts, Action<int> choose)
        {
            if (choiceTemplate == null || opts.Count == 0) return false;
            CloseChoices(false);
            var tdc = choiceTemplate.GetComponent<DialogueChanger>();
            Sprite exitIcon = null;
            if (tdc.buttons != null) foreach (var b in tdc.buttons) if (b != null && b.exitButton && b.iconButton != null) exitIcon = b.iconButton;
            choiceGo = Instantiate(choiceTemplate);
            choiceGo.name = "PeaceChoice";
            SceneManager.MoveGameObjectToScene(choiceGo, SceneManager.GetActiveScene());
            choice = choiceGo.GetComponent<DialogueChanger>();
            var cases = new Il2CppReferenceArray<DialogueChangerCase>(opts.Count);
            for (int i = 0; i < opts.Count; i++)
            {
                int idx = i;
                var c = new DialogueChangerCase();
                c.stringFile = TextHook.Register(Loc.T(opts[i]));
                c.eventClick = new UnityEvent();
                c.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => ChoiceClicked(idx))));
                c.closeClick = true;
                // the story's "leave it" case: its mark, and the one the list's own Esc handling picks
                if (IsExitOption(opts[i])) { c.iconButton = exitIcon; c.exitButton = true; }
                cases[i] = c;
            }
            choice.buttons = cases;
            var head = SpeakerBody != null ? Util.FindDeep(SpeakerBody, "Head") : null;
            choice.positionDialogue = head ?? SpeakerBody ?? mitaBody;
            choice.objectInteractive = null;
            choiceCb = choose;
            choiceCount = opts.Count;
            choiceOpen = true;
            menuFrame = Time.frameCount;
            choiceGo.SetActive(true);
            try { choice.Play(); }
            catch (Exception e) { Plugin.Log.LogWarning("[choice] play: " + e.Message); CloseChoices(false); return false; }
            return true;
        }

        // a case was clicked (by its key, the mouse or us): the list closes by itself
        void ChoiceClicked(int i)
        {
            if (DevRunner.Active) Plugin.Log.LogInfo($"[choice] picked {i}");
            if (!choiceOpen) return;
            var cb = choiceCb;
            menuFrame = Time.frameCount;
            CloseChoices(false);
            cb?.Invoke(i);
        }

        void ChooseChoice(int i)
        {
            if (!choiceOpen || i < 0 || i >= choiceCount) return;
            var list = choiceGo;
            try { choice.ClickButton(i); } catch (Exception e) { Plugin.Log.LogWarning("[choice] click: " + e.Message); }
            // in case the list didn't pass the click on (the choice may already have opened the next list)
            if (choiceOpen && choiceGo == list) ChoiceClicked(i);
        }

        void CloseChoices(bool animate)
        {
            choiceOpen = false;
            choiceCb = null;
            if (choiceGo == null) return;
            var go = choiceGo;
            try { if (animate) choice.Close(); } catch { }
            choiceGo = null; choice = null;
            Destroy(go, animate ? 0.6f : 0.4f);
        }

        // the list closed itself without an answer (its own Esc handling): let go of the controls
        float choiceGoneSince = -1f;
        void TickChoices()
        {
            if (!choiceOpen || choice == null || choice.play) { choiceGoneSince = -1f; return; }
            if (choiceGoneSince < 0f) { choiceGoneSince = Time.time; return; }
            if (Time.time - choiceGoneSince < 1f) return;   // (a click's own event may still be on its way)
            Plugin.Log.LogInfo("[choice] the list closed by itself");
            choiceGoneSince = -1f;
            CloseChoices(false);
        }

        // (the list reads its number keys itself too: one press must not also answer the list it opens)
        void Choose(int i)
        {
            if (Time.frameCount == menuFrame) return;
            if (choiceOpen) ChooseChoice(i); else ui.Choose(i);
        }
        void CloseMenus() { if (choiceOpen) CloseChoices(true); ui.CloseMenu(); }

        // pick <text>: the option of the open menu with that text (scripts don't break when options come and go)
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
            Choose(i);
        }

        public void DevChoiceEscape() { try { choice?.EscapeClick(); } catch (Exception e) { Plugin.Log.LogWarning("[choice] escape: " + e.Message); } }
        public string DevChoiceState()
        {
            if (choice == null) return $"no list (open {choiceOpen})";
            return $"open {choiceOpen} play {choice.play} canvas {(choice.canvas != null && choice.canvas.activeInHierarchy)} canInteractive {choice.canInteractive} buttons {choice.indexButtons?.Count}";
        }

        // ---------------------------------------------------------------- dev: a trial list
        GameObject devChoice;
        public void DevChoices(string texts)
        {
            if (choiceTemplate == null) { Plugin.Log.LogWarning("[choice] no template"); return; }
            if (devChoice != null) Destroy(devChoice);
            devChoice = Instantiate(choiceTemplate);
            devChoice.name = "PeaceChoice";
            SceneManager.MoveGameObjectToScene(devChoice, SceneManager.GetActiveScene());
            var dc = devChoice.GetComponent<DialogueChanger>();
            var template = choiceTemplate.GetComponent<DialogueChanger>();
            var opts = texts.Split('|');
            var cases = new Il2CppReferenceArray<DialogueChangerCase>(opts.Length);
            for (int i = 0; i < opts.Length; i++)
            {
                int idx = i;
                var c = new DialogueChangerCase();
                c.stringFile = TextHook.Register(Loc.T(opts[i]));
                c.eventClick = new UnityEvent();
                c.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => Plugin.Log.LogInfo($"[choice] clicked {idx}: {opts[idx]}"))));
                var src = template.buttons != null && template.buttons.Length > 0 ? template.buttons[Math.Min(i, template.buttons.Length - 1)] : null;
                c.iconButton = src?.iconButton;
                c.closeClick = true;
                cases[i] = c;
            }
            dc.buttons = cases;
            if (mitaBody != null) dc.positionDialogue = Util.FindDeep(mitaBody, "Head") ?? mitaBody;
            dc.objectInteractive = null;
            devChoice.SetActive(true);
            try { dc.Play(); } catch (Exception e) { Plugin.Log.LogWarning("[choice] play: " + e); }
            Plugin.Log.LogInfo("[choice] shown: " + Describe(dc));
        }
        public void DevChoiceClick(int i)
        {
            var dc = devChoice != null ? devChoice.GetComponent<DialogueChanger>() : null;
            try { dc?.ClickButton(i); } catch (Exception e) { Plugin.Log.LogWarning("[choice] click: " + e.Message); }
        }
    }
}
