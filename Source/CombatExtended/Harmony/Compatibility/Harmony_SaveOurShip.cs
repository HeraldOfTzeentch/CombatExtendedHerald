using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System;
using RimWorld;
using Verse.AI;

namespace CombatExtended.HarmonyCE.Compatibility
{
    [HarmonyPatch]
    class Harmony_Compatibility_SaveOurShip
    {
        static readonly Assembly ass = AppDomain.CurrentDomain.GetAssemblies().
                                       SingleOrDefault(assembly => assembly.GetName().Name == "ShipsHaveInsides");
        static MethodBase targetMethod = null;

        static readonly List<String> shellDefNames = new List<String> { "ShipTorpedo_HighExplosive", "ShipTorpedo_EMP", "ShipTorpedo_Antimatter" };


        internal static bool Prepare()
        {
            if (ass == null)
            {
                return false;
            }
            foreach (var t in ass.GetTypes())
            {
                if (t.Name == "WorkGiver_LoadTorpedoTube")
                {
                    targetMethod = AccessTools.Method(t, "FindAmmo");
                }
            }
            if (targetMethod == null)
            {
                Log.Error($"Failed to find target method while attempting to patch SaveOurShip.");
                return false;
            }
            return true;
        }

        internal static MethodBase TargetMethod()
        {
            return targetMethod;
        }

        public static void Postfix(Pawn pawn, Building_Turret tube, ref Thing __result)
        {
            if (__result != null)
            {
                return;
            }
            var turret = tube as Building_ShipTurret;
            StorageSettings allowedShellsSettings = ThingCompUtility.TryGetComp<CompChangeableProjectilePlural>(turret.gun).allowedShellsSettings;
            Predicate<Thing> pred = (Thing x) =>
            {
                if (ForbidUtility.IsForbidden(x, pawn) || !ReservationUtility.CanReserve(pawn, x, 1, -1, (ReservationLayerDef)null, false))
                {
                    return false;
                }
                else
                {
                    return allowedShellsSettings?.AllowedToAccept(x) ?? true;
                }
            };

            foreach (String defName in shellDefNames)
            {
                var val = ThingDef.Named(defName);
                __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(val), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, pred);
                if (__result != null)
                {
                    return;
                }
            }
        }
    }
}
