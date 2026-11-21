using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MiSidePeaceMode
{
    // "자유롭게 말하기", grown up (2026-10):
    //  - her body: besides a face, the model may add one action tag ([nod] [wave] [shy]...) and she does it (PeaceMotion);
    //  - a long memory: when a talk ends, the model writes down what it learned about you (a dozen short lines, merged
    //    with what it knew), and that goes into every later talk - she remembers your name, what you like, promises;
    //  - set up in the game (Settings > AI 대화): a server preset, the model picked from the server's own list, an API
    //    key, a connection test, forgetting.
    public partial class PeaceDirector
    {
        // tag -> motion
        static readonly Dictionary<string, string> ChatMotions = new Dictionary<string, string>
        {
            ["nod"] = "@nod", ["shake"] = "@shake", ["tilt"] = "@tilt", ["shy"] = "@shy", ["lookaway"] = "@lookaway",
            ["wave"] = "@wave", ["cheer"] = "@cheer", ["explain"] = "@explain",
        };

        string MemoryFile => Path.Combine(BepInEx.Paths.ConfigPath, $"MiSidePeaceMode_{Pn.Id}.memory.txt");
        List<string> memory;
        string memoryOf;
        int chatSinceMemory;
        bool memoryWriting;

        List<string> Memory()
        {
            if (memory != null && memoryOf == Pn.Id) return memory;
            memoryOf = Pn.Id;
            memory = new List<string>();
            try { if (File.Exists(MemoryFile)) memory = File.ReadAllLines(MemoryFile, Encoding.UTF8).Select(l => l.Trim()).Where(l => l.StartsWith("-")).Take(14).ToList(); }
            catch (Exception e) { Plugin.Log.LogWarning("[chat] memory: " + e.Message); }
            return memory;
        }

        void SaveMemory()
        {
            if (DevRunner.Active) return;   // (test runs leave her memory alone)
            try { File.WriteAllLines(MemoryFile, memory, Encoding.UTF8); } catch (Exception e) { Plugin.Log.LogWarning("[chat] memory save: " + e.Message); }
        }

        // the part of her prompt with what she remembers
        string MemoryPrompt()
        {
            var m = Memory();
            if (m.Count == 0) return "";
            return Loc.T("[플레이어에 대해 기억하고 있는 것]") + "\n" + string.Join("\n", m) + "\n" + Loc.T("- 위 기억은 자연스럽게 활용하되, 매번 늘어놓지는 않는다.") + "\n";
        }

        // after a talk: what's worth keeping, in a few lines (in the background; the game goes on)
        void WriteMemory()
        {
            if (memoryWriting || chatSinceMemory < 2 || chatHistory == null || !PeaceMode.AiEnabled.Value) return;
            memoryWriting = true;
            int took = chatSinceMemory;
            var recent = chatHistory.Skip(Math.Max(0, chatHistory.Count - Math.Min(24, took * 2))).ToList();
            var talk = new StringBuilder();
            foreach (var m in recent) talk.AppendLine((m.role == "user" ? PlayerName() : Loc.T(Pn.Short)) + ": " + m.content);
            var msgs = new List<ChatClient.Msg>
            {
                new ChatClient.Msg("system", Loc.T("너는 기록 담당이다. 대화에서 플레이어에 대해 알게 된 것(이름, 좋아하는 것과 싫어하는 것, 하는 일, 약속, 기분, 같이 한 일)만 골라, 원래 기억과 합쳐 최대 12줄로 정리한다. 각 줄은 '- '로 시작하는 짧은 한국어 문장. 중요하지 않거나 겹치는 것은 버린다. 다른 말은 쓰지 않는다.")),
                new ChatClient.Msg("user", Loc.T("[원래 기억]") + "\n" + (Memory().Count > 0 ? string.Join("\n", Memory()) : "-") + "\n\n" + Loc.T("[새 대화]") + "\n" + talk),
            };
            ChatClient.Ask(PeaceMode.AiEndpoint.Value, PeaceMode.AiModel.Value, PeaceMode.AiApiKey.Value, msgs, 400, 0.2f, Math.Max(60, PeaceMode.AiTimeout.Value),
                answer =>
                {
                    memoryWriting = false;
                    var lines = (answer ?? "").Replace("\r", "").Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("-") && l.Length > 2)
                                              .Select(l => l.Length > 90 ? l.Substring(0, 90) : l).Distinct().Take(12).ToList();
                    if (lines.Count == 0) { Plugin.Log.LogWarning("[chat] memory: nothing usable in " + answer); return; }
                    memory = lines;
                    memoryOf = Pn.Id;
                    chatSinceMemory = Math.Max(0, chatSinceMemory - took);
                    SaveMemory();
                    Plugin.Log.LogInfo($"[chat] memory: {lines.Count} lines\n  " + string.Join("\n  ", lines));
                },
                error => { memoryWriting = false; Plugin.Log.LogWarning("[chat] memory: " + error); });
        }

        public void DevChatClose() => CloseChat();

        // ---------------------------------------------------------------- Settings > AI 대화

        // (the clouds need a key, made by the player on the provider's site; the model to start with is one that is
        // quick and cheap - the model list has the rest)
        static readonly (string name, string url, string model)[] AiPresets =
        {
            ("Gemini", "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "gemini-2.5-flash"),
            ("ChatGPT", "https://api.openai.com/v1/chat/completions", "gpt-5-mini"),
            ("Ollama", "http://127.0.0.1:11434/v1/chat/completions", null),
            ("LM Studio", "http://127.0.0.1:1234/v1/chat/completions", null),
            ("llama.cpp", "http://127.0.0.1:8080/v1/chat/completions", null),
            ("OpenRouter", "https://openrouter.ai/api/v1/chat/completions", null),
        };

        void PickAiPreset(int i)
        {
            var p = AiPresets[i];
            PeaceMode.AiEndpoint.Value = p.url;
            // (a model name from another server means nothing here)
            PeaceMode.AiModel.Value = p.model ?? "";
            aiModels = null;
            aiModelIndex = -1;
        }
        List<string> aiModels;
        int aiModelIndex = -1;
        bool aiFetching, aiTesting;

        int AiPresetIndex() => Array.FindIndex(AiPresets, p => string.Equals(p.url, (PeaceMode.AiEndpoint.Value ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

        void OpenAiMenu()
        {
            int pi = AiPresetIndex();
            string model = string.IsNullOrWhiteSpace(PeaceMode.AiModel.Value) ? Loc.T("서버 기본값") : PeaceMode.AiModel.Value.Trim();
            var items = new List<MenuItem>
            {
                new MenuItem("자유롭게 말하기") { Hint = Loc.T(PeaceMode.AiEnabled.Value ? "켜짐" : "꺼짐") },
                new MenuItem("AI 서버") { Hint = pi >= 0 ? AiPresets[pi].name : Loc.T("직접 설정") },
                new MenuItem("모델") { Hint = aiFetching ? Loc.T("불러오는 중…") : model },
                new MenuItem("API 키") { Hint = Loc.T(string.IsNullOrWhiteSpace(PeaceMode.AiApiKey.Value) ? (ChatClient.IsCloud(PeaceMode.AiEndpoint.Value) ? "필요해요" : "없음") : "설정됨") },
                new MenuItem("연결 시험") { Hint = aiTesting ? Loc.T("기다리는 중…") : null },
                new MenuItem("기억 지우기") { Enabled = Memory().Count > 0, Hint = Memory().Count > 0 ? Loc.F("{0}줄", Memory().Count) : Loc.T("아직 없음") },
                new MenuItem("그만두기") { Back = true },
            };
            var acts = new List<Action>
            {
                () => { PeaceMode.AiEnabled.Value = !PeaceMode.AiEnabled.Value; RefreshAi(); ui.Toast(Loc.T("저장했어요")); },
                () => { PickAiPreset((pi + 1) % AiPresets.Length); RefreshAi(); ui.Toast(Loc.T("저장했어요")); },
                NextAiModel,
                () => StartTyping(Typing.ApiKey),
                TestAi,
                () => { memory = new List<string>(); memoryOf = Pn.Id; SaveMemory(); chatHistory = new List<ChatClient.Msg>(); chatHistoryOf = Pn.Id; SaveChat(); RefreshAi(); ui.Toast(Loc.F("{0}가 기억을 잊었어요", ShortName)); },
                () => { },
            };
            ShowItems("AI 대화", items, acts, OpenAiMenu);
        }

        // (a late answer - the model list, the test - redraws the menu only if it is still the one up)
        void RefreshAiIfShown() { if (ui.PanelOpen && shownMenu == "AI 대화") RefreshAi(); }

        void RefreshAi()
        {
            choosingFrom = null;
            reopening = refreshing = true;
            try { OpenAiMenu(); } finally { reopening = refreshing = false; }
        }

        // the models the server has (asked once), then each pick takes the next one
        void NextAiModel()
        {
            if (aiModels != null && aiModels.Count > 0)
            {
                aiModelIndex = (aiModelIndex + 1) % aiModels.Count;
                PeaceMode.AiModel.Value = aiModels[aiModelIndex];
                RefreshAi();
                return;
            }
            aiFetching = true;
            RefreshAi();
            ChatClient.ListModels(PeaceMode.AiEndpoint.Value, PeaceMode.AiApiKey.Value, list =>
            {
                aiFetching = false;
                aiModels = list;
                aiModelIndex = Math.Max(0, list.IndexOf((PeaceMode.AiModel.Value ?? "").Trim()));
                if (list.Count > 0) PeaceMode.AiModel.Value = list[aiModelIndex];
                ui.Toast(list.Count > 0 ? Loc.F("모델 {0}개", list.Count) : Loc.T("서버에 모델이 없어요"), list.Count == 0);
                RefreshAiIfShown();
            }, error =>
            {
                aiFetching = false;
                ui.Toast(Loc.T("서버에 연결할 수 없어요"), true);
                Plugin.Log.LogWarning("[chat] models: " + error);
                RefreshAiIfShown();
            });
        }

        void TestAi()
        {
            if (aiTesting) return;
            aiTesting = true;
            RefreshAi();
            float t0 = Time.realtimeSinceStartup;
            var msgs = new List<ChatClient.Msg> { new ChatClient.Msg("user", "Say hi in one word.") };
            ChatClient.Ask(PeaceMode.AiEndpoint.Value, PeaceMode.AiModel.Value, PeaceMode.AiApiKey.Value, msgs, 16, 0.2f, Math.Max(20, PeaceMode.AiTimeout.Value),
                answer => { aiTesting = false; ui.Toast(Loc.F("연결됐어요 ({0:0.0}초)", Time.realtimeSinceStartup - t0)); RefreshAiIfShown(); },
                error => { aiTesting = false; ui.Toast(Loc.T("연결 실패") + ": " + (error.Length > 40 ? error.Substring(0, 40) + "…" : error), true); Plugin.Log.LogWarning("[chat] test: " + error); RefreshAiIfShown(); });
        }
    }
}
