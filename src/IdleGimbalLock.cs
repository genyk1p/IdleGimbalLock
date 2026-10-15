using System;
using HarmonyLib;
using UnityEngine;

namespace IdleGimbalLock
{
    /// <summary>
    /// Stock ModuleGimbal turns the nozzles after the pitch/yaw/roll input every physics step, whether
    /// or not its engine gives thrust. A nozzle without thrust gives no torque, so this is useless — and
    /// with heavy engines under physics warp it is harmful: the response speed is multiplied by the
    /// physics step (gimbalResponseSpeed * TimeWarp.fixedDeltaTime), so at 4x the nozzles follow every
    /// twitch of the attitude controller at once and the vessel shakes, up to resonance (seen with the
    /// KSP Interstellar Extended "Lightbulb" while MechJeb turned the vessel).
    ///
    /// A prefix on ModuleGimbal.FixedUpdate sets the gimbal's own lock for that one step when no engine on
    /// the same part is burning, and a finalizer sets it back right after. With the lock ModuleGimbal
    /// returns the nozzles to neutral and skips the input. The lock is never left set between steps, so
    /// a save cannot catch it, and a lock the player set is not touched. Parts without an engine module
    /// are left alone. Works the same for MechJeb, any other autopilot and manual flying.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Loader : MonoBehaviour
    {
        private const string HarmonyId = "IdleGimbalLock";

        /// <summary>An engine counts as burning above this throttle...</summary>
        private const float ThrottleThreshold = 0.01f;

        /// <summary>...or above this thrust, kN.</summary>
        private const float ThrustThreshold = 0.1f;

        private void Start()
        {
            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.Patch(AccessTools.Method(typeof(ModuleGimbal), nameof(ModuleGimbal.FixedUpdate)),
                    prefix: new HarmonyMethod(typeof(Loader), nameof(Prefix)),
                    finalizer: new HarmonyMethod(typeof(Loader), nameof(Finalizer)));
                Log.Info("patched ModuleGimbal.FixedUpdate: gimbals stay neutral while their engine gives no thrust");
            }
            catch (Exception e)
            {
                Log.Error($"patching failed, gimbals left as they are: {e}");
            }
        }

        private static void Prefix(ModuleGimbal __instance, ref bool __state)
        {
            __state = false;
            if (__instance.gimbalLock || !HighLogic.LoadedSceneIsFlight)
                return;
            if (IsIdle(__instance.part))
            {
                __instance.gimbalLock = true;
                __state = true;
            }
        }

        private static Exception Finalizer(ModuleGimbal __instance, bool __state, Exception __exception)
        {
            if (__state)
                __instance.gimbalLock = false;
            return __exception;
        }

        /// <summary>The part has an engine module and none of them is burning.</summary>
        private static bool IsIdle(Part part)
        {
            if (part == null)
                return false;
            bool hasEngine = false;
            PartModuleList modules = part.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (!(modules[i] is ModuleEngines engine))
                    continue;
                hasEngine = true;
                if (engine.EngineIgnited && (engine.currentThrottle > ThrottleThreshold || engine.finalThrust > ThrustThreshold))
                    return false;
            }
            return hasEngine;
        }
    }
}
