using HandyUI_PersonalWorkCategories.Utils;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
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
            bool compareResult = PersonalWorkCategories.Settings.ProceedDefaultHashComparing(
                DefDatabase<WorkTypeDef>.AllDefsListForReading, 
                DefDatabase<WorkGiverDef>.AllDefsListForReading);

            if (compareResult)
            {
                ChangeWorkTypes();
                ChangeWorkGivers();
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
                    WorkTypeDef rootDef = inGameWorkTypes.Find(wt => wt.defName == root);
                    if (rootDef == null)
                    {
                        Log.Message("Can't find work type " + root);
                        continue;
                    }
                    WorkTypeDef extraWorkDef = new WorkTypeDef();
                    ExtraWorkGroup extraWorkCustoms = mod.extraWorks[extraIndex];

                    extraWorkDef.defName = extraWorkCustoms.defName;
                    extraWorkDef.labelShort = extraWorkCustoms.labelShort;
                    extraWorkDef.pawnLabel = extraWorkCustoms.pawnLabel;
                    extraWorkDef.gerundLabel = extraWorkCustoms.gerundLabel;
                    extraWorkDef.description = extraWorkCustoms.description;
                    extraWorkDef.verb = extraWorkCustoms.verb;
                    extraWorkDef.alwaysStartActive = rootDef.alwaysStartActive;
                    extraWorkDef.requireCapableColonist = rootDef.requireCapableColonist;
                    extraWorkDef.workTags = rootDef.workTags;
                    extraWorkDef.relevantSkills = rootDef.relevantSkills;
                    extraWorkDef.alwaysStartActive = rootDef.alwaysStartActive;
                    extraWorkDef.disabledForSlaves = rootDef.disabledForSlaves;
                    extraWorkDef.requireCapableColonist = rootDef.requireCapableColonist;
                    extraWorkDef.naturalPriority = (allWorkTypes.Count - i) * 50;

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
                        Log.Message("Can't find work giver " + workGiver);
                        continue;
                    }

                    workGiverDef.workType = DefDatabase<WorkTypeDef>.GetNamed(workType);
                    workGiverDef.priorityInType = (allWorkGivers.Count - i) * 10;
                }
            }
        }
    }
}
