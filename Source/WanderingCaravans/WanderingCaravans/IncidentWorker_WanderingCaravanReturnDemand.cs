using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI.Group;
using Verse.AI;
using Verse;

namespace WanderingCaravans
{
    public class IncidentWorker_WanderingCaravanReturnDemand : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Pawn targetCaravan = WanderingCaravansUtility.SpawnedWanderingCaravansInMap(map).RandomElement();
            if (!this.TryResolveParms(parms, targetCaravan))
            {
                return false;
            }
            IEnumerable<Pawn> retrieverPawns = this.SpawnPawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms, true), parms.spawnCenter, map);
            LordMaker.MakeNewLord(parms.faction, new LordJob_RetrieveWanderingCaravan(targetCaravan), map, retrieverPawns);
            Find.LetterStack.ReceiveLetter("Wandering Caravan Retrieval", $"{parms.faction.def.pawnsPlural.CapitalizeFirst()} from {parms.faction.Name} have claimed ownership of a neaby wandering trade caravan and have come to retrieve it.\n\n"
                + $"You can let the {parms.faction.def.pawnsPlural} retrieve the caravan, or you can kill, tame or fight the {parms.faction.def.pawnsPlural} to make sure the wandering caravan stays near "
                + $"your colony so you can obtain its potentially valuable inventory!\n\nBe warned - if you kill or tame the wandering caravan, {parms.faction.Name} will be angered.", LetterDefOf.NeutralEvent, targetCaravan);
            return true;
        }

        protected override bool CanFireNowSub(IncidentParms parms) => WanderingCaravansUtility.SpawnedWanderingCaravansInMap((Map)parms.target).Any();

        protected virtual IEnumerable<Pawn> SpawnPawns(PawnGroupMakerParms pawnGroupMakerParms, IntVec3 spawnCenter, Map map)
        {
            foreach (Pawn retrieverPawn in PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms))
            {
                GenSpawn.Spawn(retrieverPawn, CellFinder.RandomClosewalkCellNear(spawnCenter, map, 5), map);
                yield return retrieverPawn;
            }
        }

        protected virtual bool TryResolveParms(IncidentParms parms, Pawn targetCaravan)
        {
            Map map = (Map)parms.target;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, true, c => map.reachability.CanReach
                (c, targetCaravan.Position, PathEndMode.Touch, TraverseMode.NoPassClosedDoors)) || !Find.FactionManager.AllFactions.Where
                (f => !f.def.hidden && f != Faction.OfPlayer && !f.HostileTo(Faction.OfPlayer)).TryRandomElement(out parms.faction))
            {
                return false;
            }
            return true;
        }
    }
}