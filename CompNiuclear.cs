using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Niuclear
{
    internal class CompNiuclear : ThingComp
    {
        public bool outMode = true;
        int leftTime = 0;
        int leftTimeBuffer = 0;
        public int cowAmount { get; set; }
        bool writeTimeLeftToSpawn = true;
        CompPowerTrader powerTrader => this.parent.GetComp<CompPowerTrader>();
        public CompProperties_Niuclear PropsNiuclear => (CompProperties_Niuclear)this.props; 

        public override void CompTick()
        {
            base.CompTick();
            leftTime--;
            TickBuffer(outMode);
            StatusDetect(outMode);
        }

        public void StatusDetect(bool outMode)
        {
            if (!(powerTrader.PowerOn &&
                cowAmount > 0))
            {
                powerTrader.PowerOutput = (-1) * PropsNiuclear.basePower;
                writeTimeLeftToSpawn = false;
                return; 
            }
            //产物模式
            if (outMode)
            {
                ProductThing();
                powerTrader.PowerOutput = (-1) * PropsNiuclear.basePower;
                writeTimeLeftToSpawn = true;
            }
            //输电模式
            else
            {
                Reset();
                powerTrader.PowerOutput = PropsNiuclear.extraNutrient * cowAmount * PropsNiuclear.powerGeneration;
                writeTimeLeftToSpawn = false;
            }
        }

        //用于侦测非生产模式的时间重置
        void TickBuffer(bool outMode)
        {
            if (leftTimeBuffer >= 10) 
            {
                leftTimeBuffer = 0;
                if (powerTrader.PowerOn) return;
                if (cowAmount > 0) return;
                Reset();
            }
            leftTimeBuffer++;
        }

        void ProductThing()
        {
            if (leftTime <= 0)
            {
                int productAmount = PropsNiuclear.extraNutrient * cowAmount;
                ThingDef thingToSpawn = PropsNiuclear.thingToSpawn;
                for (int i = 0; i < 9 && productAmount > 0; i++)
                {
                    IntVec3 c = parent.Position + GenAdj.AdjacentCellsAndInside[i];
                    List<Thing> thingsAtC = parent.Map.thingGrid.ThingsListAt(c);
                    bool placedInExistingStack = false;
                    foreach (Thing existingThing in thingsAtC)
                    {
                        if (existingThing.def == thingToSpawn)
                        {
                            int spaceAvailable = existingThing.def.stackLimit - existingThing.stackCount;
                            if (spaceAvailable > 0)
                            {
                                int amountToAdd = Math.Min(productAmount, spaceAvailable);
                                existingThing.stackCount += amountToAdd;
                                productAmount -= amountToAdd;
                                placedInExistingStack = true;
                                if (productAmount <= 0) break;
                            }
                        }
                    }
                    if (!placedInExistingStack)
                    {
                        Thing newThing = ThingMaker.MakeThing(thingToSpawn);
                        newThing.stackCount = Math.Min(productAmount, thingToSpawn.stackLimit);
                        if (GenPlace.TryPlaceThing(newThing, c, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing))
                        {
                            productAmount -= newThing.stackCount;
                        }
                    }
                }
                Reset();
            }
        }

        //重新开始计时
        void Reset()
        {
            leftTime = PropsNiuclear.spawnTime;
        }

        //用于显示生成信息
        public override string CompInspectStringExtra()
        {
            string content = "Content".Translate() + ": " + cowAmount.ToString() + " " + "CowAmount".Translate();
            string timeLeft = "\n"+"NextSpawnedItemIn".Translate() + ": " + leftTime.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor);
            return writeTimeLeftToSpawn ? content + timeLeft : content;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            //切换模式
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "ChangeMode".Translate();
            command_Action.defaultDesc = (outMode) ? "ProductMilk".Translate() : "ProductingPower".Translate();
            command_Action.icon = (outMode) ? ContentFinder<Texture2D>.Get("Icon/NF_MilkMode") : ContentFinder<Texture2D>.Get("Icon/NF_PowerMode");
            command_Action.activateSound = SoundDefOf.Tick_Tiny;

            command_Action.action = delegate
            {
                outMode = !outMode;
            };
            yield return command_Action;


            //开发者模式
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action_Dev1 = new Command_Action();
                command_Action_Dev1.defaultLabel = "DEV: Spawn " + PropsNiuclear.thingToSpawn.label;
                command_Action_Dev1.icon = null;
                command_Action_Dev1.action = delegate
                {
                    leftTime = 0;
                    StatusDetect(outMode);
                };
                yield return command_Action_Dev1;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref leftTime, "NF_leftTime", PropsNiuclear.spawnTime);
            Scribe_Values.Look(ref outMode, "NF_outMode", true);
        }
    }
}