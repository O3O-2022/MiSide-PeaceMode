using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // Ten steps of how she feels about you, from the worst to "fate". Affection now also goes down: quitting with
    // Alt+F4 or the window's X, days without coming, letting the food she made go cold, forgetting what she asked
    // for, waking her. The step shows in the HUD (hearts and its name); at the bottom three she is cold (her own
    // mumbling, greetings, refused touches, no coming over, no stopping for you, annoyed instead of shy), at the top
    // three she comes over more and gets shy sooner; and she remarks on each step up or down (Personas.Aff).
    public partial class PeaceDirector
    {
        // lowest affection of each step (1 = worst ... 10)
        static readonly int[] TierMin = { int.MinValue, -39, -19, -5, 5, 15, 30, 50, 80, 120 };
        static readonly string[] TierNames = { "최악", "싫음", "냉랭", "낯섦", "친근", "친함", "호감", "좋아함", "사랑", "운명" };
        const int AffFloor = -60;

        public static int TierOf(int aff)
        {
            int t = 1;
            for (int i = 1; i < TierMin.Length; i++) if (aff >= TierMin[i]) t = i + 1;
            return t;
        }

        public static string TierName(int aff) => Loc.T(TierNames[TierOf(aff) - 1]);

        // "♥♥♥♥♡♡♡♡♡♡ 낯섦"
        public static string TierText(int aff)
        {
            int t = TierOf(aff);
            return new string('♥', t) + new string('♡', 10 - t) + " " + TierName(aff);
        }

        int Tier => TierOf(S.Affection);
        bool Cold => Tier <= 3;
        bool Warm => Tier >= 8;
        AffLines A => Personas.Aff(Pn.Id);

        // something that hurt: straight off, not scaled like her warming up
        void LoseAff(int n, string why)
        {
            if (n <= 0) return;
            S.Affection = Mathf.Max(AffFloor, S.Affection - n);
            Plugin.Log.LogInfo($"[aff] -{n} ({why}) -> {S.Affection}, step {Tier}");
        }

        // the step changed since she last remarked on it: she says so when the moment is quiet
        void CheckTierChange()
        {
            int t = Tier;
            if (S.LastTier == 0) { S.LastTier = t; return; }
            if (t == S.LastTier) return;
            bool up = t > S.LastTier;
            S.LastTier = t;
            var a = A;
            Line[] lines = t == 10 && up ? a.Top : t == 1 && !up ? a.Bottom : Pick(up ? a.Up : a.Down);
            Plugin.Log.LogInfo($"[aff] step {(up ? "up" : "down")} to {t} ({TierNames[t - 1]})");
            Play(lines, null, true);
        }

        public void DevContempt(bool on) => Contempt(on);
        public void DevSetAff(int v) { S.Affection = v; Plugin.Log.LogInfo($"[dev] affection {v}, step {Tier}"); }

        // her morning words, by how she feels
        Line[] MorningLines() => Cold && A.ColdGreeting != null ? Pick(A.ColdGreeting) : Pick(L.MorningGreetings).Concat(WeatherMorning()).Concat(GiftRecall()).ToArray();
    }
}
