using HandyUI_PersonalWorkCategories.Utils;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;
using static HandyUI_PersonalWorkCategories.PersonalWorkCategoriesSettings;

namespace HandyUI_PersonalWorkCategories.Patch
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    [HarmonyPriority(999)]
    class DefGenerator__GenerateImpliedDefs_PostResolve
    {
        static void Prefix()
        {            
            try
            {
                bool compareResult = PersonalWorkCategories.Settings.ProceedDefaultHashComparing(
                    DefDatabase<WorkTypeDef>.AllDefsListForReading,
                    DefDatabase<WorkGiverDef>.AllDefsListForReading);

                if (compareResult)
                {
                    ChangeWorkTypes();
                    ChangeWorkGivers();
                }
            }
            catch(Exception e)
            {
                Log.Error("personalWorkCategories_loadingError".Translate());
                Log.Error("personalWorkCategories_loadingErrorMessage".Translate() + " " + e);
            }
        }

        private static void ChangeWorkTypes()
        {
            List<WorkTypeDef> inGameWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.ListFullCopy();
            DefDatabase<WorkTypeDef>.Clear();
            List<WorkTypeDef> moddedWorkTypes = new List<WorkTypeDef>();

            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<string> allWorkTypes = mod.works.GetKeysAsList();
            int i = 0;
            foreach (string workType in allWorkTypes)
            {
                int extraIndex = mod.GetExtraWorkIndex(workType);

                if (extraIndex >= 0)
                {
                    string root = mod.GetRootOfWorkType(workType);

                    WorkTypeDef extraWorkDef = new WorkTypeDef();
                    ExtraWorkGroup extraWorkCustoms = mod.extraWorks[extraIndex];

                    extraWorkDef.defName = extraWorkCustoms.defName;
                    extraWorkDef.labelShort = extraWorkCustoms.labelShort;
                    extraWorkDef.pawnLabel = string.IsNullOrEmpty(extraWorkCustoms.pawnLabel) ? "personalWorkCategories_defaultPawnLabel".Translate().RawText : extraWorkCustoms.pawnLabel;
                    extraWorkDef.gerundLabel = string.IsNullOrEmpty(extraWorkCustoms.gerundLabel) ? "personalWorkCategories_defaultGerungLabel".Translate().RawText : extraWorkCustoms.gerundLabel;
                    extraWorkDef.description = string.IsNullOrEmpty(extraWorkCustoms.description) ? "personalWorkCategories_defaultDescription".Translate().RawText : extraWorkCustoms.description;
                    extraWorkDef.verb = string.IsNullOrEmpty(extraWorkCustoms.verb) ? "personalWorkCategories_defaultVerb".Translate().RawText : extraWorkCustoms.verb;
                    Converter<string, SkillDef> skillConverter = new Converter<string, SkillDef>(skillDefName => DefDatabase<SkillDef>.GetNamed(skillDefName));
                    extraWorkDef.relevantSkills = extraWorkCustoms.skills.ConvertAll<SkillDef>(skillConverter);

                    WorkTypeDef rootDef = inGameWorkTypes.Find(wt => wt.defName == root);
                    if (rootDef != null)
                    {
                        extraWorkDef.alwaysStartActive = rootDef.alwaysStartActive;
                        extraWorkDef.requireCapableColonist = rootDef.requireCapableColonist;
                        extraWorkDef.workTags = rootDef.workTags;
                        extraWorkDef.relevantSkills = rootDef.relevantSkills;
                        extraWorkDef.alwaysStartActive = rootDef.alwaysStartActive;
                        extraWorkDef.disabledForSlaves = rootDef.disabledForSlaves;
                        extraWorkDef.requireCapableColonist = rootDef.requireCapableColonist;
                    }

                    extraWorkDef.naturalPriority = (allWorkTypes.Count - i) * 50;
                    if (mod.works.GetByKey(workType).Count <= 0) extraWorkDef.visible = false;

                    moddedWorkTypes.Add(extraWorkDef);
                }
                else
                {
                    WorkTypeDef workTypeDef = inGameWorkTypes.Find(wt => wt.defName == workType);
                    if (workTypeDef == null)
                    {
                        Log.Message("Can't find work type " + workType);
                        continue;
                    }
                    workTypeDef.naturalPriority = (allWorkTypes.Count - i) * 50;
                    if (mod.works.GetByKey(workType).Count <= 0) workTypeDef.visible = false;

                    moddedWorkTypes.Add(workTypeDef);
                }

                i++;
            }
            DefDatabase<WorkTypeDef>.Add(moddedWorkTypes);
        }

        private static void ChangeWorkGivers()
        {
            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<string> allWorkTypes = mod.works.GetKeysAsList();
            foreach (string workType in allWorkTypes)
            {
                List<string> allWorkGivers = mod.works.GetByKey(workType);
                int i = 0;
                foreach (string workGiver in allWorkGivers)
                {
                    WorkGiverDef workGiverDef = DefDatabase<WorkGiverDef>.GetNamed(workGiver);
                    if (workGiverDef == null)
                    {
                        continue;
                    }

                    workGiverDef.workType = DefDatabase<WorkTypeDef>.GetNamed(workType);
                    workGiverDef.priorityInType = (allWorkGivers.Count - i) * 10;
                    i++;
                }
            }
        }
    }
}
