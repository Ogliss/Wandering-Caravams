using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI.Group;
using Verse.AI;
using Verse;

namespace WanderingCaravans
{
    public class LordJob_RetrieveWanderingCaravan : LordJob
    {
        private Pawn targetCaravan;

        public LordJob_RetrieveWanderingCaravan(Pawn targetCaravan)
        {
            if (!targetCaravan.IsWanderingCaravan())
            {
                Log.Error("Retrieve wandering caravan lord job has a target caravan of a non wandering caravan pawn");
            }
            this.targetCaravan = targetCaravan;
        }

        public override StateGraph CreateGraph()
        {
            List<Pawn> lordPawns = this.lord.ownedPawns;
            StateGraph stateGraph = new StateGraph();
            IntVec3 retrieversPosition = default(IntVec3);
            stateGraph.StartingToil = stateGraph.AttachSubgraph(new LordJob_Travel(this.targetCaravan.Position).CreateGraph()).StartingToil;
            LordToil_DefendPoint defendPointLordToil = new LordToil_DefendPoint();
            stateGraph.AddToil(defendPointLordToil);
            LordToil_ExitMap exitMapLordToil = new LordToil_ExitMap();
            stateGraph.AddToil(exitMapLordToil);
            LordToil_ExitMap exitMapOnConditionLordToil = new LordToil_ExitMap();
            stateGraph.AddToil(exitMapOnConditionLordToil);
            stateGraph.AddTransition(new Transition(stateGraph.StartingToil, defendPointLordToil)
            {
                triggers =
                {
                    new Trigger_Memo("TravelArrived")
                },
                postActions =
                {
                    new TransitionAction_Custom(new Action(delegate
                    {
                        if (this.targetCaravan == null || this.targetCaravan.Faction == Faction.OfPlayer)
                        {
                            this.TargetCaravnLost();
                            this.lord.ReceiveMemo("TargetCaravanGone");
                            return;
                        }
                        this.targetCaravan.pather.StartPath(retrieversPosition = lordPawns.First().Position, PathEndMode.ClosestTouch);
                        this.lord.AddPawn(this.targetCaravan);
                    }))
                }
            });
            Transition exitMapTransition = new Transition(defendPointLordToil, exitMapLordToil)
            {
                triggers =
                {
                    new Trigger_Memo("TargetCaravanGone"),
                    new Trigger_Custom(t => t.type == TriggerSignalType.Tick && lordPawns.Contains(this.targetCaravan))
                }
            };
            exitMapTransition.AddSource(stateGraph.StartingToil);
            stateGraph.AddTransition(exitMapTransition);
            stateGraph.AddTransition(new Transition(exitMapLordToil, exitMapOnConditionLordToil)
            {
                triggers =
                {
                    new Trigger_PawnLost()
                },
                preActions =
                {
                    new TransitionAction_Custom(new Action(delegate
                    {
                        if (!lordPawns.Contains(this.targetCaravan))
                        {
                            this.TargetCaravnLost();
                        }
                    }))
                }
            });
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref this.targetCaravan, "targetCaravan");
        }

        private void TargetCaravnLost()
        {
            Faction lordFaction = this.lord.faction;
            Faction.OfPlayer.TryAffectGoodwillWith(lordFaction, -5);
            Messages.Message($"WanderingCaravan.CaravanReturnDemand_Fail".Translate(lordFaction.def.pawnsPlural), MessageTypeDefOf.NegativeEvent);
        }
    }
}
