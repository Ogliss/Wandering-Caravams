using RimWorld.Planet;
using Verse;

namespace WanderingCaravans
{
    public class WanderingCaravanEncounterField : MapParent
    {
        private int wanderingCaravansLeaveTick;

        public override void PostMapGenerate()
        {
            wanderingCaravansLeaveTick = Find.TickManager.TicksGame + Rand.Range(30000, 150000);
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            bool shouldRemove = !this.Map.mapPawns.AnyPawnBlockingMapRemoval && Find.TickManager.TicksGame > this.wanderingCaravansLeaveTick;
            alsoRemoveWorldObject = shouldRemove;
            return shouldRemove;
        }
    }
}
