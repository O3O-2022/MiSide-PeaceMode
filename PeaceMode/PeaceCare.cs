using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // Looking after things (2026-10): reasons to come back tomorrow.
    //  - Her plant: the little flower on the kitchen counter is "ours". Watered together once a (real) day it grows a
    //    step every two waterings, from a sprout to a flower; left three days without water it droops.
    //  - Her mood: up with what you do together, down slowly when nothing happens; shown on the status card. Bored,
    //    she comes to you with ideas more often; happy, she moves more.
    public partial class PeaceDirector
    {
        // ---------------------------------------------------------------- the plant

        Transform plant;   // the vase she keeps it next to
        Vector3 plantScale0;
        Quaternion plantRot0;
        float plantBounceAt = -10f, nextPlantFind;

        static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        int PlantStage => Mathf.Clamp(S.PlantDays / 2, 0, 4);   // 0 sprout .. 4 in flower
        bool PlantWateredToday => S.PlantWatered == Today;
        int PlantDry
        {
            get
            {
                if (string.IsNullOrEmpty(S.PlantWatered) || !DateTime.TryParse(S.PlantWatered, out var w)) return 0;
                return (int)(DateTime.Today - w.Date).TotalDays;
            }
        }
        bool PlantWilted => S.PlantDays > 0 && PlantDry >= 3;
        static readonly string[] PlantStageNames = { "새싹", "떡잎", "줄기", "꽃봉오리", "활짝 핀 꽃" };

        // (the vase with a flower on the kitchen counter is one mesh: scaled, the vase would grow too. Next to it stands a
        // little pot of ours instead, made of simple shapes: soil, a stem, leaves by the stage, a bud, then a flower)
        Transform potRoot, potPlant;
        readonly List<GameObject> potParts = new List<GameObject>();
        int potBuiltStage = -2;
        bool potBuiltWilted;

        void FindPlant()
        {
            if (plant != null || Time.time < nextPlantFind) return;
            nextPlantFind = Time.time + 5f;
            Transform best = null;
            float bestD = 4f;
            var want = new Vector3(14.10f, 0.94f, 2.34f);
            foreach (var t in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
            {
                if (t == null || t.gameObject.name != "TableFlower") continue;
                float d = Vector3.Distance(t.transform.position, want);
                if (d < bestD) { bestD = d; best = t.transform; }
            }
            if (best == null) return;
            plant = best;
            plantScale0 = plant.localScale;
            plantRot0 = plant.localRotation;
            Plugin.Log.LogInfo($"[care] plant next to {Util.PathOf(plant)} at {plant.position}");
        }

        static GameObject Shape(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = go.GetComponent<MeshRenderer>();
            if (r != null) { r.material.color = c; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            return go;
        }

        void BuildPot()
        {
            if (potRoot != null || plant == null) return;
            var root = new GameObject("PeacePlantPot");
            potRoot = root.transform;
            // beside the vase, along the counter
            potRoot.position = plant.position + plant.right * 0.24f;
            potRoot.rotation = Quaternion.Euler(0f, plant.eulerAngles.y, 0f);
            var clay = new Color(0.82f, 0.46f, 0.30f);
            Shape(PrimitiveType.Cylinder, potRoot, new Vector3(0, 0.045f, 0), new Vector3(0.11f, 0.045f, 0.11f), clay);
            Shape(PrimitiveType.Cylinder, potRoot, new Vector3(0, 0.088f, 0), new Vector3(0.125f, 0.008f, 0.125f), clay * 0.92f);
            Shape(PrimitiveType.Cylinder, potRoot, new Vector3(0, 0.091f, 0), new Vector3(0.1f, 0.004f, 0.1f), new Color(0.26f, 0.17f, 0.12f));
            potPlant = new GameObject("Plant").transform;
            potPlant.SetParent(potRoot, false);
            potPlant.localPosition = new Vector3(0, 0.093f, 0);
        }

        // the plant for its stage (rebuilt when the stage or the wilting changes)
        void BuildStage(int stage, bool wilted)
        {
            foreach (var g in potParts) if (g != null) UnityEngine.Object.Destroy(g);
            potParts.Clear();
            potBuiltStage = stage; potBuiltWilted = wilted;
            if (stage < 0) return;
            var green = wilted ? new Color(0.62f, 0.62f, 0.30f) : new Color(0.33f, 0.68f, 0.32f);
            float[] heights = { 0.035f, 0.07f, 0.11f, 0.14f, 0.16f };
            float h = heights[stage];
            potParts.Add(Shape(PrimitiveType.Cylinder, potPlant, new Vector3(0, h / 2f, 0), new Vector3(0.009f, h / 2f, 0.009f), green));
            // leaves: two at the top for the first ones, then up the stem
            int leaves = stage == 0 ? 2 : 2 + stage * 2;
            for (int i = 0; i < leaves; i++)
            {
                float y = stage == 0 ? h : Mathf.Lerp(0.03f, h * 0.85f, (float)(i / 2) / Mathf.Max(1, leaves / 2 - 1));
                float side = i % 2 == 0 ? 1f : -1f;
                float yaw = (i / 2) * 70f;
                var leaf = Shape(PrimitiveType.Sphere, potPlant, Quaternion.Euler(0, yaw, 0) * new Vector3(side * 0.022f, y, 0),
                                 new Vector3(0.04f, 0.007f, 0.02f) * (stage == 0 ? 0.7f : 1f), green, new Vector3(0, yaw, side * -25f));
                potParts.Add(leaf);
            }
            if (stage == 3)
                potParts.Add(Shape(PrimitiveType.Sphere, potPlant, new Vector3(0, h + 0.012f, 0), new Vector3(0.022f, 0.03f, 0.022f), new Color(1f, 0.55f, 0.72f)));
            if (stage == 4)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f;
                    potParts.Add(Shape(PrimitiveType.Sphere, potPlant, new Vector3(0, h + 0.01f, 0) + Quaternion.Euler(0, a, 0) * new Vector3(0.028f, 0, 0),
                                       new Vector3(0.046f, 0.01f, 0.026f), new Color(1f, 0.62f, 0.8f), new Vector3(0, a, 12f)));
                }
                potParts.Add(Shape(PrimitiveType.Sphere, potPlant, new Vector3(0, h + 0.015f, 0), new Vector3(0.022f, 0.016f, 0.022f), new Color(1f, 0.85f, 0.3f)));
            }
        }

        void TickPlant()
        {
            FindPlant();
            if (plant == null) return;
            BuildPot();
            if (potRoot == null) return;
            int stage = S.PlantDays == 0 ? -1 : PlantStage;   // (an empty pot until the first watering)
            bool wilted = PlantWilted;
            if (stage != potBuiltStage || wilted != potBuiltWilted) BuildStage(stage, wilted);
            // a little hop when watered; drooping when dry
            float q = (Time.time - plantBounceAt) / 0.6f;
            float bounce = q < 1f ? 1f + 0.15f * Mathf.Sin(q * Mathf.PI * 2f) * (1f - q) : 1f;
            potPlant.localScale = new Vector3(1f, bounce, 1f);
            potPlant.localRotation = wilted ? Quaternion.Euler(28f, 0f, 12f) : Quaternion.identity;
        }

        void WaterPlant()
        {
            if (PlantWateredToday) { Play(new[] { new Line(Who.Mita, "오늘은 이미 물 줬어요. 너무 많이 주면 뿌리가 숨을 못 쉬어요.", "smile") }); return; }
            int before = PlantStage;
            bool wilted = PlantWilted;
            bool first = S.PlantDays == 0;
            S.PlantWatered = Today;
            S.PlantDays++;
            plantBounceAt = Time.time;
            Did("plant");
            AddAff(1);
            moodValue = Mathf.Min(100f, moodValue + 6f);
            var lines = new List<Line>();
            if (first) lines.Add(new Line(Who.Mita, "이 화분, 오늘부터 우리 둘이 키우는 거예요. 이름도 지어 줄까요?", "smile", "@cheer"));
            else if (wilted) lines.Add(new Line(Who.Mita, "다행이다… 조금 시들었었는데, 물 마시니까 고개를 드는 것 같아요.", "smile"));
            else lines.Add(new Line(Who.Mita, Random.value < 0.5f ? "쪼르륵~ 오늘도 잘 마시네요." : "물 주는 시간, 저 이거 좋아해요. 우리 둘만의 일과 같아서.", "smile"));
            if (PlantStage > before)
            {
                ui.Toast(Loc.T("화분이 자랐어요") + " · " + Loc.T(PlantStageNames[PlantStage]), true);
                lines.Add(PlantStage switch
                {
                    1 => new Line(Who.Mita, "봐요! 떡잎이 나왔어요!", "surprise", "@cheer"),
                    2 => new Line(Who.Mita, "줄기가 쑥 자랐어요. 우리가 키운 거예요!", "smile", "@nod"),
                    3 => new Line(Who.Mita, "꽃봉오리가 생겼어요… 곧 필 것 같아요. 두근두근.", "shy"),
                    _ => new Line(Who.Mita, "꽃이 폈어요! …같이 키운 꽃이라 더 예뻐 보여요. 사진 찍을래요?", "smile", "@cheer"),
                });
                if (PlantStage == 4) AddAff(3);
            }
            S.Write();
            Play(lines);
        }

        // ---------------------------------------------------------------- her mood

        float moodValue = -1f, moodLastTick;
        float lastTogether;

        // 0..100: how she feels right now
        float Mood => moodValue < 0f ? 60f : moodValue;

        static readonly (float min, string word, string mark)[] MoodWords =
        {
            (85f, "신남", "♫"), (65f, "좋음", "♪"), (45f, "보통", "~"), (25f, "심심함", "…"), (0f, "시무룩", "…"),
        };

        string MoodText()
        {
            var m = MoodWords.First(w => Mood >= w.min);
            return m.mark + " " + Loc.T("기분") + " " + Loc.T(m.word);
        }

        float MoodBase => Cold ? 35f : 50f + Mathf.Clamp(Tier, 0, 9) * 3f;

        void TickMood()
        {
            if (moodValue < 0f) { moodValue = S.Mood > 0f ? S.Mood : MoodBase; lastTogether = Time.time; }
            float dt = Time.time - moodLastTick;
            if (dt < 1f) return;
            moodLastTick = Time.time;
            dt = Mathf.Min(dt, 5f);
            // towards how she is in general; down when nothing has happened for a while (not while asleep)
            moodValue = Mathf.MoveTowards(moodValue, MoodBase, dt * 0.02f);
            bool asleep = activity == MitaActivity.Sleeping;
            if (!asleep && freeMode && Time.time - lastTogether > 420f) moodValue = Mathf.Max(18f, moodValue - dt * 0.035f);
            if (asleep) moodValue = Mathf.MoveTowards(moodValue, Mathf.Max(MoodBase, 70f), dt * 0.05f);
            S.Mood = moodValue;
        }

        // something done together (from Did)
        void MoodUp(string what)
        {
            if (moodValue < 0f) moodValue = MoodBase;
            lastTogether = Time.time;
            float up = what switch { "meal" => 10f, "gift" => 14f, "present" => 8f, "heart" => 10f, "lap" => 9f, "photo" => 7f, "" => 0f, _ => 5f };
            moodValue = Mathf.Min(100f, moodValue + up);
        }

        // bored: the next idea of hers comes sooner
        float MoodIdleFactor => Mood < 40f ? 0.55f : Mood > 85f ? 0.85f : 1f;
        bool Bored => Mood < 35f;

        public void DevMood(float v) { moodValue = Mathf.Clamp(v, 0f, 100f); }
        public void DevPlant(int days) { S.PlantDays = days; S.PlantWatered = days < 0 ? DateTime.Today.AddDays(-4).ToString("yyyy-MM-dd") : ""; if (days < 0) S.PlantDays = 4; }
    }
}
