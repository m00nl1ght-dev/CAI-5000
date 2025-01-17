﻿using System.Collections.Generic;
using System.Linq;
using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class LordToil_AssaultColony_Patch
    {
        private static readonly List<Pawn>[] forces = new List<Pawn>[10];
        private static readonly List<Thing>  things = new List<Thing>();
        private static readonly List<Thing>  thingsImportant = new List<Thing>();
        private static readonly List<Zone>   zones  = new List<Zone>();

        static LordToil_AssaultColony_Patch()
        {
            forces[0] = new List<Pawn>();
            forces[1] = new List<Pawn>();
            forces[2] = new List<Pawn>();
            forces[3] = new List<Pawn>();
            forces[4] = new List<Pawn>();
            forces[5] = new List<Pawn>();
            forces[6] = new List<Pawn>();
            forces[7] = new List<Pawn>();
            forces[8] = new List<Pawn>();
            forces[9] = new List<Pawn>();
        }

        public static void ClearCache()
        {
            zones.Clear();
            things.Clear();
            thingsImportant.Clear();
            forces[0].Clear();
            forces[1].Clear();
            forces[2].Clear();
            forces[3].Clear();
            forces[4].Clear();
            forces[5].Clear();
            forces[6].Clear();
            forces[7].Clear();
            forces[8].Clear();
            forces[9].Clear();
        }

        private static float GetZoneTotalMarketValue(Zone zone)
        {
            if (!TKVCache<int, Zone_Stockpile, float>.TryGet(zone.ID, out float val, 6000))
            {
                val = zone.AllContainedThings.Sum(t => t.GetStatValue_Fast(StatDefOf.MarketValue, 1200));
                TKVCache<int, Zone_Stockpile, float>.Put(zone.ID, val);
            }
            return val;
        }

        [HarmonyPatch(typeof(LordToil_AssaultColony), nameof(LordToil_AssaultColony.UpdateAllDuties))]
        private static class LordToil_AssaultColony_UpdateAllDuties_Patch
        {
            public static void Postfix(LordToil_AssaultColony __instance)
            {
                if (Finder.Settings.Enable_Groups && __instance.lord.ownedPawns.Count > 10)
                {
                    ClearCache();
                    Map map = __instance.Map;
                    things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.Bed).Where(b => b is Building_Bed bed && bed.CompAssignableToPawn.AssignedPawns.Any(p => p.Faction == map.ParentFaction)));
                    things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.ResearchBench).Where(t => t.Faction == map.ParentFaction));
                    things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodDispenser).Where(t => t.Faction == map.ParentFaction));
                    things.AddRange(map.mapPawns.PrisonersOfColonySpawned);
                    // add custom and modded raid targets.
                    foreach (ThingDef def in RaidTargetDatabase.allDefs)
                    {
                        if (map.listerThings.listsByDef.TryGetValue(def, out List<Thing> things) && things != null)
                        {
                            if (Finder.Settings.Debug)
                            {
                                Log.Message($"ISMA: Added things of def {def} to the current raid pool.");
                            }
                            thingsImportant.AddRange(things);
                        }
                    }
                    if (ModsConfig.BiotechActive)
                    {
                        things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.MechCharger).Where(t => t.Faction == map.ParentFaction));
                        things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.GenepackHolder));
                    }
                    if (ModsConfig.IdeologyActive)
                    {
                        things.AddRange(map.mapPawns.SlavesOfColonySpawned);
                    }
                    if (ModsConfig.RoyaltyActive)
                    {
                        things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.Throne));
                    }
                    zones.AddRange(__instance.Map.zoneManager.AllZones.Where(z => z is Zone_Stockpile || z is Zone_Growing));
                    int taskForceNum = Maths.Min(__instance.lord.ownedPawns.Count / 5, 10);
                    int m            = Rand.Range(1, 7);
                    int c            = 0;
                    for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
                    {
                        int k = Rand.Range(0, taskForceNum + m);
                        if (k < m)
                        {
                            c++;
                            continue;
                        }
                        forces[k - m].Add(__instance.lord.ownedPawns[i]);
                    }
                    if (Finder.Settings.Debug)
                    {
                        Log.Message($"{__instance.lord.ownedPawns.Count - c} pawns are assigned to attack specific targets and {c} are assigned to assault duties. {__instance.lord.ownedPawns.Count - c}/{__instance.lord.ownedPawns.Count} ");
                    }
                    for (int i = 0; i < taskForceNum; i++)
                    {
                        List<Pawn> force = forces[i];
                        if (zones.Count != 0 && (Rand.Chance(0.333f) || things.Count == 0))
                        {
                            Zone zone = zones.RandomElementByWeight(s => GetZoneTotalMarketValue(s) / 100f + (s.Position.Roofed(__instance.Map) ? 2 : 0f));
                            for (int j = 0; j < force.Count; j++)
                            {
                                ThingComp_CombatAI comp = force[j].AI();
                                if (comp == null &&  force[j] is Pawn p)
                                {
                                    Log.Error($"IMSA: {p} has no ThingComp_CombatAI");
                                }
                                if (comp != null && !comp.duties.Any(CombatAI_DutyDefOf.CombatAI_AssaultPoint))
                                {
                                    Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.AssaultPoint(zone.Position, Rand.Range(7, 15), 3600 * Rand.Range(3, 8));
                                    if (force[j].TryStartCustomDuty(customDuty))
                                    {
                                        if (Finder.Settings.Debug)
                                        {
                                            Log.Message($"{comp.parent} task force {i} attacking {zone}");
                                        }
                                        if (Rand.Chance(0.33f))
                                        {
                                            Pawn_CustomDutyTracker.CustomPawnDuty customDuty2 = CustomDutyUtility.DefendPoint(zone.Position, Rand.Range(30, 60), true, 3600 + Rand.Range(0, 60000));
                                            force[j].EnqueueFirstCustomDuty(customDuty);
                                            if (Finder.Settings.Debug)
                                            {
                                                Log.Message($"{comp.parent} task force {i} occupying area around {zone}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (things.Count != 0)
                        {
                            List<Thing> collection;
                            if (thingsImportant.NullOrEmpty())
                            {
                                collection = things;
                            }
                            else
                            {
                                collection = Rand.Chance(0.5f) ? thingsImportant : things;
                            }
                            Thing       thing = collection.RandomElementByWeight(t => t.GetStatValue_Fast(StatDefOf.MarketValue, 1200) * (t is Pawn ? 10 : 1));
                            if (thing != null)
                            {
                                for (int j = 0; j < force.Count; j++)
                                {
                                    ThingComp_CombatAI comp = force[j].AI();
                                    if (comp != null && !comp.duties.Any(CombatAI_DutyDefOf.CombatAI_AssaultPoint))
                                    {
                                        Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.AssaultPoint(thing.Position, Rand.Range(7, 15), 3600 * Rand.Range(3, 8));
                                        if (force[j].TryStartCustomDuty(customDuty))
                                        {
                                            if (Finder.Settings.Debug)
                                            {
                                                Log.Message($"{comp.parent} task force {i} attacking {thing}");
                                            }
                                            if (Rand.Chance(0.33f))
                                            {
                                                Pawn_CustomDutyTracker.CustomPawnDuty customDuty2 = CustomDutyUtility.DefendPoint(thing.Position, Rand.Range(30, 60), true, 3600 + Rand.Range(0, 60000));
                                                force[j].EnqueueFirstCustomDuty(customDuty);
                                                if (Finder.Settings.Debug)
                                                {
                                                    Log.Message($"{comp.parent} task force {i} occupying area around {thing}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ClearCache();
                }
            }
        }
    }
}
