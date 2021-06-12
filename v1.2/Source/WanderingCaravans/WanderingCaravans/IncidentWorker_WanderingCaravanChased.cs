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
            DiaNode descDiaNode = new DiaNode($"WanderingCaravan.CaravanChased_OptionText".Translate(parms.faction.def.pawnsPlural, parms.faction.Name));
            descDiaNode.options.Add(new DiaOption("WanderingCaravan.CaravanChased_OptionDefend".Translate())
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
            DiaNode rejectDiaNode = new DiaNode("WanderingCaravan.CaravanChased_Rejected".Translate(parms.faction.def.pawnsPlural));
            rejectDiaNode.options.Add(new DiaOption("OK")
            {
                resolveTree = true
            });
            descDiaNode.options.Add(new DiaOption($"WanderingCaravan.CaravanChased_OptionReject".Translate(parms.faction.def.pawnsPlural))
            {
                link = rejectDiaNode
            });
            Find.WindowStack.Add(new Dialog_NodeTree(descDiaNode, true, true, "WanderingCaravan.CaravanChased_OptionTitle".Translate(map.info.parent.Label)));
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
