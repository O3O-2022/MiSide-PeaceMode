using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Listening to the cassette player together (특별한 시간), anniversaries of the first visit, the morning after a
    // gift and a present of her own (PeaceGifts). Lines: PersonasMore.cs.
    public partial class PeaceDirector
    {
        // ---- the cassette player: the house music comes up while the two of you stand at the bedroom's recorder ----

        bool musicBoost;

        void DoMusic()
        {
            EndRoutine();
            Did("music");
            var m = Personas.Moments(Pn.Id);
            var rec = spots.FirstOrDefault(s => s.id == "recorder")?.go ?? Util.FindPath("Bedroom>Recorder")?.gameObject;
            if (rec == null)
            {
                // no recorder in this house: the music comes up where you are
                musicBoost = true;
                Play(m.Music ?? new Line[0], () => { musicBoost = false; AddAff(2); CheckMilestones(); });
                return;
            }
            fadeTarget = 1f;
            afterFade = () =>
            {
                ClearLeftoverText();
                ShowRoom("Bedroom");
                // you a step back from the recorder, facing it, she beside you on the right looking at it too
                var at = rec.transform.position;
                var back = BedCenter - at; back.y = 0f;
                if (back.sqrMagnitude < 0.01f) back = Vector3.back;
                back.Normalize();
                // (the recorder sits on a shelf at about 2 m: you two steps back, she a step ahead on your right so both
                // she and the shelf are in view, the view aimed between them)
                var pp = OnFloor(new Vector3(at.x, 0f, at.z) + back * 1.9f);
                var look = new Vector3(at.x, 0f, at.z) - pp; look.y = 0; look.Normalize();
                var right = Vector3.Cross(Vector3.up, look);
                var lookAt = new Vector3(at.x, 1.45f, at.z) + right * 0.3f;
                MovePlayer(pp, lookAt);
                var mp = OnFloor(pp + right * 0.7f + look * 1.0f);
                var toRec = new Vector3(at.x, 0f, at.z) - mp; toRec.y = 0; toRec.Normalize();
                PoseMitaUpright(mp, toRec);
                uprightFront = Vector3.zero;
                poseRot = Quaternion.LookRotation(toRec);
                mitaBody.rotation = poseRot;
                try { mitaLook?.Activation(false); } catch { }
                musicBoost = true;
                Plugin.Log.LogInfo($"[moment] music: you at {pp}, she at {mp}, recorder {at}");
                fadeTarget = 0f;
                afterFade = () => Play(m.Music ?? new Line[0], () =>
                {
                    musicBoost = false;
                    AddAff(2);
                    S.Minute = Mathf.Min(S.Minute + 15f, 1439f);
                    if (posed && mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
                    try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
                    CheckMilestones();
                });
            };
        }

        // ---- anniversaries: real days since the first visit, in the session greeting ----

        static readonly int[] AnniversaryMarks = { 7, 30, 100, 365, 730 };
        int anniversaryDays;   // {days}

        IEnumerable<Line> AnniversaryLines(WallLines w)
        {
            var today = Now.Date;
            if (string.IsNullOrEmpty(S.FirstDate)) { S.FirstDate = today.ToString("yyyy-MM-dd"); return Array.Empty<Line>(); }
            if (!DateTime.TryParseExact(S.FirstDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first)) return Array.Empty<Line>();
            int days = (int)(today - first.Date).TotalDays + 1;   // (the first day counts as day 1)
            int mark = 0;
            foreach (var m in AnniversaryMarks) if (days >= m) mark = m;
            if (mark == 0 || S.Seen.Contains("#anniv" + mark) || w.Anniversary == null) return Array.Empty<Line>();
            // (a mark passed while away is told on the next visit, the latest one only)
            foreach (var m in AnniversaryMarks) if (m <= mark) S.Seen.Add("#anniv" + m);
            anniversaryDays = days;
            AddAff(3);
            Plugin.Log.LogInfo($"[peace] anniversary: day {days} (mark {mark})");
            return w.Anniversary;
        }

        // ---- gifts: the morning after, and a present of her own ----

        // in her morning words: yesterday's gift (once)
        IEnumerable<Line> GiftRecall()
        {
            if (S.LastGiftDay <= 0 || S.LastGiftDay >= S.Day || string.IsNullOrEmpty(S.LastGift)) return Array.Empty<Line>();
            var g = Personas.Gifts(Pn.Id);
            var kind = Array.Find(GiftKinds, k => k.id == S.LastGift);
            bool loved = S.LastGiftLoved && g.RecallLoved != null;
            S.LastGiftDay = 0;
            if (kind.id == null || (!loved && g.Recall == null)) return Array.Empty<Line>();
            giftText = Loc.T(kind.name);
            return loved ? g.RecallLoved : g.Recall;
        }

        // her present (a suggestion of hers, from affection 50, once a game day)
        bool PresentOpen => S.Affection >= 50 && S.PresentDay != S.Day && Personas.Gifts(Pn.Id).Back != null;

        void GiveBack()
        {
            var g = Personas.Gifts(Pn.Id);
            S.PresentDay = S.Day;
            Did("present");
            AddAff(2);
            Plugin.Log.LogInfo("[gift] her present");
            Play(Pick(g.Back), CheckMilestones);
        }
    }
}
