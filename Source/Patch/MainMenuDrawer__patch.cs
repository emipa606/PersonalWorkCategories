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
            if (PersonalWorkCategories.Settings.isNeedToShowWarning)
            {
                Find.WindowStack.Add(new WarningMessage());
            }
        }
    }

    internal class WarningMessage : Window
    {
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, 300f);
            }
        }
        public WarningMessage()
        {
            this.doCloseButton = true;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(inRect, "personalWorkCategories_warningHeader".Translate());
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect.ContractedBy(25f, 65f), "personalWorkCategories_warningBody".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void PreClose()
        {
            base.PreClose();
            PersonalWorkCategories.Settings.isNeedToShowWarning = false;
        }
    }
}
