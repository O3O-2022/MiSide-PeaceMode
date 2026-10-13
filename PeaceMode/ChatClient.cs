using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiSidePeaceMode
{
    // Talks to a local language model server with the OpenAI chat API (Ollama, llama.cpp's server, LM Studio...:
    // POST {endpoint} with {model, messages, max_tokens, temperature}). Runs off the game's thread; the answer (or
    // the error) comes back through Drain(), called from the director's Update.
    public static class ChatClient
    {
        public class Msg { public string role, content; public Msg(string r, string c) { role = r; content = c; } }

        static readonly HttpClient http = new HttpClient();
        static readonly ConcurrentQueue<Action> done = new ConcurrentQueue<Action>();
        static int running;

        public static bool Busy => running > 0;

        public static void Ask(string endpoint, string model, string apiKey, IList<Msg> messages, int maxTokens, float temperature, int timeoutSec,
                               Action<string> onAnswer, Action<string> onError)
        {
            Interlocked.Increment(ref running);
            var body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = BuildMessages(messages),
                ["max_tokens"] = maxTokens,
                ["temperature"] = temperature,
                ["stream"] = false,
            };
            var json = JsonSerializer.Serialize(body);
            Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, timeoutSec)));
                    using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                    if (!string.IsNullOrEmpty(apiKey)) req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
                    using var res = await http.SendAsync(req, cts.Token).ConfigureAwait(false);
                    var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!res.IsSuccessStatusCode) { done.Enqueue(() => onError($"HTTP {(int)res.StatusCode}: {Trim(text)}")); return; }
                    var answer = ReadAnswer(text);
                    if (answer == null) done.Enqueue(() => onError(Loc.T("응답에 내용이 없음") + ": " + Trim(text)));
                    else done.Enqueue(() => onAnswer(answer));
                }
                catch (TaskCanceledException) { done.Enqueue(() => onError(Loc.F("{0}초 안에 응답 없음", timeoutSec))); }
                catch (Exception e) { done.Enqueue(() => onError(e.GetBaseException().Message)); }
                finally { Interlocked.Decrement(ref running); }
            });
        }

        static List<Dictionary<string, string>> BuildMessages(IList<Msg> messages)
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var m in messages) list.Add(new Dictionary<string, string> { ["role"] = m.role, ["content"] = m.content });
            return list;
        }

        // choices[0].message.content (OpenAI / llama.cpp / LM Studio), or message.content (Ollama's own /api/chat)
        static string ReadAnswer(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var c = choices[0];
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
