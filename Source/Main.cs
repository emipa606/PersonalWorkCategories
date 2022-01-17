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


    public class PersonalWorkCategoriesSettings : ModSettings
    {
        public enum Category
        {
            Undefined,
            Types,
            Givers
        }

        public struct Preset : IExposable
        {
            public string name;
            public string hash;

            public void ExposeData()
            {
                Scribe_Values.Look<string>(ref name, "Name");
                Scribe_Values.Look<string>(ref hash, "Hash");
            }

            public override string ToString()
            {
                return name.ToString();
            }
        }

        public struct WorkGroup : IExposable
        {
            public string workType;
            public string root;
            public List<string> workGivers;
            public WorkGroup(string workType, List<string> workGivers, string root)
            {
                this.workType = workType;
                this.workGivers = workGivers;
                this.root = root;
            }

            public WorkGroup(string workType, List<string> workGivers): this(workType, workGivers, null)
            {
            }

            public void ExposeData()
            {
                Scribe_Values.Look<string>(ref workType, "WorkType");
                Scribe_Values.Look<string>(ref root, "Root");
                Scribe_Collections.Look<string>(ref workGivers, "WorkGivers");
            }
        }
        internal struct ExtraWorkGroup : IExposable
        {
            public string root;
            public string defName;
            public string labelShort;
            public string pawnLabel;
            public string gerundLabel;
            public string description;
            public string verb;

            void IExposable.ExposeData()
            {
                Scribe_Values.Look<string>(ref root, "Root");
                Scribe_Values.Look<string>(ref defName, "DefName");
                Scribe_Values.Look<string>(ref labelShort, "LabelShort");
                Scribe_Values.Look<string>(ref pawnLabel, "PawnLabel");
                Scribe_Values.Look<string>(ref gerundLabel, "GerundLabel");
                Scribe_Values.Look<string>(ref description, "Description");
                Scribe_Values.Look<string>(ref verb, "Verb");
            }
        }

        public struct WorkElement
        {
            public Category category;
            public string name;
            public ElementStatus status;

            public WorkElement(Category category, string name = null, ElementStatus status = ElementStatus.Normal)
            {
                this.category = category;
                this.name = name;
                this.status = status;
            }
        }

        public enum ElementStatus
        {
            Available,
            Forbidden,
            Normal
        }

        private List<Preset> presets;
        private string selectedPreset;
        private string editablePresetName;
        private const string DEFAULT_PRESET = "Default";

        private Dictionary<string, OrderedDictionary<string, List<string>>> worksByPresets;
        public OrderedDictionary<string, List<string>> works
        {
            get
            {
                return worksByPresets[selectedPreset];
            }
        }

        internal Dictionary<string, List<ExtraWorkGroup>> extraWorksByPresets;
        internal List<ExtraWorkGroup> extraWorks
        {
            get
            {
                return extraWorksByPresets[selectedPreset];
            }
        }

        private Category currentMouseOverColumnType;
        internal string selectedWorkType;

        internal FloatingElement draggedElement;
        private WorkElement currentDropPosition;

        private Vector2 workTypesScrollPosition = Vector2.zero;
        private Vector2 workGiversScrollPosition = Vector2.zero;

        delegate void DrawElementFunc(Rect rowRect, string name, ElementStatus status);

        private float presetsContainerHeight = 0f;
        private float workTypesContainerHeight = 0f;
        internal bool isNeedToShowWarning;
        internal bool isChangesOccurred;

        public override void ExposeData()
        {
            base.ExposeData();
            
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.Saving)
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    ConfirmPresetNameEditing();
                }

                List<string> typesList = null;
                Dictionary<string, List<string>> typesGroups = null;

                Scribe_Collections.Look<Preset>(ref presets, "Presets", LookMode.Deep);
                Scribe_Values.Look<string>(ref selectedPreset, "SelectedPreset");

                if (Scribe.mode == LoadSaveMode.LoadingVars && selectedPreset != null)
                {
                    worksByPresets = new Dictionary<string, OrderedDictionary<string, List<string>>>();
                    editablePresetName = selectedPreset;
                    extraWorksByPresets = new Dictionary<string, List<ExtraWorkGroup>>();
                }

                foreach (Preset preset in presets)
                {
                    Scribe.EnterNode(GenText.CapitalizedNoSpaces(preset.name));

                    List<WorkGroup> workGroups = null;
                    List<ExtraWorkGroup> extraWorksOfPreset = null;

                    if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        typesList = worksByPresets[preset.name].GetKeysAsList();
                        typesGroups = worksByPresets[preset.name].GetAsDictionary();

                        workGroups = new List<WorkGroup>();
                        foreach (string type in typesList)
                        {
                            string root = extraWorksByPresets[preset.name].Find(ewg => ewg.defName == type).root;
                            workGroups.Add(new WorkGroup(type, typesGroups[type], root));
                        }

                        extraWorksOfPreset = extraWorksByPresets[preset.name];
                    }

                    Scribe_Collections.Look<WorkGroup>(ref workGroups, "WorkGroups", LookMode.Deep);
                    Scribe_Collections.Look<ExtraWorkGroup>(ref extraWorksOfPreset, "ExtraWorkGroups", LookMode.Deep);

                    if (Scribe.mode == LoadSaveMode.LoadingVars)
                    {
                        typesList = new List<string>();
                        typesGroups = new Dictionary<string, List<string>>();

                        foreach (WorkGroup group in workGroups)
                        {
                            typesList.Add(group.workType);
                            typesGroups.Add(group.workType, group.workGivers);
                        }

                        worksByPresets.Add(preset.name, new OrderedDictionary<string, List<string>>(typesList, typesGroups));

                        if (extraWorksOfPreset != null)
                            extraWorksByPresets.Add(preset.name, extraWorksOfPreset);
                    }

                    Scribe.ExitNode();
                }
            }
        }

        internal void DoWindowContents(Rect inRect)
        {
            //Log.Clear();
            inRect.yMax -= 10f;

            float upperY = 45f;
            float curX = 0f; ;
            float curY = upperY;
            float centerX = inRect.xMax / 2f;
            const float COLUMN_GAP = 5f;
            const float HALFS_GAP = 10f;
            const float BUTTONS_GAP = 5f;
            float columnWidth = (centerX - COLUMN_GAP) / 2f;
            const float CONTAINER_PADDING = 10f;

            Rect presetsContainer = new Rect(curX, upperY, centerX - HALFS_GAP, presetsContainerHeight);
            Widgets.DrawLightHighlight(presetsContainer);

            curX += CONTAINER_PADDING;
            curY += CONTAINER_PADDING;

            float standartButtonWidth = (centerX - HALFS_GAP - 2f * CONTAINER_PADDING - BUTTONS_GAP * 2f) / 3f;
            float standartButtonHeight = 35f;

            Rect firstButRect = new Rect(curX, curY, standartButtonWidth, standartButtonHeight);

            if (Widgets.ButtonText(firstButRect, "personalWorkCategories_selectPreset".Translate(), true, true, true))
            {
                ConfirmPresetNameEditing();
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                foreach (Preset preset in presets)
                {
                    list.Add(new FloatMenuOption(preset.name, delegate ()
                    {
                        SwitchPresetTo(preset.name);
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(list));
            }

            if (Widgets.ButtonText(new Rect(firstButRect) { x = firstButRect.x + firstButRect.width + BUTTONS_GAP }, "personalWorkCategories_copyPreset".Translate(), true, true, true))
            {
                CopySelectedPresetAndSwitch("", " " + "personalWorkCategories_copy".Translate());
            }

            bool isDefaultPreset = selectedPreset == DEFAULT_PRESET;
            Rect deleteButRect = new Rect(firstButRect) { x = firstButRect.x + (firstButRect.width + BUTTONS_GAP) * 2 };

            if (!isDefaultPreset)
            {
                if(Widgets.ButtonText(deleteButRect, "personalWorkCategories_deletePreset".Translate(), true, true, !isDefaultPreset))
                {
                    DeletePreset(selectedPreset);
                }
            }
            else
            {
                if (Widgets.CustomButtonText(ref deleteButRect, "personalWorkCategories_deletePreset".Translate(), Color.gray, Color.white, Color.black))
                {
                    SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }
            }

            curY += 45f;

            Rect presetLabelRect = new Rect(curX, curY + 5f, 66f, 30f);
            Widgets.Label(presetLabelRect, "personalWorkCategories_current".Translate() + ": ");

            Rect presetNameRect = new Rect(presetLabelRect.xMax + 5f, curY, centerX - HALFS_GAP - 2 * CONTAINER_PADDING - presetLabelRect.xMax, 30f);
            if (!isDefaultPreset)
            {
                if (editablePresetName == null) editablePresetName = selectedPreset;
                editablePresetName = Widgets.TextField(presetNameRect, editablePresetName, 30, Outfit.ValidNameRegex);
            }
            else
                Widgets.Label(new Rect(presetNameRect) { x = presetNameRect.x + 3f, y = presetNameRect.y + 5f, height = presetNameRect.height - 5f }, selectedPreset);

            curY += 30f;

            curX -= CONTAINER_PADDING;
            curY += CONTAINER_PADDING;

            presetsContainerHeight = curY - upperY;

            if (!isDefaultPreset)
            {
                string defaultHash = presets.Find(p => p.name == DEFAULT_PRESET).hash;
                string currentHash = presets.Find(p => p.name == selectedPreset).hash;

                if (defaultHash == currentHash)
                {
                    if (selectedWorkType != null)
                    {
                        curY += 10f;
                        upperY = curY;

                        Rect workTypesContainer = new Rect(presetsContainer) { y = curY, height = workTypesContainerHeight };
                        Widgets.DrawLightHighlight(workTypesContainer);

                        curX += CONTAINER_PADDING;
                        curY += CONTAINER_PADDING;

                        Text.Font = GameFont.Medium;
                        Rect selectedTypeLabelRect = new Rect(curX, curY + 5f, centerX - HALFS_GAP - 2 * CONTAINER_PADDING, 50f);
                        Widgets.Label(selectedTypeLabelRect, GetWorkTypeLabel(selectedWorkType));
                        Text.Font = GameFont.Small;

                        curY += selectedTypeLabelRect.height;

                        int extraIndex = GetExtraWorkIndex(selectedWorkType);
                        if (extraIndex == -1)
                        {
                            Rect splitWorkTypeRect = new Rect(curX, curY, standartButtonWidth, standartButtonHeight);
                            if (Widgets.ButtonText(splitWorkTypeRect, "personalWorkCategories_splitGroup".Translate(), true, true, true))
                            {
                                int i = 1;
                                int ind = -1;
                                string extraWorkGroupName;
                                do
                                {
                                    i++;
                                    extraWorkGroupName = selectedWorkType + i;
                                    ind = extraWorks.FindIndex(wg => wg.defName == extraWorkGroupName);
                                }
                                while (ind != -1);

                                WorkTypeDef rootDef = DefDatabase<WorkTypeDef>.GetNamed(selectedWorkType);

                                ExtraWorkGroup ewg = new ExtraWorkGroup
                                {
                                    root = selectedWorkType,
                                    defName = extraWorkGroupName,
                                    labelShort = rootDef.labelShort + i,
                                    pawnLabel = rootDef.pawnLabel,
                                    gerundLabel = rootDef.gerundLabel,
                                    description = rootDef.description,
                                    verb = rootDef.verb
                                };

                                extraWorks.Add(ewg);

                                int selectedWorkIndex = works.IndexOf(selectedWorkType) + 1;
                                works.Insert(selectedWorkIndex, extraWorkGroupName, new List<string>());

                                selectedWorkType = extraWorkGroupName;
                                isChangesOccurred = true;
                            }

                            Rect resetToDefaultRect = new Rect(splitWorkTypeRect) { x = splitWorkTypeRect.x + (splitWorkTypeRect.width + BUTTONS_GAP) * 1 };
                            if (Widgets.ButtonText(resetToDefaultRect, "personalWorkCategories_resetToDefault".Translate(), true, true, true))
                            {
                                List<string> defaultGivers = worksByPresets[DEFAULT_PRESET].GetByKey(selectedWorkType).ListFullCopy();
                                works.SetAt(works.IndexOf(selectedWorkType), defaultGivers);
                                foreach (ExtraWorkGroup ewg in extraWorks)
                                {
                                    if (ewg.root == selectedWorkType)
                                    {
                                        works.GetByKey(ewg.defName).Clear();
                                    }
                                }
                            }

                            curY += splitWorkTypeRect.height;
                        }
                        else
                        {
                            ExtraWorkGroup ewg = extraWorks[extraIndex];

                            Rect initRect = new Rect(curX, curY, centerX - HALFS_GAP - 2 * CONTAINER_PADDING, 30f);
                            Rect leftPart = initRect.LeftPart(0.3f);
                            leftPart.y += 5f;
                            Rect rightPart = initRect.RightPart(0.7f);

                            Widgets.Label(leftPart, "personalWorkCategories_groupLabel".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + 40f }, "personalWorkCategories_pawnLabel".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + 80f }, "personalWorkCategories_gerungLabel".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + 120f }, "personalWorkCategories_description".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + 160f }, "personalWorkCategories_verb".Translate() +  ":");

                            ewg.labelShort = Widgets.TextField(rightPart, ewg.labelShort);
                            ewg.pawnLabel = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + 40f }, ewg.pawnLabel);
                            ewg.gerundLabel = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + 80f }, ewg.gerundLabel);
                            ewg.description = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + 120f }, ewg.description);
                            ewg.verb = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + 160f }, ewg.verb);

                            if (!extraWorks[extraIndex].Equals(ewg)) isChangesOccurred = true;

                            extraWorks[extraIndex] = ewg;
                            curY += 220f;

                            Rect deleteGroupButRect = new Rect(curX, curY, standartButtonWidth, standartButtonHeight);
                            if (Widgets.ButtonText(deleteGroupButRect, "personalWorkCategories_deleteGroup".Translate(), true, true, true))
                            {
                                string root = GetRootOfWorkType(selectedWorkType);
                                List<string> currentGivers = works.GetByKey(selectedWorkType);
                                works.GetByKey(root).AddRange(currentGivers);
                                works.Remove(selectedWorkType);
                                extraWorks.RemoveAt(extraIndex);
                                selectedWorkType = root;
                            }
                            curY += deleteGroupButRect.height;
                        }


                        curX -= CONTAINER_PADDING;
                        curY += CONTAINER_PADDING;

                        workTypesContainerHeight = curY - upperY;
                    }

                    if (isChangesOccurred)
                    {
                        if (Widgets.ButtonText(
                            new Rect(0, inRect.yMax - standartButtonHeight, centerX - HALFS_GAP, standartButtonHeight),
                            "personalWorkCategories_rebootGame".Translate()))
                        {
                            Write();
                            GenCommandLine.Restart();
                        }
                    }

                    // right window part
                    curY = 45f;

                    Rect workTypesRect = new Rect(inRect) { x = centerX, y = curY, width = columnWidth, yMax = inRect.yMax };

                    List<string> workTypesList = works.GetKeysAsList();

                    currentDropPosition = new WorkElement(Category.Undefined, null);
                    currentMouseOverColumnType = Category.Undefined;

                    DrawList(Category.Types, workTypesRect, ref workTypesScrollPosition, workTypesList,
                        (elemtnRect, name, status) => { DrawWorkTypeElement(elemtnRect, name, status); });

                    Rect workGiversRect = new Rect(workTypesRect) { x = workTypesRect.xMax + COLUMN_GAP };

                    if (selectedWorkType == null)
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        GUI.color = Color.gray;
                        Widgets.Label(workGiversRect, "personalWorkCategories_selectWorkType".Translate());
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = Color.white;
                    }
                    else
                    {
                        DrawList(Category.Givers, workGiversRect, ref workGiversScrollPosition, works.GetByKey(selectedWorkType),
                            (elemtnRect, name, status) => { DrawWorkGiverElement(elemtnRect, name, status); });
                    }
                }
                else
                {
                    Rect rect = new Rect(0f, curY, inRect.xMax, inRect.yMax - curY);

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    GUI.color = Color.gray;
                    Widgets.Label(rect, "personalWorkCategories_presetDoesNotMatch".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;

                    Vector2 center = rect.center;
                    if (Widgets.ButtonText(new Rect()
                    {
                        x = center.x - standartButtonWidth / 2,
                        y = center.y - standartButtonHeight / 2 + 50f,
                        width = standartButtonWidth,
                        height = standartButtonHeight
                    }, "personalWorkCategories_tryToFix".Translate()))
                    {
                        CopySelectedPresetAndSwitch("personalWorkCategories_fixed".Translate() + " ");

                        OrderedDictionary<string, List<string>> defaultWorks = worksByPresets[DEFAULT_PRESET];
                        List<string> defaultWorkTypes = defaultWorks.GetKeysAsList();
                        List<string> currentWorksTypes = works.GetKeysAsList();

                        //Delete deprecated works
                        foreach (string workType in currentWorksTypes)
                        {
                            if (defaultWorkTypes.Contains(workType)) continue;
                            ExtraWorkGroup extraWork = extraWorks.Find(ew => ew.defName == workType);
                            if (defaultWorkTypes.Contains(extraWork.root)) continue;

                            works.Remove(workType);
                        }

                        //Add new works
                        foreach (string mayBeNewWorkType in defaultWorkTypes)
                        {
                            if (works.GetKeysAsList().Contains(mayBeNewWorkType)) continue;

                            int insertedWorkPriority = DefDatabase<WorkTypeDef>.GetNamed(mayBeNewWorkType).naturalPriority;

                            int insertIndex = -1;
                            foreach (string compareWorkType in works.GetKeysAsList())
                            {
                                if (extraWorks.Any(ew => ew.defName == compareWorkType)) continue;
                                int compareWorkPriority = DefDatabase<WorkTypeDef>.GetNamed(compareWorkType).naturalPriority;
                                if (insertedWorkPriority > compareWorkPriority)
                                {
                                    insertIndex = works.IndexOf(compareWorkType);
                                    break;
                                }
                            }

                            List<WorkGiverDef> workGivers = DefDatabase<WorkGiverDef>.AllDefsListForReading.FindAll(wg => wg.workType.defName == mayBeNewWorkType);
                            List<string> giversNames = workGivers.ConvertAll<string>(new Converter<WorkGiverDef, string>(wg => wg.defName));
                            if (insertIndex >= 0)
                                works.Insert(insertIndex, mayBeNewWorkType, giversNames);
                            else
                                works.Add(mayBeNewWorkType, giversNames);
                        }

                        int ind = presets.FindIndex(p => p.name == selectedPreset);
                        presets[ind] = new Preset() { name = presets[ind].name, hash = defaultHash }; 
                    }
                }
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, curY, inRect.xMax, inRect.yMax - curY), "personalWorkCategories_cantChangeDefault".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        private void DrawList(Category column, Rect inRect, ref Vector2 scrollPosition, IEnumerable<string> data, DrawElementFunc drawElement)
        {
            bool isMouseOver = Mouse.IsOver(inRect);
            if (isMouseOver) currentMouseOverColumnType = column;

            int rowsCount = data.EnumerableCount();

            const float GAP = 4f;
            Rect elementRect = new Rect(0f, 0f, inRect.width - 18f, 50f);
            Rect viewRect = new Rect(elementRect) { height = rowsCount * (elementRect.height + GAP) };

            IEnumerator<string> worksList = data.GetEnumerator();

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            float yPosition = 0f;

            while (worksList.MoveNext())
            {
                bool isSource = false;
                bool isTarget = false;
                FloatingElement.DragReaction reaction = FloatingElement.DragReaction.Nothing;
                ElementStatus status = ElementStatus.Normal;

                if (draggedElement != null)
                {
                    bool isAvailable = false;

                    if (column == Category.Types && draggedElement.element.category == Category.Givers)
                    {
                        string draggedParentDef = GetWorkTypeOfWorkGIver(draggedElement.element.name);
                        string draggedParentRoot = GetRootOfWorkType(draggedParentDef);
                        string thisRoot = GetRootOfWorkType(worksList.Current);
                        isAvailable = draggedParentRoot == thisRoot;
                        if (isAvailable) status = ElementStatus.Available;
                        else status = ElementStatus.Forbidden;
                    }

                    Vector2 mp = Event.current.mousePosition;
                    isSource = draggedElement.element.category == column && draggedElement.element.name == worksList.Current;
                    float y = mp.y - yPosition;
                    isTarget = isMouseOver && y >= 0f && y < elementRect.height + 4f;
                    if (isTarget)
                    {
                        reaction = draggedElement.DoDragReaction(column, worksList.Current, status);
                        currentDropPosition = new WorkElement(column, worksList.Current, status);
                    }
                }

                if (!isSource)
                {
                    if (isTarget)
                    {
                        if (reaction == FloatingElement.DragReaction.Spread)
                        {
                            yPosition += elementRect.yMax + GAP;
                        }
                    }
                    drawElement(new Rect(elementRect) { y = yPosition }, worksList.Current, status);
                    yPosition += elementRect.yMax + GAP;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawWorkTypeElement(Rect rowRect, string name, ElementStatus status)
        {
            Widgets.DraggableResult dragResult = Widgets.ButtonInvisibleDraggable(rowRect, status != ElementStatus.Forbidden);
            switch (dragResult)
            {
                case Widgets.DraggableResult.Pressed:

                    SoundDefOf.Click.PlayOneShotOnCamera();
                    selectedWorkType = name;
                    break;

                case Widgets.DraggableResult.Dragged:
                    GUI.FocusControl(null);
                    Find.WindowStack.Add(draggedElement = new WorkTypeFloatingElement(this, rowRect, name));
                    break;
            }

            DrawWorkTypeContent(rowRect, name, Mouse.IsOver(rowRect), status);
        }

        internal void DrawWorkTypeContent(Rect inRect, string name, bool isHighlight = false, ElementStatus status = ElementStatus.Normal)
        {
            if (selectedWorkType == name)
            {
                Widgets.DrawHighlightSelected(inRect);
            }
            else if ((isHighlight && status != ElementStatus.Forbidden) || status == ElementStatus.Available)
            {
                Widgets.DrawHighlight(inRect);
            }
            else
            {
                Widgets.DrawLightHighlight(inRect);
            }

            if (status == ElementStatus.Forbidden) GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inRect) { xMin = inRect.x + 5f }, GetWorkTypeLabel(name));

            int extraIndex = GetExtraWorkIndex(name);
            if (extraIndex >= 0)
            {
                Text.Anchor = TextAnchor.LowerRight;
                GUI.color = Color.gray;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(inRect) { xMax = inRect.xMax - 5f }, "personalWorkCategories_root".Translate() + ": " + GetWorkTypeLabel(extraWorks[extraIndex].root));
                Text.Font = GameFont.Small;
            }
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawWorkGiverElement(Rect rowRect, string name, ElementStatus status)
        {
            Widgets.DraggableResult dragResult = Widgets.ButtonInvisibleDraggable(rowRect, true);

            switch (dragResult)
            {
                case Widgets.DraggableResult.Dragged:
                    GUI.FocusControl(null);
                    Find.WindowStack.Add(draggedElement = new WorkGiverFloatingElement(this, rowRect, name));
                    break;
            }

            DrawWorkGiverContent(rowRect, name, Mouse.IsOver(rowRect));
        }

        internal void DrawWorkGiverContent(Rect inRect, string name, bool isHighlight = false)
        {
            if (isHighlight)
            {
                Widgets.DrawHighlight(inRect);
            }
            else
            {
                Widgets.DrawLightHighlight(inRect);
            }

            string label = GenerateWorkGiverLabel(DefDatabase<WorkGiverDef>.GetNamed(name));
            if (label == null) label = name;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inRect) { xMin = inRect.x + 5f }, label.CapitalizeFirst());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        internal void DraggedElementDropped()
        {
            WorkElement source = draggedElement.element;
            WorkElement target = currentDropPosition;

            draggedElement = null;
            currentDropPosition = new WorkElement(Category.Undefined);

            if (source.category == Category.Undefined || currentMouseOverColumnType == Category.Undefined || source.Equals(target)) return;

            switch (source.category)
            {
                case Category.Types:
                    MoveWorkType(source, target);
                    break;
                case Category.Givers:
                    MoveWorkGiver(source, target);
                    break;
            }
        }

        private string GetWorkTypeOfWorkGIver(string workGiver)
        {
            foreach (string workType in works.GetKeysAsList())
            {
                if (works.GetByKey(workType).Contains(workGiver))
                    return workType;
            }

            return null;
        }

        private void MoveWorkType(WorkElement source, WorkElement target)
        {
            if (target.category != Category.Types) return;

            List<string> workGiverList = works.GetByKey(source.name);
            works.Remove(source.name);
            int index = works.IndexOf(target.name);
            works.Insert(index, source.name, workGiverList);
            isChangesOccurred = true;
        }

        private void MoveWorkGiver(WorkElement source, WorkElement target)
        {
            switch (target.category)
            {
                case Category.Types:
                    string sourceParent = GetWorkTypeOfWorkGIver(source.name);
                    if (selectedWorkType != sourceParent)
                    {
                        RemoveWorkGiverFromParent(source.name);
                        List<string> list = works.GetByKey(selectedWorkType);
                        if (list != null)
                        {
                            list.Add(source.name);
                            isChangesOccurred = true;
                        }
                    }
                    break;

                case Category.Givers:
                    RemoveWorkGiverFromParent(source.name);
                    string parentWorkType = GetWorkTypeOfWorkGIver(target.name);
                    List<string> list2 = works.GetByKey(parentWorkType);
                    if (list2 != null)
                    {
                        int index = list2.IndexOf(target.name);
                        list2.Insert(index, source.name);
                        isChangesOccurred = true;
                    }
                    break;

                case Category.Undefined:
                    if (selectedWorkType != null)
                    {
                        RemoveWorkGiverFromParent(source.name);
                        works.GetByKey(selectedWorkType).Add(source.name);
                        isChangesOccurred = true;
                    }
                    break;
            }
        }

        private void RemoveWorkGiverFromParent(string name)
        {
            foreach (string workType in works.GetKeysAsList())
            {
                if (works.GetByKey(workType).Contains(name))
                {
                    works.GetByKey(workType).Remove(name);
                }
            }
        }

        internal string GetWorkTypeLabel(string defName)
        {
            int index = extraWorks.FindIndex(ew => ew.defName == defName);
            if (index == -1)
            {
                return DefDatabase<WorkTypeDef>.GetNamed(defName).labelShort.CapitalizeFirst();
            }
            else
            {
                return extraWorks[index].labelShort.CapitalizeFirst();
            }
        }

        internal string GetRootOfWorkType(string defName)
        {
            string root = extraWorks.Find(ew => ew.defName == defName).root;
            if (root == null) root = defName;
            return root;
        }

        internal int GetExtraWorkIndex(string defName)
        {
            return extraWorks.FindIndex(ew => ew.defName == defName);
        }

        internal bool ProceedDefaultHashComparing(List<WorkTypeDef> defaultWorkTypes, List<WorkGiverDef> defaultWorkGivers)
        {
            if (presets == null) InitMod();

            defaultWorkTypes.Sort((a, b) => a.naturalPriority >= b.naturalPriority ? -1 : 1);

            string toHash = defaultWorkTypes.ToStringSafeEnumerable() + defaultWorkGivers.ToStringSafeEnumerable();
            string newHash = Sha256Util.ComputeSha256Hash(toHash);

            bool selectedPresetDeprecate = false;
            bool defaultPresetDeprecate = false;

            if (selectedPreset != DEFAULT_PRESET)
            {
                int selectedPresetIndex = presets.FindIndex(p => p.name == selectedPreset);
                if (selectedPresetIndex >= 0 && presets[selectedPresetIndex].hash != newHash)
                {
                    selectedPreset = DEFAULT_PRESET;
                    selectedPresetDeprecate = true;
                }
            }

            int defaultPresetIndex = presets.FindIndex(p => p.name == DEFAULT_PRESET);
            if (defaultPresetIndex >= 0 && presets[defaultPresetIndex].hash != newHash)
            {
                defaultPresetDeprecate = true;
            }

            if (defaultPresetIndex < 0 || defaultPresetDeprecate)
            {
                if (selectedPresetDeprecate && defaultPresetDeprecate) isNeedToShowWarning = true;

                if (defaultPresetIndex >= 0)
                    presets[defaultPresetIndex] = new Preset() { name = DEFAULT_PRESET, hash = newHash };
                else
                    presets.Insert(0, new Preset() { name = DEFAULT_PRESET, hash = newHash });

                selectedPreset = DEFAULT_PRESET;
                worksByPresets.SetOrAdd(DEFAULT_PRESET, new OrderedDictionary<string, List<string>>());
                extraWorksByPresets.SetOrAdd(DEFAULT_PRESET, new List<ExtraWorkGroup>());

                foreach (WorkTypeDef workType in defaultWorkTypes)
                {
                    List<WorkGiverDef> workGivers = defaultWorkGivers.FindAll(wg => wg.workType.defName == workType.defName);
                    List<string> workGiversNames = workGivers.ConvertAll<string>(new Converter<WorkGiverDef, string>(wg => wg.defName));
                    worksByPresets[DEFAULT_PRESET].Add(workType.defName, workGiversNames);
                }

                Write();
                return false;
            }

            return true;
        }

        private void InitMod()
        {
            presets = new List<Preset>();
            worksByPresets = new Dictionary<string, OrderedDictionary<string, List<string>>>();
            extraWorksByPresets = new Dictionary<string, List<ExtraWorkGroup>>();
        }

        public static string GenerateWorkGiverLabel(WorkGiverDef def)
        {
            if (def == null) return null;
            return def.label + (def.emergency ? " (E)" : "");
        }

        internal void ConfirmPresetNameEditing()
        {
            if (selectedPreset != DEFAULT_PRESET && editablePresetName != selectedPreset)
            {
                RenamePreset(selectedPreset, editablePresetName);
                selectedPreset = editablePresetName;
            }
        }

        internal void RenamePreset(string oldName, string newName)
        {
            CheckNameForUnique(ref newName, presets);

            int ind = presets.FindIndex(p => p.name == oldName);
            presets[ind] = new Preset() { name = newName, hash = presets[ind].hash };

            OrderedDictionary<string, List<string>> value = worksByPresets[oldName];
            worksByPresets.Remove(oldName);
            worksByPresets.Add(newName, value);

            List<ExtraWorkGroup> extraWorkGroups = extraWorksByPresets[oldName];
            extraWorksByPresets.Remove(oldName);
            extraWorksByPresets.Add(newName, extraWorkGroups);
        }

        internal void CheckNameForUnique(ref string name, List<Preset> list)
        {
            string compareName = name;
            int i = 1;
            while (list.Any(p => p.name == compareName))
            {
                i++;
                compareName = name + " " + i;
            }
            name = compareName;
        }

        private void CopySelectedPresetAndSwitch(string prefix = "", string postfix = "")
        {
            string newPresetName = prefix + selectedPreset + postfix;
            CheckNameForUnique(ref newPresetName, presets);
            presets.Add(new Preset() { name = newPresetName, hash = presets.Find(p => p.name == selectedPreset).hash });

            OrderedDictionary<string, List<string>> newPreset = new OrderedDictionary<string, List<string>>();
            foreach(string workType in works.GetKeysAsList())
            {
                newPreset.Add(workType, new List<string>(works.GetByKey(workType)));
            }

            worksByPresets.Add(newPresetName, newPreset);
            extraWorksByPresets.Add(newPresetName, new List<ExtraWorkGroup>(extraWorks));
            SwitchPresetTo(newPresetName);
        }

        internal void DeletePreset(string name)
        {
            string presetToDelet = name;
            if (presetToDelet == DEFAULT_PRESET) return;

            int presetIndex = presets.FindIndex(s => s.name == presetToDelet);
            if (presetIndex < 0) return;

            int switchToIndex = presetIndex - 1;
            if (switchToIndex < 0) switchToIndex = presets.FindIndex(s => s.name == DEFAULT_PRESET);
            string switchTo = presets[switchToIndex].name;
            presets.RemoveAt(presetIndex);
            worksByPresets.Remove(presetToDelet);
            extraWorksByPresets.Remove(presetToDelet);
            SwitchPresetTo(switchTo);
        }

        internal void SwitchPresetTo(string to)
        {
            ConfirmPresetNameEditing();
            selectedWorkType = null;
            selectedPreset = to;
            editablePresetName = to;
            isChangesOccurred = true;
        }
    }
}
