using HandyUI_PersonalWorkCategories.Utils;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HandyUI_PersonalWorkCategories
{
    public class PersonalWorkCategories : Mod
    {
        public static PersonalWorkCategoriesSettings Settings;

        public PersonalWorkCategories(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PersonalWorkCategoriesSettings>();
            new Harmony("densevoid.hui.personalworkcat").PatchAll(Assembly.GetExecutingAssembly());
        }


        public override string SettingsCategory()
        {
            return "Personal Work Categories";
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
