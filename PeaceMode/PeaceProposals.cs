using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // She asks first. Now and then, instead of a line to herself, she suggests something to do together - what fits
    // the hour: a game, a photo, the window, a walk round the house, cooking at meal time, bed at night, her own
    // special, TV / a book / a rest on the sofa (PeaceLounge) - and you say yes or not now (each Mita in her own words,
    // Personas.Proposals). When she comes over to you after you've been elsewhere, she often has one ready. Cold she
    // doesn't; warm she asks more often.
    public partial class PeaceDirector
    {
        float nextProposal = -1f;
        readonly Queue<string> recentProposals = new Queue<string>();
        string devProposal;

        // what she could suggest right now: id -> what saying yes starts
        List<(string id, Action go)> OpenProposals()
        {
            var list = new List<(string, Action)>
            {
                ("talk", DoTalk),
                ("rps", () => Play(L.RpsStart, () => ShowMenu("가위바위보", new List<string> { "가위", "바위", "보" },
                    new List<Action> { () => Rps(0), () => Rps(1), () => Rps(2) }))),
                ("cham", StartCham),
                ("photo", DoPhoto),
            };
            bool night = IsNight();
            if (!night) list.Add(("hide", StartHide));
            // (Korean word play: in other languages these two stay Korean)
            if (Loc.Korean) { list.Add(("words", StartWordChain)); list.Add(("riddle", StartRiddles)); }
            if (S.Affection >= 15) list.Add(("window", DoWindow));
            if (!following) list.Add(("walk", StartFollow));
            if (MealTime() && S.LastMeal != MealKey() && !cooking && activity == MitaActivity.Free) list.Add(("cook", DoCook));
            if (night) list.Add(("sleep", DoSleep));
            if (!string.IsNullOrEmpty(Pn.SpecialName)) list.Add(("special", DoSpecial));
            if (PresentOpen) list.Add(("present", GiveBack));   // (her own present: PeaceMore)
            if (DanceReady) list.Add(("dance", StartDance));
            if (HammerReady) list.Add(("hammer", StartHammer));
            if (LoungeReady)
            {
                list.Add(("tv", () => StartLounge(LoungeKind.Tv)));
                list.Add(("read", () => StartLounge(LoungeKind.Read)));
                list.Add(("rest", () => StartLounge(LoungeKind.Rest)));
            }
            return list;
        }

        // in her idle slot (see Tick); true = she suggested something
        bool TryPropose(bool now = false)
        {
            if (Cold || request != null || activity != MitaActivity.Free || following || cooking || GameRunning) return false;
            if (nextProposal < 0f) nextProposal = Time.time + Random.Range(150f, 300f);
            if (!now && Time.time < nextProposal) return false;
            nextProposal = Time.time + (Warm ? Random.Range(240f, 420f) : Random.Range(360f, 600f));
            var lines = Personas.Proposals(Pn.Id);
            // (her present is asked in the gift lines' words)
            var backAsk = Personas.Gifts(Pn.Id).BackAsk;
            if (backAsk != null && !lines.Ask.ContainsKey("present")) lines.Ask["present"] = backAsk;
            var open = OpenProposals().Where(p => lines.Ask.ContainsKey(p.id) && (devProposal == null || p.id == devProposal)).ToList();
            // (not the same few again and again; meal time and bedtime come first)
            var pick = open.FirstOrDefault(p => p.id == "cook" || p.id == "sleep");
            if (pick.id == null)
            {
                var fresh = open.Where(p => !recentProposals.Contains(p.id)).ToList();
                if (fresh.Count == 0) fresh = open;
                if (fresh.Count == 0) return false;
                pick = fresh[Random.Range(0, fresh.Count)];
            }
            recentProposals.Enqueue(pick.id);
            while (recentProposals.Count > 5) recentProposals.Dequeue();
            var go = pick.go;
            Plugin.Log.LogInfo("[propose] " + pick.id);
            Play(lines.Ask[pick.id], () => ShowMenu(Pn.Short, new List<string> { "좋아", "지금은 좀…" }, new List<Action>
            {
                () => { Did("propose"); go(); },
                () => Play(Pick(lines.No)),
            }));
            return true;
        }

        public void DevPropose(string id) { devProposal = string.IsNullOrEmpty(id) ? null : id; TryPropose(true); devProposal = null; }
    }
}
