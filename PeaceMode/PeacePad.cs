using System;
using HarmonyLib;
using UnityEngine;

namespace MiSidePeaceMode
{
    // A gamepad (2026-10): Unity's joystick input (Xbox layout; a DualSense through Steam Input). MiSide itself only knows the keyboard
    // and mouse for its own menus; walking and looking with a pad the game does itself (its joystick axes). Here the pad
    // drives our menus: A = choose / talk to her, B = back,
    // D-pad or stick = move, LB/RB = album pages, Start = her menu.
    public static class PeacePad
    {
        // (no native XInput call: an antivirus (McAfee) took a build that P/Invoked xinput1_4.dll for malware and
        // deleted it - Unity's own joystick input instead: buttons as JoystickButton0.., sticks as the game's input axes)
        static readonly string[] AxisLX = { "Horizontal" }, AxisLY = { "Vertical" };
        static string axisRX, axisRY, axisDX, axisDY;
        static bool axesProbed;

        public const ushort Up = 0x1, Down = 0x2, Left = 0x4, Right = 0x8, Start = 0x10, Select = 0x20, LStick = 0x40, RStick = 0x80,
                            LB = 0x100, RB = 0x200, A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;

        static ushort now, before;
        static int frame = -1;
        public static bool Connected;
        public static Vector2 LeftStick, RightStick;
        public static float LastUsed = -100f;   // (the prompts show the pad's buttons while it is the one in use)
        public static bool InUse => Connected && Time.unscaledTime - LastUsed < 20f;
        static float nextNames;

        // dev: a pad pressed for a few frames (tests without one)
        static ushort fakeButtons; static Vector2 fakeL, fakeR; static int fakeUntil = -1;
        public static void Fake(ushort buttons, Vector2 l, Vector2 r, int frames) { if (!axesProbed) ProbeAxes(); fakeButtons = buttons; fakeL = l; fakeR = r; fakeUntil = Time.frameCount + frames; }

        static float Axis(string name)
        {
            if (name == null) return 0f;
            try { return Input.GetAxisRaw(name); } catch { return 0f; }
        }

        static bool Has(string name) { try { Input.GetAxisRaw(name); return true; } catch { return false; } }

        // which of the usual names the game's input settings define for the right stick and the D-pad
        public static void ProbeAxes()
        {
            axesProbed = true;
            foreach (var (x, y) in new[] { ("Mouse X Joystick", "Mouse Y Joystick"), ("Joystick X", "Joystick Y"), ("RightStickX", "RightStickY"), ("Right Stick X", "Right Stick Y"),
                                           ("RightHorizontal", "RightVertical"), ("Look X", "Look Y"), ("Axis 4", "Axis 5"), ("4th axis", "5th axis"), ("Joy X", "Joy Y") })
                if (Has(x) && Has(y)) { axisRX = x; axisRY = y; break; }
            foreach (var (x, y) in new[] { ("DPadX", "DPadY"), ("DPad X", "DPad Y"), ("Dpad Horizontal", "Dpad Vertical"), ("6th axis", "7th axis"), ("Axis 6", "Axis 7") })
                if (Has(x) && Has(y)) { axisDX = x; axisDY = y; break; }
            Plugin.Log.LogInfo($"[pad] axes: right stick {axisRX ?? "-"}/{axisRY ?? "-"}, d-pad {axisDX ?? "-"}/{axisDY ?? "-"}");
        }

        static readonly KeyCode[] ButtonKeys =
        {
            KeyCode.JoystickButton0, KeyCode.JoystickButton1, KeyCode.JoystickButton2, KeyCode.JoystickButton3, KeyCode.JoystickButton4,
            KeyCode.JoystickButton5, KeyCode.JoystickButton6, KeyCode.JoystickButton7, KeyCode.JoystickButton8, KeyCode.JoystickButton9,
        };
        // JoystickButtonN (Xbox layout on Windows) -> our bits
        static readonly ushort[] ButtonBits = { A, B, X, Y, LB, RB, Select, Start, LStick, RStick };

