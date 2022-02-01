using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HandyUI_PersonalWorkCategories.Patch
{
    [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
    class MainMenuDrawer__MainMenuOnGUI
    {
        static void Postfix()
        {
            if (PersonalWorkCategories.Settings.isSavedDataVersionDeprecated)
            {
                Find.WindowStack.Add(new WarningMessage(
                    "personalWorkCategories_warningHeader".Translate(),
                    "personalWorkCategories_newVersionWarningMessage".Translate(),
                    () => PersonalWorkCategories.Settings.isSavedDataVersionDeprecated = false));
            }
            if (PersonalWorkCategories.Settings.isWorksListWasChanged)
            {
                Find.WindowStack.Add(new WarningMessage(
                    "personalWorkCategories_warningHeader".Translate(),
                    "personalWorkCategories_warningBody".Translate(),
                    () => PersonalWorkCategories.Settings.isWorksListWasChanged = false));
            }
        }
    }

    delegate void PreClose();

    internal class WarningMessage : Window
    {
        private string title;
        private string message;
        private PreClose preClose;

        public WarningMessage(string title, string message, PreClose preClose)
        {
            this.title = title;
            this.message = message;
            this.preClose = preClose;

            this.doCloseButton = true;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, 300f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(inRect, title);
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect.ContractedBy(25f, 65f), message);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void PreClose()
        {
            base.PreClose();
            preClose();
        }
    }
}
