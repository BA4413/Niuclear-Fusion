using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Niuclear
{
    public class NiuclearSetting : ModSettings
    {
        public int extraNutrient = 8;
        public float powerGeneration = 10f;
        public float explosiveMaxRadius = 32f;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref extraNutrient, "NF_settings_extraNutrient", 8);
            Scribe_Values.Look(ref powerGeneration, "NF_settings_powerGeneration", 10f);
            Scribe_Values.Look(ref explosiveMaxRadius, "NF_settings_explosiveMaxRadius", 32f);
        }
    }

    public class Niuclear : Mod
    {
        public NiuclearSetting settings;
        public Niuclear(ModContentPack content) : base(content)
        {
            settings = GetSettings<NiuclearSetting>();
        }
        public override string SettingsCategory() => "Niuclear Fusion";
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width / 2f, inRect.height);
            listingStandard.Begin(rect);
            string buf1 = settings.extraNutrient.ToString();
            string buf2 = settings.powerGeneration.ToString();
            string buf3 = settings.explosiveMaxRadius.ToString();
            listingStandard.TextFieldNumericLabeled<int>("MilkToSpawn_Cow".Translate(), ref settings.extraNutrient, ref buf1, min: 1, max: 20000);
            listingStandard.TextFieldNumericLabeled<float>("PowerGeneration_Milk".Translate(), ref settings.powerGeneration, ref buf2);
            listingStandard.TextFieldNumericLabeled<float>("ExplosiveMaxRadius".Translate(), ref settings.explosiveMaxRadius, ref buf3, max:64);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
