using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WanderingCaravans
{
    public class IncidentWorker_SpawnWanderingCaravan : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return (parms.target is Map map && !map.wildAnimalSpawner.AnimalEcosystemFull) || CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(parms.target.Tile);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (parms.target is Map map)
            {
                return Execute(parms, map);
            }
            else if (parms.target is Caravan caravan)
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    Execute(caravan);
                }, "GeneratingMapForNewEncounter", false, null);
                return true;
            }
            return true;
        }

        protected bool Execute(IncidentParms parms, Map map)
        {
            IntVec3 spawnSpot = IntVec3.Invalid;
            if (map != null && !CellFinder.TryFindRandomEdgeCellWith(vec => vec.Standable(map) && map.reachability.CanReachColony(vec), map, CellFinder.EdgeRoadChance_Ignore, out spawnSpot)
                || !WanderingCaravansUtility.AllWanderingCaravansSpawnableInMap(map).TryRandomElementByWeight(animal => map.Biome.CommonalityOfAnimal(animal) / animal.wildGroupSize.Average
                , out PawnKindDef wanderingCaravanDef))
            {
                return false;
            }
            IEnumerable<Pawn> wanderingCaravans = this.GenerateWanderingCaravans(wanderingCaravanDef);
            Pawn p = null;

            List<Pawn> list = new List<Pawn>();
            foreach (Pawn pawn in wanderingCaravans)
            {
                p = pawn;
                list.Add(p);
                GenSpawn.Spawn(pawn, CellFinder.RandomSpawnCellForPawnNear(spawnSpot, map), map, Rot4.Random);
            }
            //    this.SpawnPawns(wanderingCaravans, map, spawnSpot);

            base.SendStandardLetter(parms, list, wanderingCaravans.First().def.LabelCap);
            return true;
        }

        protected bool Execute(Caravan caravan)
        {
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(caravan.Tile, DefDatabase<WorldObjectDef>.GetNamed("WanderingCaravanEncounter"));
            if (!WanderingCaravansUtility.AllWanderingCaravansSpawnableInMap(map).TryRandomElement(out PawnKindDef wanderingCaravanDef))
            {
                return false;
            }
            MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out IntVec3 caravanSpot, out IntVec3 wanderingCaravansSpot);
            IEnumerable<Pawn> wanderingCaravans = this.GenerateWanderingCaravans(wanderingCaravanDef);
            Pawn infoPawn = wanderingCaravans.First();
            DiaNode diaNode = new DiaNode($"A wandering {infoPawn.LabelCap} has been spotted in the distance by {caravan.LabelCap}, with more wandering caravans possible following it.\n\nYou can "
                + $"ignore them, or you can tame or kill them to obtain their potentially valuable inventory.")
            {
                options =
                {
                    new DiaOption("Go and claim their inventory")
                    {
                        action = delegate
                        {
                            string plural = wanderingCaravans.Count() > 1 ? "ies" : "y";
                            CaravanEnterMapUtility.Enter(caravan, map, pawn => CellFinder.RandomSpawnCellForPawnNear(caravanSpot, map));
                            Messages.Message($"You have {TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(60000)} to claim the wandering {infoPawn.LabelCap}s inventor{plural} before the "
                                + $"caravan is reformed.", infoPawn, MessageTypeDefOf.PositiveEvent);
                            ((WorldObject)map.ParentHolder).GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown();
                        },
                        resolveTree = true
                    }, new DiaOption("Ignore them and continue")
                    {
                        resolveTree = true
                    }
                }
            };
            Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, false, $"Wandering {infoPawn.LabelCap} spotted"));
            this.SpawnPawns(wanderingCaravans, map, wanderingCaravansSpot);
            return true;
        }

        private IEnumerable<Pawn> GenerateWanderingCaravans(PawnKindDef def)
        {
            int wanderingCaravansCount = def.RaceProps.wildness != 0 ? def.wildGroupSize.RandomInRange : Rand.Range(3, 9);
            for (int i = 0; i < wanderingCaravansCount; i++)
            {
                yield return WanderingCaravansUtility.GenerateWanderingCaravan(def);
            }
        }

        private void SpawnPawns(IEnumerable<Pawn> pawns, Map map, IntVec3 spawnSpot)
        {
            foreach (Pawn pawn in pawns)
            {
                GenSpawn.Spawn(pawn, CellFinder.RandomSpawnCellForPawnNear(spawnSpot, map), map, Rot4.Random);
            }
        }
    }
}
