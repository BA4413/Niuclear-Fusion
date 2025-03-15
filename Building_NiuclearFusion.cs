using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Niuclear
{
    public class Building_NiuclearFusion : Building_Enterable,
        IThingHolderWithDrawnPawn,
        IThingHolder
    {
        bool cancel = false;
        CompNiuclear cowContent => this.GetComp<CompNiuclear>();
        CompExplosive compExplosive => this.GetComp<CompExplosive>();

        public float HeldPawnDrawPos_Y => this.DrawPos.y + 0.03846154f;

        public float HeldPawnBodyAngle => this.Rotation.AsAngle;

        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public override Vector3 PawnDrawOffset => Vector3.zero;


        public override void Tick()
        {
            base.Tick();
            DetectCow();
        }

        void DetectCow()
        {
            this.cowContent.cowAmount = this.innerContainer.Count;
            this.compExplosive.customExplosiveRadius = 1.9f + 2f * this.innerContainer.Count;
            if (this.compExplosive.customExplosiveRadius >= this.cowContent.PropsNiuclear.explosiveMaxRadius) 
            {
                this.compExplosive.customExplosiveRadius = this.cowContent.PropsNiuclear.explosiveMaxRadius;
            }
            if (innerContainer != null &&
                innerContainer.Contains(selectedPawn))
            {
                cancel = false;
                selectedPawn = null;
            }
        }

        //判断标准
        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            var adultStage = pawn.RaceProps.lifeStageAges.FirstOrDefault(ls => ls.def.defName == "Adult");
            if (pawn.def.race == cowContent.PropsNiuclear.pawnKind.RaceProps &&
                pawn.gender == Gender.Female &&
                pawn.ageTracker.AgeBiologicalYearsFloat >= pawn.def.race.lifeStageAges[1].minAge &&
                pawn.Faction == Faction.OfPlayer) 
            {
                return true;
            }
            return false;
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if ((bool)CanAcceptPawn(pawn))
            {
                if (pawn.holdingOwner != null)
                {
                    pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
                }
                else
                {
                    innerContainer.TryAdd(pawn);
                }
                if (selectedPawn!=null)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
        }

        /// <summary>
        /// 取消行动
        /// </summary>
        /// <param name="pawn">被运送的pawn</param>
        void Cancel(Pawn pawn)
        {
            Pawn pawnCarry = FindCarryPawn(pawn);
            if (pawnCarry != null &&
                pawnCarry.jobs != null &&
                pawnCarry.jobs.curJob != null)
            {
                if (pawnCarry.jobs.curJob.def == JobDefOf.CarryToBuilding)
                {
                    pawnCarry.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    selectedPawn = null;
                }
            }
        }

        /// <summary>
        /// 将pawn运送进去
        /// </summary>
        /// <param name="pawn">被运送的pawn</param>
        protected override void SelectPawn(Pawn pawn)
        {
            Pawn pawnCarry = FindCarryPawn(pawn);
            Job carryJob = JobMaker.MakeJob(JobDefOf.CarryToBuilding, this, pawn);
            carryJob.count = 1;
            pawnCarry.jobs.TryTakeOrderedJob(carryJob, JobTag.Misc);
            /*pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.EnterBuilding, this), JobTag.Misc);*/
        }

        /// <summary>
        /// 寻找能运送的pawn 
        /// </summary>
        /// <param name="pawn">被运送的pawn</param>
        /// <returns></returns>
        Pawn FindCarryPawn(Pawn pawn)
        {
            foreach (Pawn pawnCarry in this.Map.mapPawns.FreeColonistsSpawned)
            {
                if (pawnCarry.CanReach(pawn, PathEndMode.Touch, Danger.Deadly) &&
                    pawnCarry.CanReserve(pawn) &&
                    pawnCarry.CanReach(this, PathEndMode.Touch, Danger.Deadly))
                {
                    return pawnCarry;
                }
            }
            return null;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            //输入
            Command_Action command_Action1 = new Command_Action();
            command_Action1.defaultLabel = (cancel) ? "Cancel".Translate() : "Input".Translate();
            command_Action1.defaultDesc = (cancel) ? "Cancel".Translate() : "InputCow".Translate();
            command_Action1.icon = (cancel) ? ContentFinder<Texture2D>.Get("UI/Designators/Cancel") : ContentFinder<Texture2D>.Get("Icon/NF_Input");
            command_Action1.activateSound = (cancel) ? SoundDefOf.Designate_Cancel : null;
            if (cancel)
            {
                command_Action1.action = delegate
                {
                    Cancel(selectedPawn);
                    cancel = false;
                };
            }
            else
            {
                command_Action1.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    IReadOnlyList<Pawn> allPawnsSpawned = base.Map.mapPawns.AllPawnsSpawned;
                    for (int j = 0; j < allPawnsSpawned.Count; j++)
                    {
                        Pawn pawn = allPawnsSpawned[j];
                        AcceptanceReport acceptanceReport = CanAcceptPawn(pawn);
                        if (!acceptanceReport.Accepted)
                        {
                            if (!acceptanceReport.Reason.NullOrEmpty())
                            {
                                list.Add(new FloatMenuOption(pawn.LabelShortCap + ": " + acceptanceReport.Reason, null, pawn, Color.white));
                            }
                        }
                        else
                        {
                            list.Add(new FloatMenuOption(pawn.LabelShortCap, delegate
                            {
                                SelectPawn(pawn);
                                selectedPawn = pawn;
                                cancel = true;
                            }, pawn, Color.white));
                        }
                    }
                    if (!list.Any())
                    {
                        list.Add(new FloatMenuOption("NoCow".Translate(), null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                };
            }
            yield return command_Action1;

            //输出
            Command_Action command_Action2 = new Command_Action();
            command_Action2.defaultLabel = "Output".Translate();
            command_Action2.defaultDesc = "OutputCow".Translate();
            command_Action2.icon = ContentFinder<Texture2D>.Get("Icon/NF_Output"); ;

            command_Action2.action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                // 获取容器里的 Pawn
                foreach (Pawn pawn in innerContainer)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap, delegate
                    {
                        innerContainer.TryDrop(pawn, ThingPlaceMode.Near, out Thing lastResultingThing, null, null);
                    }, pawn, Color.white));
                }
                if (!list.Any())
                {
                    list.Add(new FloatMenuOption("NoCow".Translate(), null));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            };
            yield return command_Action2;
        }
        
    }
}