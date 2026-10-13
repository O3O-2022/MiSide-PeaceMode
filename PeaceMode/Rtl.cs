using System.Collections.Generic;
using System.Text;

namespace MiSidePeaceMode
{
    // Arabic and Persian for the game's text: Unity's text lays letters out left to right one by one, so the game's own
    // Arabic files hold every letter already in its joined form (presentation forms) and the line reversed. Our text
    // is written normally; this does the same to it right before it is shown (after names are filled in): each
    // letter's joined form, lam-alef ligatures, then the line in visual order - right-to-left runs reversed, runs of
    // Latin letters and digits (a name, a number) kept readable, brackets mirrored.
    static class Rtl
    {
        // isolated, final, initial, medial (0 = the letter doesn't join on that side)
        static readonly Dictionary<char, (char iso, char fin, char ini, char med)> Forms = new Dictionary<char, (char, char, char, char)>
        {
            ['ء'] = ('ﺀ', '\0', '\0', '\0'),
            ['آ'] = ('ﺁ', 'ﺂ', '\0', '\0'), ['أ'] = ('ﺃ', 'ﺄ', '\0', '\0'),
            ['ؤ'] = ('ﺅ', 'ﺆ', '\0', '\0'), ['إ'] = ('ﺇ', 'ﺈ', '\0', '\0'),
            ['ئ'] = ('ﺉ', 'ﺊ', 'ﺋ', 'ﺌ'), ['ا'] = ('ﺍ', 'ﺎ', '\0', '\0'),
            ['ب'] = ('ﺏ', 'ﺐ', 'ﺑ', 'ﺒ'), ['ة'] = ('ﺓ', 'ﺔ', '\0', '\0'),
            ['ت'] = ('ﺕ', 'ﺖ', 'ﺗ', 'ﺘ'), ['ث'] = ('ﺙ', 'ﺚ', 'ﺛ', 'ﺜ'),
            ['ج'] = ('ﺝ', 'ﺞ', 'ﺟ', 'ﺠ'), ['ح'] = ('ﺡ', 'ﺢ', 'ﺣ', 'ﺤ'),
            ['خ'] = ('ﺥ', 'ﺦ', 'ﺧ', 'ﺨ'), ['د'] = ('ﺩ', 'ﺪ', '\0', '\0'),
            ['ذ'] = ('ﺫ', 'ﺬ', '\0', '\0'), ['ر'] = ('ﺭ', 'ﺮ', '\0', '\0'),
            ['ز'] = ('ﺯ', 'ﺰ', '\0', '\0'), ['س'] = ('ﺱ', 'ﺲ', 'ﺳ', 'ﺴ'),
            ['ش'] = ('ﺵ', 'ﺶ', 'ﺷ', 'ﺸ'), ['ص'] = ('ﺹ', 'ﺺ', 'ﺻ', 'ﺼ'),
            ['ض'] = ('ﺽ', 'ﺾ', 'ﺿ', 'ﻀ'), ['ط'] = ('ﻁ', 'ﻂ', 'ﻃ', 'ﻄ'),
            ['ظ'] = ('ﻅ', 'ﻆ', 'ﻇ', 'ﻈ'), ['ع'] = ('ﻉ', 'ﻊ', 'ﻋ', 'ﻌ'),
            ['غ'] = ('ﻍ', 'ﻎ', 'ﻏ', 'ﻐ'), ['ف'] = ('ﻑ', 'ﻒ', 'ﻓ', 'ﻔ'),
            ['ق'] = ('ﻕ', 'ﻖ', 'ﻗ', 'ﻘ'), ['ك'] = ('ﻙ', 'ﻚ', 'ﻛ', 'ﻜ'),
            ['ل'] = ('ﻝ', 'ﻞ', 'ﻟ', 'ﻠ'), ['م'] = ('ﻡ', 'ﻢ', 'ﻣ', 'ﻤ'),
            ['ن'] = ('ﻥ', 'ﻦ', 'ﻧ', 'ﻨ'), ['ه'] = ('ﻩ', 'ﻪ', 'ﻫ', 'ﻬ'),
            ['و'] = ('ﻭ', 'ﻮ', '\0', '\0'), ['ى'] = ('ﻯ', 'ﻰ', '\0', '\0'),
            ['ي'] = ('ﻱ', 'ﻲ', 'ﻳ', 'ﻴ'),
            // Persian
            ['پ'] = ('ﭖ', 'ﭗ', 'ﭘ', 'ﭙ'), ['چ'] = ('ﭺ', 'ﭻ', 'ﭼ', 'ﭽ'),
            ['ژ'] = ('ﮊ', 'ﮋ', '\0', '\0'), ['ک'] = ('ﮎ', 'ﮏ', 'ﮐ', 'ﮑ'),
            ['گ'] = ('ﮒ', 'ﮓ', 'ﮔ', 'ﮕ'), ['ی'] = ('ﯼ', 'ﯽ', 'ﯾ', 'ﯿ'),
        };

