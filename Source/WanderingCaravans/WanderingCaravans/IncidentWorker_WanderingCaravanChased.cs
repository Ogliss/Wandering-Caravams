using System.Linq;
using RimWorld;
using Verse;

namespace WanderingCaravans
{
    public class IncidentWorker_WanderingCaravanChased : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!this.TryResolveParms(parms, out PawnKindDef chasedCaravanDef))
            {
                return false;
            }
            DiaNode descDiaNode = new DiaNode($"A group of {parms.faction.def.pawnsPlural} from {parms.faction.Name} can be seen in the distance, and are chasing down a "
                + $"trade caravan carrying a potentially valuable inventory.\n\nYou can either defend the caravan from the {parms.faction.def.pawnsPlural} and obtain its "
                + $"inventory or let them have it.\n\nBe warned - if you accept, you'll have to fight off the {parms.faction.def.pawnsPlural} on its tail.");
            descDiaNode.options.Add(new DiaOption("Defend the caravan")
            {
                action = delegate
                {
                    Pawn chasedCaravan = WanderingCaravansUtility.GenerateWanderingCaravan(chasedCaravanDef);
                    chasedCaravan.SetFaction(Faction.OfPlayer);
                    GenSpawn.Spawn(chasedCaravan, parms.spawnCenter, map);
                    CameraJumper.TryJump(chasedCaravan);
                    IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParams.forced = true;
                    incidentParams.faction = parms.faction;
                    incidentParams.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    incidentParams.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    incidentParams.spawnCenter = parms.spawnCenter;
                    incidentParams.points = parms.points;
                    Find.Storyteller.incidentQueue.Add(new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, null, incidentParams), Find.TickManager.TicksGame
                        + new IntRange(1000, 2500).RandomInRange));
                },
                resolveTree = true
            });
            DiaNode rejectDiaNode = new DiaNode("The hunters have almost caught up to the caravan and will be able to obtain its potentially valuable items.");
            rejectDiaNode.options.Add(new DiaOption("OK")
            {
                resolveTree = true
            });
            descDiaNode.options.Add(new DiaOption($"Let the {parms.faction.def.pawnsPlural} have it")
            {
                link = rejectDiaNode
            });
            Find.WindowStack.Add(new Dialog_NodeTree(descDiaNode, true, true, "Wandering caravan chased to " + map.info.parent.Label));
            return true;
        }

        protected virtual bool TryResolveParms(IncidentParms parms, out PawnKindDef chasedCaravan)
        {
            Map map = (Map)parms.target;
            chasedCaravan = null;
            return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out parms.spawnCenter)
                && Find.FactionManager.AllFactions.Where(f => !f.def.hidden && f.HostileTo(Faction.OfPlayer)).TryRandomElement(out parms.faction) 
                && WanderingCaravansUtility.AllWanderingCaravansSpawnableInMap(map).TryRandomElement(out chasedCaravan);
        }
    }
}
