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
            List<Pawn> pawns = new List<Pawn>( WanderingCaravansUtility.SpawnedWanderingCaravansInMap(map));
            Pawn targetCaravan = pawns.RandomElement();
            if (!this.TryResolveParms(parms, targetCaravan))
            {
                return false;
            }
            IEnumerable<Pawn> retrieverPawns = this.SpawnPawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms, true), parms.spawnCenter, map);
            LordMaker.MakeNewLord(parms.faction, new LordJob_RetrieveWanderingCaravan(targetCaravan), map, retrieverPawns);
            Find.LetterStack.ReceiveLetter("WanderingCaravan.CaravanReturnDemand_OptionTitle".Translate(), $"WanderingCaravan.CaravanReturnDemand_OptionText".Translate(parms.faction.def.pawnsPlural.CapitalizeFirst(),parms.faction.Name, parms.faction.def.pawnsPlural), LetterDefOf.NeutralEvent, pawns);
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