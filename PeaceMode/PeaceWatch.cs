using System;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace MiSidePeaceMode
{
    // Dev: a running log of where she is and what holds her there (found the "she stands in odd places" reports),
    // and of where Cappie's toy hammer is during the hammer game.
    public partial class PeaceDirector
    {
        public static float DevWatchEvery;   // dev `watch <seconds>|off`
        float nextWatch;

        void TickWatch()
        {
            if (DevWatchEvery <= 0f || !ready || Time.realtimeSinceStartup < nextWatch) return;
            nextWatch = Time.realtimeSinceStartup + DevWatchEvery;
            try { Plugin.Log.LogInfo("[watch] " + WatchLine()); } catch (Exception e) { Plugin.Log.LogWarning("[watch] " + e.Message); }
        }

        string WatchLine()
        {
            var sb = new StringBuilder();
            if (mitaBody == null) return "no body";
            var p = mitaBody.position;
            sb.Append($"t={Time.time:0} act={activity} posed={posed} lying={lying} busy={busy} free={freeMode} hold={addonHeld} root=({p.x:0.00},{p.y:0.00},{p.z:0.00}) yaw={mitaBody.eulerAngles.y:0}");
            var hips = Util.FindDeep(mitaBody, "Hips");
            if (hips != null) { var h = hips.position; var off = h - p; off.y = 0; sb.Append($" hips=({h.x:0.00},{h.y:0.00},{h.z:0.00}) hipsOff={off.magnitude:0.00}"); }
            bool nmaOn = mita != null && mita.nma != null && mita.nma.enabled;
            sb.Append($" nma={nmaOn}");
            if (nmaOn) { try { sb.Append($" vel={mita.nma.velocity.magnitude:0.00} dest=({mita.nma.destination.x:0.0},{mita.nma.destination.z:0.0}) onMesh={mita.nma.isOnNavMesh}"); } catch { } }
            if (NavMesh.SamplePosition(p, out var hit, 3f, -1)) { var d = hit.position - p; d.y = 0; sb.Append($" mesh={d.magnitude:0.00}"); } else sb.Append(" mesh=FAR");
            if (pm != null) { var d = p - pm.transform.position; d.y = 0; sb.Append($" toYou={d.magnitude:0.00}"); }
            try { sb.Append(" anim=" + DevAnimNow().Split('|')[0]); } catch { }
            if (hammerStage != 0) sb.Append(" | " + HammerToyState());
            // flags
            if (!posed && hips != null)
            {
                var off = hips.position - p; off.y = 0;
                if (off.magnitude > 0.45f) sb.Append("  <-- HIPS OFF HER ROOT");
                if (hips.position.y < p.y + 0.4f && activity != MitaActivity.Sleeping) sb.Append("  <-- HIPS LOW");
            }
            if (!posed && NavMesh.SamplePosition(p, out var h2, 3f, -1)) { var d = h2.position - p; d.y = 0; if (d.magnitude > 0.3f) sb.Append("  <-- OFF THE NAVMESH"); }
            return sb.ToString();
        }

        string HammerToyState()
        {
            if (hammerToy == null) return "toy=null";
            string par = hammerToy.parent != null ? Util.PathOf(hammerToy.parent) : "-";
            var mg = hammerToy.GetComponent<Transform_Magnet>();
            string tgt = "-";
            try { if (mg != null && mg.transformTarget != null) tgt = Util.PathOf(mg.transformTarget); else if (mg != null) tgt = "null"; } catch { tgt = "dead"; }
            var r = hammerToy.GetComponentInChildren<Renderer>(true);
            return $"toy active={hammerToy.gameObject.activeInHierarchy} self={hammerToy.gameObject.activeSelf} pos={hammerToy.position} parent={par} magnet={(mg != null ? mg.active.ToString() : "-")}->{tgt} renderer={(r != null ? (r.enabled && r.gameObject.activeInHierarchy).ToString() : "none")}";
        }
    }
}
