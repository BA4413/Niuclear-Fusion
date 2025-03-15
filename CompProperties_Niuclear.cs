using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Niuclear
{
    internal class CompProperties_Niuclear : CompProperties
    {
        public ThingDef thingToSpawn;
        public PawnKindDef pawnKind;
        public int extraNutrient;
        public float powerGeneration;
        public int spawnTime = 6000;
        public float basePower = 200f;
        public float explosiveMaxRadius = 32f;
        
        public CompProperties_Niuclear() 
        {
            this.compClass = typeof(CompNiuclear);
            extraNutrient = LoadedModManager.GetMod<Niuclear>().GetSettings<NiuclearSetting>().extraNutrient;
            powerGeneration = LoadedModManager.GetMod<Niuclear>().GetSettings<NiuclearSetting>().powerGeneration;
            explosiveMaxRadius = LoadedModManager.GetMod<Niuclear>().GetSettings<NiuclearSetting>().explosiveMaxRadius;
        }
    }
}