        // lam + alef: isolated, final
        static readonly Dictionary<char, (char iso, char fin)> LamAlef = new Dictionary<char, (char, char)>
        {
            ['آ'] = ('ﻵ', 'ﻶ'), ['أ'] = ('ﻷ', 'ﻸ'),
            ['إ'] = ('ﻹ', 'ﻺ'), ['ا'] = ('ﻻ', 'ﻼ'),
        };

        static bool Harakah(char c) => (c >= 'ً' && c <= 'ْ') || c == 'ٰ';
        static bool JoinsBefore(char c) => c == 'ـ' || (Forms.TryGetValue(c, out var f) && f.ini != '\0');   // joins the next letter
        static bool JoinsAfter(char c) => c == 'ـ' || (Forms.TryGetValue(c, out var f) && f.fin != '\0');    // joins the previous one
        static bool IsRtl(char c) => (c >= '֐' && c <= 'ࣿ') || (c >= 'יִ' && c <= '﷿') || (c >= 'ﹰ' && c <= '﻿');
        static bool IsLtr(char c) => char.IsLetterOrDigit(c) && !IsRtl(c);

        public static string Shape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = Visual(Join(lines[i]));
            return string.Join("\n", lines);
        }

        // logical order, letters in their joined forms
        static string Join(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!Forms.TryGetValue(c, out var f)) { sb.Append(c); continue; }
                char prev = Neighbour(s, i, -1), next = Neighbour(s, i, +1);
                bool joinPrev = prev != '\0' && JoinsBefore(prev) && f.fin != '\0';
                // lam-alef
                if (c == 'ل' && next != '\0' && LamAlef.TryGetValue(next, out var la))
                {
                    sb.Append(joinPrev ? la.fin : la.iso);
                    i = IndexOfNeighbour(s, i, +1);
                    continue;
                }
                bool joinNext = next != '\0' && JoinsAfter(next) && f.ini != '\0';
                sb.Append(joinPrev && joinNext ? f.med : joinPrev ? f.fin : joinNext ? f.ini : f.iso);
            }
            return sb.ToString();
        }

        static int IndexOfNeighbour(string s, int i, int dir)
        {
            for (int j = i + dir; j >= 0 && j < s.Length; j += dir) if (!Harakah(s[j])) return j;
            return -1;
        }

        static char Neighbour(string s, int i, int dir)
        {
            int j = IndexOfNeighbour(s, i, dir);
            return j < 0 ? '\0' : s[j];
        }

        static char Mirror(char c) => c switch { '(' => ')', ')' => '(', '[' => ']', ']' => '[', '{' => '}', '}' => '{', '<' => '>', '>' => '<', '«' => '»', '»' => '«', _ => c };

        // right-to-left line -> the order Unity lays it out in
        static string Visual(string s)
        {
            // runs: true = left-to-right (Latin letters and digits, with what sits between them)
            var runs = new List<(bool ltr, string text)>();
            int i = 0;
            while (i < s.Length)
            {
                if (IsLtr(s[i]))
                {
                    int j = i;
                    int lastLtr = i;
                    while (j < s.Length && !IsRtl(s[j])) { if (IsLtr(s[j])) lastLtr = j; j++; }
                    runs.Add((true, s.Substring(i, lastLtr + 1 - i)));
                    i = lastLtr + 1;
                }
                else
                {
                    int j = i;
                    while (j < s.Length && !IsLtr(s[j])) j++;
                    runs.Add((false, s.Substring(i, j - i)));
                    i = j;
                }
            }
            var sb = new StringBuilder(s.Length);
            for (int r = runs.Count - 1; r >= 0; r--)
            {
                var (ltr, text) = runs[r];
                if (ltr) { sb.Append(text); continue; }
                for (int k = text.Length - 1; k >= 0; k--) sb.Append(Mirror(text[k]));
            }
            return sb.ToString();
        }
    }
}
