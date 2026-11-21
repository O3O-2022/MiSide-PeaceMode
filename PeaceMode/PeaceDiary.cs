using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // Her diary (2026-10): every game day keeps what the two of you did (the keys of Did(), as for her bedtime diary),
    // the weather, the real date and how her feelings moved, in a file of its own; 추억 > 미타의 일기 shows the pages
    // as she'd write them, newest first, turned like the album.
    public class DiaryDay
    {
        public int Day, AffStart, AffEnd;
        public string Date = "", Weather = "";
        public readonly List<string> Keys = new List<string>();
    }

    public partial class PeaceDirector
    {
        string DiaryFile => Path.Combine(BepInEx.Paths.ConfigPath, $"MiSidePeaceMode_{Pn.Id}.diary.txt");
        List<DiaryDay> diary;
        string diaryOf;
        float diarySaveAt = -1f;

        List<DiaryDay> DiaryBook()
        {
            if (diary != null && diaryOf == Pn.Id) return diary;
            diaryOf = Pn.Id;
            diary = new List<DiaryDay>();
            try
            {
                if (File.Exists(DiaryFile))
                    foreach (var raw in File.ReadAllLines(DiaryFile, Encoding.UTF8))
                    {
                        // day|yyyy-MM-dd|weather|affStart|affEnd|key,key
                        var p = raw.Split('|');
                        if (p.Length < 6 || !int.TryParse(p[0], out var day)) continue;
                        var d = new DiaryDay { Day = day, Date = p[1], Weather = p[2] };
                        int.TryParse(p[3], out d.AffStart); int.TryParse(p[4], out d.AffEnd);
                        d.Keys.AddRange(p[5].Split(',', StringSplitOptions.RemoveEmptyEntries));
                        diary.Add(d);
                    }
            }
            catch (Exception e) { Plugin.Log.LogWarning("[diary] load: " + e.Message); }
            return diary;
        }

        // today's page (made when first needed)
        DiaryDay DiaryToday()
        {
            var book = DiaryBook();
            var d = book.LastOrDefault(x => x.Day == S.Day);
            if (d == null)
            {
                d = new DiaryDay { Day = S.Day, Date = DateTime.Now.ToString("yyyy-MM-dd"), AffStart = S.Affection, AffEnd = S.Affection };
                book.Add(d);
                if (book.Count > 400) book.RemoveAt(0);
            }
            return d;
        }

        // from Did(): one more thing on today's page (written out a moment later, not on every call)
        void DiaryNote(string key)
        {
            if (string.IsNullOrEmpty(key) || !DiaryBits.Any(b => b.key == key)) return;
            var d = DiaryToday();
            if (!d.Keys.Contains(key)) d.Keys.Add(key);
            d.AffEnd = S.Affection;
            d.Weather = weather.ToString();
            diarySaveAt = Time.realtimeSinceStartup + 2f;
        }

        void TickDiary()
        {
            if (diarySaveAt < 0f || Time.realtimeSinceStartup < diarySaveAt) return;
            diarySaveAt = -1f;
            SaveDiary();
        }

        void SaveDiary()
        {
            if (diary == null || DevRunner.Active) return;   // (test runs leave her diary alone)
            try
            {
                if (diary.Count > 0 && diary[diary.Count - 1].Day == S.Day) diary[diary.Count - 1].AffEnd = S.Affection;
                File.WriteAllLines(DiaryFile, diary.Select(d => $"{d.Day}|{d.Date}|{d.Weather}|{d.AffStart}|{d.AffEnd}|{string.Join(",", d.Keys)}"), Encoding.UTF8);
            }
            catch (Exception e) { Plugin.Log.LogWarning("[diary] save: " + e.Message); }
        }

        static string WeatherWord(string w) => w switch { "Cloudy" => "흐림", "Rain" => "비", "Snow" => "눈", _ => "맑음" };
        static string WeatherMark(string w) => w switch { "Cloudy" => "☁", "Rain" => "☂", "Snow" => "❄", _ => "☀" };

        // the page as she'd write it
        string DiaryText(DiaryDay d)
        {
            var sb = new StringBuilder();
            var lines = Personas.DiaryLines(Pn.Id);
            var bits = DiaryBits.Where(b => d.Keys.Contains(b.key)).ToList();
            string Say(Line[] ls) => ls != null && ls.Length > 0 ? WithName(Loc.T(ls[0].text)) : "";
            if (d.Day == 1) sb.AppendLine(Loc.F("처음 만난 날. {0}님이 우리 집에 왔어요.", PlayerName()));
            if (bits.Count == 0) sb.AppendLine(Say(lines.quiet));
            else
            {
                for (int i = 0; i < bits.Count; i += 2)
                {
                    var part = bits.Skip(i).Take(2).ToList();
                    var joined = string.Join(Loc.T(", "), part.Take(part.Count - 1).Select(b => Loc.T(b.and)).Append(Loc.T(part.Last().end)));
                    sb.AppendLine(Loc.F(i == 0 ? "오늘은 {0}." : "그리고 {0}.", joined));
                }
            }
            int moved = d.AffEnd - d.AffStart;
            sb.AppendLine();
            sb.AppendLine(Loc.T(moved >= 8 ? "정말, 정말 행복한 하루였어요. 오늘은 오래 기억할 거예요."
                              : moved >= 3 ? "조금 더 가까워진 것 같아서 기분이 좋아요."
                              : moved < 0 ? "조금 서운한 일도 있었지만… 내일은 더 좋을 거예요."
                              : bits.Count > 0 ? "평범하지만 따뜻한 하루였어요." : "그래도 곁에 있어 줘서 좋았어요."));
            if (bits.Count > 0) sb.AppendLine(Say(lines.outro));
            return sb.ToString().TrimEnd();
        }

        List<DiaryDay> diaryPages;
        int diaryIndex, diaryTurn;

        void OpenDiary()
        {
            SaveDiary();
            diaryPages = DiaryBook().Where(d => d.Keys.Count > 0 || d.Day == 1 || d.Day == S.Day).OrderByDescending(d => d.Day).ToList();
            if (diaryPages.Count == 0) { ui.Toast(Loc.T("아직 쓴 일기가 없어요"), true); return; }
            cardReturn = choosingFrom ?? cardReturn;
            diaryIndex = 0; diaryTurn = 0;
            cardKind = CardKind.Diary;
            HoldControls(true);
            ShowDiaryPage();
        }

        void ShowDiaryPage()
        {
            diaryIndex = (diaryIndex % diaryPages.Count + diaryPages.Count) % diaryPages.Count;
            var d = diaryPages[diaryIndex];
            string date = DateTime.TryParse(d.Date, out var dt) ? dt.ToString("yyyy. M. d.") : d.Date;
            string head = Loc.F("{0}일차", d.Day) + "   ·   " + date + "   ·   " + WeatherMark(d.Weather) + " " + Loc.T(WeatherWord(d.Weather));
            ui.ShowPage(Loc.F("{0}의 일기", ShortName), head, DiaryText(d), $"{diaryIndex + 1} / {diaryPages.Count}",
                        Loc.T(PeacePad.InUse ? "LB RB 넘기기   ·   Ⓑ 닫기" : "← → 넘기기   ·   Esc 닫기"), diaryTurn);
            diaryTurn = 0;
        }

        void TurnDiary(int dir)
        {
            if (diaryPages == null || diaryPages.Count < 2) return;
            diaryTurn = dir;
            diaryIndex += dir;
            ShowDiaryPage();
        }

        public void DevDiary(string arg)
        {
            // diary fill: a week of made-up pages (in memory only), diary: open it, diary 1/-1: turn
            if (arg == "fill")
            {
                var keys = DiaryBits.Select(b => b.key).ToArray();
                var book = DiaryBook();
                for (int i = 1; i <= 6; i++)
                {
                    var d = new DiaryDay { Day = i, Date = DateTime.Now.AddDays(i - 7).ToString("yyyy-MM-dd"), Weather = ((Weather)(i % 4)).ToString(), AffStart = i * 4, AffEnd = i * 4 + (i % 3) * 4 - 1 };
                    for (int k = 0; k < 2 + i % 4; k++) d.Keys.Add(keys[(i * 5 + k * 3) % keys.Length]);
                    book.Insert(0, d);
                }
                return;
            }
            if (cardKind == CardKind.Diary && int.TryParse(arg, out var t)) TurnDiary(t); else OpenDiary();
        }
    }
}
