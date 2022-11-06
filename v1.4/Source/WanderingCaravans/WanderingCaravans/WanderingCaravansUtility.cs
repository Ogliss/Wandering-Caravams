using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WanderingCaravans
{
    public static class WanderingCaravansUtility
    {
        public static IEnumerable<Thing> GenerateWanderingCaravanInventory(float? fixedCaravanValue = null)
        {
            if (fixedCaravanValue < 1f)
            {
                Log.Error("Tried to generate a wandering caravan inventory with a fixed caravan value smaller than 1");
                yield break;
            }
            if (fixedCaravanValue == null)
            {
                float randValue = Rand.Value;
                float caravanValue = 50f;
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.75f, 50f);  // 100
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.5f, 50f);   // 150
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.4f, 50f);   // 200
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.25f, 25f);  // 225
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.1f, 25f);   // 250
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.05f, 25f);  // 275
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.075f, 25f); // 300
                ModifyValueBasedOnChance(ref caravanValue, randValue, 0.025f, 25f); // 325
                caravanValue += caravanValue * Rand.Range(-0.2f, 0.2f);
                fixedCaravanValue = caravanValue;
            }
            ThingSetMakerParams thingSetMakerParams = default(ThingSetMakerParams);
            thingSetMakerParams.traderDef = DefDatabase<TraderKindDef>.AllDefs.Where(trader => !trader.orbital).RandomElementByWeight(trader => trader.commonality);
            List<Thing> traderInventory = ThingSetMakerDefOf.TraderStock.root.Generate(thingSetMakerParams);
            traderInventory.RemoveAll(t => t is Pawn || t.MarketValue < 1 || t is MinifiedThing || ((CompProperties_Rottable)t.TryGetComp<CompRottable>()?.props)?.daysToRotStart < 25);
            List<Thing> inventory = new List<Thing>();
            while (inventory.CollectionMarketValue() < fixedCaravanValue && traderInventory.Count > 0)
            {
                Thing selectedItem = traderInventory.RandomElementByWeight(thing => Math223.Inverse(thing.MarketValue));
                if (inventory.Select(item => item.def).Contains(selectedItem.def))
                {
                    ++inventory.First(item => item.def == selectedItem.def).stackCount;
                }
                else
                {
                    inventory.Add(ThingMaker.MakeThing(selectedItem.def, selectedItem.Stuff));
                }
                if (--selectedItem.stackCount == 0)
                {
                    traderInventory.Remove(selectedItem);
                }
            }
            foreach (Thing item in inventory)
            {
                CompRottable compRottable = item.TryGetComp<CompRottable>();
                if (compRottable != null)
                {
                    compRottable.RotProgress = ((CompProperties_Rottable)compRottable.props).TicksToRotStart * Rand.Range(0.5f, 0.9f);
                }
                CompQuality compQuality = item.TryGetComp<CompQuality>();
                if (compQuality != null)
                {
                    compQuality.SetQuality(QualityUtility.GenerateQualityTraderItem(), ArtGenerationContext.Outsider);
                }
                yield return item;
            }
        }

        public static IEnumerable<Pawn> SpawnedWanderingCaravansInMap(Map map)
        {
            return map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.IsWanderingCaravan());
        }

        public static IEnumerable<PawnKindDef> AllWanderingCaravans()
        {
            IEnumerable<PawnKindDef> wanderingCaravans = DefDatabase<PawnKindDef>.AllDefs.Where(def => def.IsWanderingCaravanDef());
            if (!wanderingCaravans.Any())
            {
                Log.Error("No wandering caravans found");
            }
            return wanderingCaravans;
        }

        public static IEnumerable<PawnKindDef> AllWanderingCaravansSpawnableInMap(Map map)
        {
            return AllWanderingCaravans().Where(wc => map.mapTemperature.SeasonAcceptableFor(wc.race));
        }

        public static Pawn GenerateWanderingCaravan(PawnKindDef def)
        {
            Pawn wanderingCaravan = PawnGenerator.GeneratePawn(def);
            foreach (Thing thing in GenerateWanderingCaravanInventory())
            {
                wanderingCaravan.inventory.innerContainer.TryAdd(thing);
            }
            return wanderingCaravan;
        }

        private static float CollectionMarketValue(this IEnumerable<Thing> collection)
        {
            float marketValue = 0f;
            foreach (Thing thing in collection)
            {
                marketValue += thing.MarketValue * thing.stackCount;
            }
            return marketValue;
        }

        public static bool IsWanderingCaravan(this Pawn pawn) => pawn.RaceProps.packAnimal && pawn.inventory.innerContainer.Any() && pawn.Faction == null;

        public static bool IsWanderingCaravanDef(this PawnKindDef pawnKindDef) => pawnKindDef.RaceProps.packAnimal;

        private static void ModifyValueBasedOnChance(ref float caravanValue, float rand, float chance, float increase)
        {
            if (rand < chance)
            {
                caravanValue += increase;
            }
        }
    }
}
