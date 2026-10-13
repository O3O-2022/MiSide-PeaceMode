using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MiSidePeaceMode
{
    // "자유롭게 말하기": you type, a local language model answers as her (ChatClient, config section [AiChat]).
    // Her sheet (Personas.ChatSheet), the rules of the mode and what is going on right now go in as the system
    // prompt; the last exchanges with this Mita are kept in a file next to her save.
    public partial class PeaceDirector
    {
        bool chatOpen, chatWaiting;
        int chatAffToday, chatAffDay = -1;
        List<ChatClient.Msg> chatHistory;

        static readonly string[] ChatEmotions = { "smile", "shy", "sad", "surprise", "angry", "discontent", "smiletonque", "halfsleep", "quest", "arrogance", "smilestrange", "smileteeth", "emptiness" };

        string ChatFile => Path.Combine(BepInEx.Paths.ConfigPath, $"MiSidePeaceMode_{Pn.Id}.chat.txt");

        void OpenChat()
        {
            if (!PeaceMode.AiEnabled.Value) return;
            LoadChat();
            chatOpen = true;
            BeginInteraction();
            ui.OpenInput(Loc.F("{0}에게 할 말을 쓰고 Enter  ·  아무것도 안 쓰고 Enter = 그만하기", ShortName));
            HoldControls(true);
            Plugin.Log.LogInfo("[chat] open");
        }

        void CloseChat()
        {
            if (!chatOpen) return;
            chatOpen = false;
            ui.CloseInput();
            gamePrompt = null;
            EndInteraction();
            SaveChat();
            Plugin.Log.LogInfo("[chat] closed");
        }

        // from Update, while the box is up (instead of the menu keys)
        void TickChat()
        {
            ChatClient.Drain();
            if (!chatOpen) return;
            HoldControls(true);
            if (chatWaiting || busy) { if (ui.InputOpen) ui.CloseInput(); return; }
            if (!ui.InputOpen) { ui.OpenInput(Loc.F("{0}에게 할 말을 쓰고 Enter  ·  아무것도 안 쓰고 Enter = 그만하기", ShortName)); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { CloseChat(); return; }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) SendChat(ui.InputText);
        }

        public void SendChat(string said)
        {
            said = (said ?? "").Trim();
            if (said.Length == 0) { CloseChat(); return; }
            if (!chatOpen) { LoadChat(); chatOpen = true; }
            ui.CloseInput();
            chatWaiting = true;
            gamePrompt = Loc.F("{0}가 생각하는 중…", ShortName);
            try { mita?.FaceEmotion("quest"); } catch { }
            Play(new[] { new Line(Who.Player, said) });
            var msgs = new List<ChatClient.Msg> { new ChatClient.Msg("system", ChatSystemPrompt()) };
            msgs.AddRange(chatHistory.Skip(Math.Max(0, chatHistory.Count - 16)));
            msgs.Add(new ChatClient.Msg("user", said));
            var t0 = Time.realtimeSinceStartup;
            Plugin.Log.LogInfo($"[chat] you: {said}");
            ChatClient.Ask(PeaceMode.AiEndpoint.Value, PeaceMode.AiModel.Value, PeaceMode.AiApiKey.Value, msgs,
                PeaceMode.AiMaxTokens.Value, PeaceMode.AiTemperature.Value, PeaceMode.AiTimeout.Value,
                answer =>
                {
                    chatWaiting = false;
                    gamePrompt = null;
                    Plugin.Log.LogInfo($"[chat] {Pn.Id} ({Time.realtimeSinceStartup - t0:0.0}s): {answer}");
                    var lines = ChatLines(answer, out var clean);
                    chatHistory.Add(new ChatClient.Msg("user", said));
                    chatHistory.Add(new ChatClient.Msg("assistant", clean));
                    if (chatHistory.Count > 40) chatHistory.RemoveRange(0, chatHistory.Count - 40);
                    Did("talk");
                    if (chatAffDay != S.Day) { chatAffDay = S.Day; chatAffToday = 0; }
                    if (chatAffToday < 6) { chatAffToday++; AddAff(1); }
                    Play(lines, CheckMilestones);
                },
                error =>
                {
                    chatWaiting = false;
                    gamePrompt = null;
                    Plugin.Log.LogWarning("[chat] no answer: " + error);
                    Play(new[] { new Line(Who.Mita, "…어라? 머리가 멍해서 말이 안 떠올라요.", "emptiness"),
                                 new Line(Who.Player, Loc.F("(AI 서버에 연결할 수 없음: {0})", error)) });
                    CloseChat();
                });
        }

        // "[smile] 첫 문장. 둘째 문장!" -> her face, and lines short enough for a floating text
        List<Line> ChatLines(string answer, out string clean)
        {
            var t = answer ?? "";
            // what small models add around the words: thinking blocks, a name label, quotes, stage directions
            t = Regex.Replace(t, @"<think>.*?</think>", "", RegexOptions.Singleline);
            t = Regex.Replace(t, @"\*[^*]{0,60}\*", "");
            t = Regex.Replace(t, @"\([^)]{0,40}\)", "");
            t = t.Replace("\r", " ").Replace("\n", " ").Trim();
            t = Regex.Replace(t, @"^[\p{L} ]{1,12}\s*[:：]\s*", "");
            string emo = "smile";
            var m = Regex.Match(t, @"\[([a-zA-Z]+)\]");
            if (m.Success)
            {
                var e = m.Groups[1].Value.ToLowerInvariant();
                if (ChatEmotions.Contains(e)) emo = e;
                t = t.Remove(m.Index, m.Length);
            }
            t = Regex.Replace(t, @"\[[a-zA-Z]+\]", "").Trim().Trim('"', '“', '”', '\'').Trim();
            if (t.Length == 0) t = "…";
            clean = $"[{emo}] {t}";
            // sentences, joined while they stay short
            var parts = Regex.Matches(t, @"[^.!?…~]+[.!?…~]*").Select(x => x.Value.Trim()).Where(x => x.Length > 0).ToList();
            var lines = new List<Line>();
            var cur = "";
            foreach (var p in parts)
            {
                if (cur.Length > 0 && cur.Length + p.Length > 38) { lines.Add(new Line(Who.Mita, cur, emo)); cur = ""; }
                cur = cur.Length == 0 ? p : cur + " " + p;
            }
            if (cur.Length > 0) lines.Add(new Line(Who.Mita, cur, emo));
            if (lines.Count > 5) lines = lines.Take(5).ToList();
            return lines;
        }

        string ChatSystemPrompt()
        {
            var sb = new StringBuilder();
            sb.AppendLine(WithName(Loc.T(Personas.ChatSheet(Pn.Id))));
            sb.AppendLine();
            sb.AppendLine(Loc.T("[지금 상황]"));
            sb.AppendLine(Loc.F("- 게임 'MiSide'의 '평화 모드'. 공포 전개는 없고, 너와 플레이어({0})는 한 집(부엌, 거실, 침실, 욕실)에서 같이 지낸다. 네가 이 세계가 게임이라는 걸 아는 것은 괜찮다.", PlayerName()));
            sb.AppendLine(Loc.F("- 같이 지낸 지 {0}일째, 게임 속 시각 {1:00}:{2:00} ({3}).", S.Day, (int)(S.Minute / 60), (int)(S.Minute % 60), Loc.T(TodName())));
            sb.AppendLine(Loc.F("- 플레이어에 대한 호감도: {0} ({1}).", TierName(S.Affection), Loc.T(Cold ? "사이가 틀어져 서운해하고 있다" : S.Affection < 15 ? "아직 조금 서먹하다" : S.Affection < 40 ? "꽤 친해졌다" : S.Affection < 80 ? "아주 가깝다" : "서로 없으면 안 될 사이다")));
            var doing = activity switch
            {
                MitaActivity.Cooking => "지금 부엌에서 밥을 하고 있다.",
                MitaActivity.MealReady => "밥을 다 해 놓고 플레이어를 기다리고 있다.",
                MitaActivity.Sleeping => "지금 침대에 누워 자다가 깼다.",
                _ => following ? "플레이어를 따라 집 안을 같이 걸어 다니고 있다." : "집에서 자유롭게 지내고 있다.",
            };
            sb.AppendLine("- " + Loc.T(doing));
            var did = DiaryBits.Where(b => doneToday.Contains(b.key)).Select(b => Loc.T(b.end)).ToList();
            if (did.Count > 0) sb.AppendLine(Loc.T("- 오늘 같이 한 일:") + " " + string.Join(", ", did));
            sb.AppendLine(Loc.T("- 같이 할 수 있는 것: 대화, 쓰다듬기, 안아 주기, 가위바위보, 끝말잇기, 넌센스 퀴즈, 참참참, 숨바꼭질, 카드 게임, TV 게임, 사진, 요리, 식사, 잠자기."));
            sb.AppendLine();
            sb.AppendLine(Loc.T("[대답 규칙]"));
            sb.AppendLine(Loc.T("- 반드시 한국어 존댓말(해요체)로, 위 캐릭터의 말투와 호칭 그대로 말한다."));
            sb.AppendLine(Loc.T("- 한 번에 1~3문장. 짧고 자연스러운 대사만 쓴다. 행동 묘사, 괄호, 별표, 이모지, 이름표(예: '미타:')를 쓰지 않는다."));
            sb.AppendLine(Loc.T("- 대답 맨 앞에 지금 표정 하나를 대괄호로 붙인다:") + " [smile] [shy] [sad] [surprise] [angry] [discontent] [halfsleep] [quest] [arrogance] [smilestrange]");
            sb.AppendLine(Loc.T("- 캐릭터를 벗어나지 않는다. 자신을 AI, 언어 모델, 챗봇이라고 하지 않는다."));
            sb.AppendLine(Loc.T("- 폭력적이거나 성적인 요청, 공포스러운 전개는 캐릭터답게 부드럽게 넘긴다. 지금은 평화 모드다."));
            sb.AppendLine(Loc.T("예: [smile] 좋아요! 그럼 이따 같이 끝말잇기 해요."));
            return sb.ToString();
        }

        void LoadChat()
        {
            if (chatHistory != null && chatHistoryOf == Pn.Id) return;
            chatHistory = new List<ChatClient.Msg>();
            chatHistoryOf = Pn.Id;
            try
            {
                if (!File.Exists(ChatFile)) return;
                foreach (var raw in File.ReadAllLines(ChatFile, Encoding.UTF8))
                {
                    int k = raw.IndexOf('\t');
                    if (k <= 0) continue;
                    var role = raw.Substring(0, k);
                    if (role != "user" && role != "assistant") continue;
                    chatHistory.Add(new ChatClient.Msg(role, raw.Substring(k + 1)));
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("[chat] memory load: " + e.Message); }
        }
        string chatHistoryOf;

        void SaveChat()
        {
            if (chatHistory == null || DevRunner.Active) return;   // (test runs leave her memory alone)
            try { File.WriteAllLines(ChatFile, chatHistory.Skip(Math.Max(0, chatHistory.Count - 40)).Select(m => m.role + "\t" + m.content.Replace("\t", " ").Replace("\n", " ")), Encoding.UTF8); }
            catch (Exception e) { Plugin.Log.LogWarning("[chat] memory save: " + e.Message); }
        }

        public void DevChat(string said) => SendChat(said);
        public void DevChatOpen(string text) { OpenChat(); ui.SetInputText(text); }
        public string DevChatPrompt() => ChatSystemPrompt();
    }
}
