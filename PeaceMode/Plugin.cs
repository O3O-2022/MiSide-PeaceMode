using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace MiSidePeaceMode
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "kr.miside.peacemode";
        public const string Name = "MiSide Peace Mode";
        public const string Version = "1.1.0";

        internal static new ManualLogSource Log;
        internal static Harmony Harmony;

        public override void Load()
        {
            Log = base.Log;
            ClassInjector.RegisterTypeInIl2Cpp<PeaceDirector>();
#if DEVTOOLS
            ClassInjector.RegisterTypeInIl2Cpp<DevRunner>();
#if DEVREC
            ClassInjector.RegisterTypeInIl2Cpp<AudioTap>();
#endif
#endif
            PeaceMode.BindConfig(Config);
#if DEVTOOLS
            AddComponent<DevRunner>();
#endif
            Harmony = new Harmony(Guid);
            Harmony.PatchAll(typeof(SaveGuard));
            Harmony.PatchAll(typeof(TextHook));
            Harmony.PatchAll(typeof(MenuPatch));
            Harmony.PatchAll(typeof(DirectorHook));
            Harmony.PatchAll(typeof(MitaClipEventHook));
            Harmony.PatchAll(typeof(CallShield));
            Harmony.PatchAll(typeof(SceneImport));
            Harmony.PatchAll(typeof(SpeechFontPatch));
            Harmony.PatchAll(typeof(PeacePad));
#if DEVTOOLS
            if (System.IO.File.Exists(System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "peacemode_dev.txt")))
            {
                TraceHooks.Apply(Harmony);
                DevWalk.Apply(Harmony);
            }
#endif
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
