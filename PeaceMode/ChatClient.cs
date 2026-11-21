using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiSidePeaceMode
{
    // Talks to a language model server with the OpenAI chat API - local (Ollama, llama.cpp's server, LM Studio) or a
    // cloud one (Google Gemini's OpenAI-compatible endpoint, OpenAI's ChatGPT API, OpenRouter):
    // POST {endpoint} with {model, messages, max_tokens, temperature}). Runs off the game's thread; the answer (or
    // the error) comes back through Drain(), called from the director's Update.
    public static class ChatClient
    {
        public class Msg { public string role, content; public Msg(string r, string c) { role = r; content = c; } }

        static readonly HttpClient http = new HttpClient();
        static readonly ConcurrentQueue<Action> done = new ConcurrentQueue<Action>();
        static int running;

        public static bool Busy => running > 0;

        // What each server/model refused, learned from its 400 answers and kept for the session (key: endpoint|model):
        //  - "reasoning_effort": thinking models (gemma4, qwen3, gpt-5, gemini 2.5) would spend the answer's tokens
        //    thinking; we ask for "none", then "minimal", then "low", and leave the field out if all are refused;
        //  - OpenAI's newer models take "max_completion_tokens" instead of "max_tokens", and some only the default
        //    temperature.
        class Quirks { public int effort; public bool completionTokens, noTemperature; }
        static readonly ConcurrentDictionary<string, Quirks> quirks = new ConcurrentDictionary<string, Quirks>();
        static readonly string[] Efforts = { "none", "minimal", "low", null };

        public static bool IsGemini(string endpoint) => (endpoint ?? "").IndexOf("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase) >= 0;
        public static bool IsOpenAI(string endpoint) => (endpoint ?? "").IndexOf("api.openai.com", StringComparison.OrdinalIgnoreCase) >= 0;
        public static bool IsCloud(string endpoint) => IsGemini(endpoint) || IsOpenAI(endpoint) || (endpoint ?? "").IndexOf("openrouter.ai", StringComparison.OrdinalIgnoreCase) >= 0;

        public static void Ask(string endpoint, string model, string apiKey, IList<Msg> messages, int maxTokens, float temperature, int timeoutSec,
                               Action<string> onAnswer, Action<string> onError)
        {
            endpoint = (endpoint ?? "").Trim();
            model = (model ?? "").Trim();
            apiKey = (apiKey ?? "").Trim();
            if (IsCloud(endpoint) && apiKey.Length == 0) { done.Enqueue(() => onError(Loc.T("API 키가 필요해요 (설정 › AI 대화 › API 키)"))); return; }
            Interlocked.Increment(ref running);
            var q = quirks.GetOrAdd(endpoint + "|" + model, _ => new Quirks());
            string Body(int tokens)
            {
                var body = new Dictionary<string, object>
                {
                    ["messages"] = BuildMessages(messages),
                    ["stream"] = false,
                };
                if (model.Length > 0) body["model"] = model;
                body[q.completionTokens ? "max_completion_tokens" : "max_tokens"] = tokens;
                if (!q.noTemperature) body["temperature"] = temperature;
                var effort = Efforts[Math.Min(q.effort, Efforts.Length - 1)];
                if (effort != null) body["reasoning_effort"] = effort;
                return JsonSerializer.Serialize(body);
            }
            Task.Run(async () =>
            {
                try
                {
                    int tokens = maxTokens;
                    for (int attempt = 0; attempt < 7; attempt++)
                    {
                        var json = Body(tokens);
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, timeoutSec)));
                        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                        if (apiKey.Length > 0) req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
                        using var res = await http.SendAsync(req, cts.Token).ConfigureAwait(false);
                        var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        int code = (int)res.StatusCode;
                        if (code == 400 || code == 422)
                        {
                            // what the server didn't like, if it's one of the fields we can do without
                            var low = text.ToLowerInvariant();
                            if (low.Contains("max_tokens") && !q.completionTokens) { q.completionTokens = true; continue; }
                            if (low.Contains("temperature") && !q.noTemperature) { q.noTemperature = true; continue; }
                            if ((low.Contains("reasoning") || low.Contains("thinking")) && q.effort < Efforts.Length - 1) { q.effort++; continue; }
                        }
                        if (!res.IsSuccessStatusCode) { done.Enqueue(() => onError(ErrorText(code, text))); return; }
                        var answer = ReadAnswer(text, out var finish);
                        // (a thinking model that used the whole budget before saying anything: once more with room)
                        if (string.IsNullOrWhiteSpace(answer) && finish == "length" && tokens < 4096) { tokens = Math.Min(4096, Math.Max(tokens * 4, 1024)); continue; }
                        if (string.IsNullOrWhiteSpace(answer)) done.Enqueue(() => onError(Loc.T("응답에 내용이 없음") + ": " + Trim(text)));
                        else done.Enqueue(() => onAnswer(answer));
                        return;
                    }
                    done.Enqueue(() => onError(Loc.T("서버가 요청을 계속 거절함")));
                }
                catch (TaskCanceledException) { done.Enqueue(() => onError(Loc.F("{0}초 안에 응답 없음", timeoutSec))); }
                catch (Exception e) { done.Enqueue(() => onError(e.GetBaseException().Message)); }
                finally { Interlocked.Decrement(ref running); }
            });
        }

        // the usual cloud errors in words a player can act on
        static string ErrorText(int code, string text)
        {
            string why = code switch
            {
                401 => Loc.T("API 키가 틀렸어요"),
                403 => Loc.T("이 키로는 쓸 수 없어요 (권한 · 지역)"),
                404 => Loc.T("모델이나 주소를 찾을 수 없어요"),
                429 => Loc.T("사용량 한도에 걸렸어요 (잠시 뒤 · 결제 확인)"),
                _ => null,
            };
            // (Gemini says a bad key with 400 API_KEY_INVALID)
            if (why == null && text != null && text.IndexOf("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase) >= 0) why = Loc.T("API 키가 틀렸어요");
            return (why != null ? why + " · " : "") + $"HTTP {code}: {Trim(text)}";
        }

        // the server's models: GET {endpoint without /chat/completions}/models -> data[].id (OpenAI, Ollama, LM Studio,
        // llama.cpp, OpenRouter all answer this)
        public static void ListModels(string endpoint, string apiKey, Action<List<string>> onList, Action<string> onError)
        {
            var url = (endpoint ?? "").Trim();
            int k = url.IndexOf("/chat/completions", StringComparison.OrdinalIgnoreCase);
            url = (k > 0 ? url.Substring(0, k) : url.TrimEnd('/')) + "/models";
            Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrEmpty(apiKey)) req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
                    using var res = await http.SendAsync(req, cts.Token).ConfigureAwait(false);
                    var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!res.IsSuccessStatusCode) { done.Enqueue(() => onError($"HTTP {(int)res.StatusCode}: {Trim(text)}")); return; }
                    var list = new List<string>();
                    using (var doc = JsonDocument.Parse(text))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("data", out var data))
                            foreach (var m in data.EnumerateArray()) if (m.TryGetProperty("id", out var id)) list.Add(id.GetString());
                        if (root.TryGetProperty("models", out var models))   // (Ollama's own /api/tags)
                            foreach (var m in models.EnumerateArray()) if (m.TryGetProperty("name", out var n)) list.Add(n.GetString());
                    }
                    // (the clouds list every model they have - pictures, speech, embeddings - keep the ones that chat)
                    if (IsGemini(endpoint) || IsOpenAI(endpoint))
                        list = list.Select(m => m.StartsWith("models/") ? m.Substring(7) : m).Where(ChatModel).Distinct().ToList();
                    list.Sort(StringComparer.OrdinalIgnoreCase);
                    done.Enqueue(() => onList(list));
                }
                catch (Exception e) { done.Enqueue(() => onError(e.GetBaseException().Message)); }
            });
        }

        static bool ChatModel(string id)
        {
            var m = (id ?? "").ToLowerInvariant();
            if (!(m.StartsWith("gemini") || m.StartsWith("gpt-") || m.StartsWith("chatgpt") || System.Text.RegularExpressions.Regex.IsMatch(m, @"^o\d"))) return false;
            foreach (var bad in new[] { "embed", "tts", "audio", "image", "realtime", "transcribe", "whisper", "dall", "moderation", "search", "live", "computer-use", "codex", "instruct", "aqa", "veo", "imagen", "learnlm", "robotics" })
                if (m.Contains(bad)) return false;
            return true;
        }

        static List<Dictionary<string, string>> BuildMessages(IList<Msg> messages)
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var m in messages) list.Add(new Dictionary<string, string> { ["role"] = m.role, ["content"] = m.content });
            return list;
        }

        // choices[0].message.content (OpenAI / llama.cpp / LM Studio), or message.content (Ollama's own /api/chat)
        static string ReadAnswer(string json, out string finish)
        {
            finish = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var c = choices[0];
                    if (c.TryGetProperty("finish_reason", out var fr) && fr.ValueKind == JsonValueKind.String) finish = fr.GetString();
                    if (c.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content)) return content.GetString();
                    if (c.TryGetProperty("text", out var t)) return t.GetString();
                }
                if (root.TryGetProperty("message", out var m2) && m2.TryGetProperty("content", out var c2)) return c2.GetString();
            }
            catch { }
            return null;
        }

        static string Trim(string s) => s == null ? "" : (s.Length > 200 ? s.Substring(0, 200) + "…" : s);

        // on the game's thread
        public static void Drain()
        {
            while (done.TryDequeue(out var a))
                try { a(); } catch (Exception e) { Plugin.Log.LogError("[chat] " + e); }
        }
    }
}