        public static void Poll()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            before = now;
            now = 0; LeftStick = RightStick = Vector2.zero;
            if (Time.frameCount <= fakeUntil)
            {
                now = fakeButtons; LeftStick = fakeL; RightStick = fakeR; Connected = true; LastUsed = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime >= nextNames)
            {
                nextNames = Time.unscaledTime + 2f;
                bool any = false;
                try { foreach (var n in Input.GetJoystickNames()) if (!string.IsNullOrEmpty(n)) any = true; } catch { }
                Connected = any;
                if (any && !axesProbed) ProbeAxes();
            }
            if (!Connected) return;
            for (int i = 0; i < ButtonKeys.Length; i++) if (Input.GetKey(ButtonKeys[i])) now |= ButtonBits[i];
            float dx = Axis(axisDX), dy = Axis(axisDY);
            if (dy > 0.5f) now |= Up; else if (dy < -0.5f) now |= Down;
            if (dx < -0.5f) now |= Left; else if (dx > 0.5f) now |= Right;
            // (the left stick shares Horizontal/Vertical with the keys: only counted while no key is down)
            bool keys = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)
                        || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
            if (!keys) LeftStick = Stick(Axis(AxisLX[0]), Axis(AxisLY[0]));
            RightStick = Stick(Axis(axisRX), -Axis(axisRY));
            if (now != 0 || LeftStick != Vector2.zero || RightStick != Vector2.zero) LastUsed = Time.unscaledTime;
        }

        static Vector2 Stick(float x, float y)
        {
            var v = new Vector2(x, y);
            float m = v.magnitude;
            if (m < 0.24f) return Vector2.zero;
            return v / m * Mathf.Clamp01((m - 0.24f) / 0.76f);   // (rescaled past the dead zone)
        }

        public static bool Pressed(ushort b) { Poll(); return (now & b) != 0 && (before & b) == 0; }
        public static bool Held(ushort b) { Poll(); return (now & b) != 0; }

        // up/down for lists: a press, then repeats while held (D-pad or the left stick)
        static int heldDir; static float repeatAt;
        public static int MenuStep()
        {
            Poll();
            int d = Held(Up) || LeftStick.y > 0.55f ? -1 : Held(Down) || LeftStick.y < -0.55f ? 1 : 0;
            if (d == 0) { heldDir = 0; return 0; }
            float t = Time.unscaledTime;
            if (d != heldDir) { heldDir = d; repeatAt = t + 0.38f; return d; }
            if (t >= repeatAt) { repeatAt = t + 0.11f; return d; }
            return 0;
        }

        // ---- walking: the stick as the keys PlayerMove reads (-1, 0, 1 each way) ----
        public static bool Walking;

        [HarmonyPatch(typeof(PlayerMove), nameof(PlayerMove.FixedUpdate)), HarmonyPrefix]
        static void BeforeMove(PlayerMove __instance)
        {
            if (!Walking || __instance == null || __instance.dontMove) return;
            var l = LeftStick;
            if (l.sqrMagnitude < 0.01f) return;
            if (__instance.moveH == 0) __instance.moveH = Mathf.Abs(l.x) > 0.35f ? (l.x > 0 ? 1 : -1) : 0;
            if (__instance.moveV == 0) __instance.moveV = Mathf.Abs(l.y) > 0.35f ? (l.y > 0 ? 1 : -1) : 0;
        }
    }

    public partial class PeaceDirector
    {
        // free roaming: walk and look with the pad, A at her = her menu, Start = her menu wherever you are close enough
        void TickPad(float dt, bool near)
        {
            PeacePad.Poll();
            // (walking and looking are the game's own: it reads the pad through its "Horizontal"/"Vertical" and
            // "Mouse X Joystick" axes - ours on top would double them. PeacePad.Walking stays off; kept for a game
            // build without those axes)
            PeacePad.Walking = false;
            if (!PeacePad.Connected || pm == null) return;
            if (MenuOpen || !freeMode) return;
            if (PeacePad.Pressed(PeacePad.A) && near) OpenMainMenu();
            else if (PeacePad.Pressed(PeacePad.Start) && PlayerNearMita(4f, 360f)) OpenMainMenu();
        }

        // the menus, the album, the records, typing (from TickMenus)
        bool TickPadMenus()
        {
            if (!PeacePad.Connected) return false;
            if (typing != Typing.None)
            {
                if (PeacePad.Pressed(PeacePad.B)) { CancelTyping(); return true; }
                return false;
            }
            if (ui.CardOpen || cardKind != CardKind.None)
            {
                if (PeacePad.Pressed(PeacePad.B) || PeacePad.Pressed(PeacePad.A) && cardKind == CardKind.Records) { CloseCardBack(); return true; }
                if (cardKind == CardKind.Diary)
                {
                    if (PeacePad.Pressed(PeacePad.RB) || PeacePad.Pressed(PeacePad.Right) || PeacePad.Pressed(PeacePad.A)) { TurnDiary(+1); return true; }
                    if (PeacePad.Pressed(PeacePad.LB) || PeacePad.Pressed(PeacePad.Left)) { TurnDiary(-1); return true; }
                }
                if (cardKind == CardKind.Album)
                {
                    if (PeacePad.Pressed(PeacePad.RB) || PeacePad.Pressed(PeacePad.Right) || PeacePad.Pressed(PeacePad.A)) { TurnAlbum(+1); return true; }
                    if (PeacePad.Pressed(PeacePad.LB) || PeacePad.Pressed(PeacePad.Left)) { TurnAlbum(-1); return true; }
                }
                return false;
            }
            if (!ui.PanelOpen) return false;
            int step = PeacePad.MenuStep();
            if (step != 0) { ui.PanelMove(step); return true; }
            if (PeacePad.Pressed(PeacePad.A)) { ui.PanelPickSelected(); return true; }
            if (PeacePad.Pressed(PeacePad.B)) { ui.PanelBack(); return true; }
            return false;
        }
    }
}
