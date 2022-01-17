using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HandyUI_PersonalWorkCategories.PersonalWorkCategoriesSettings;

namespace HandyUI_PersonalWorkCategories
{
    internal abstract class FloatingElement : Window
    {
        protected PersonalWorkCategoriesSettings settings;
        protected Rect rect;
        public readonly WorkElement element;
        protected static Vector2? dragOffset = new Vector2(-5f, -5f);

        public enum DragReaction { Nothing, Insert, Spread };

        public FloatingElement(PersonalWorkCategoriesSettings settings, Rect rect, Category category, string name, Vector2? dragOffset = null)
        {
            this.settings = settings;
            this.rect = rect;
            this.element = new WorkElement(category, name);

            this.layer = WindowLayer.Super;
            this.closeOnClickedOutside = true;
            this.doWindowBackground = false;
            this.drawShadow = true;
        }

        public abstract DragReaction DoDragReaction(Category category, string name, ElementStatus status);

        protected override float Margin
        {
            get
            {
                return 0f;
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return rect.size;
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 vector = UI.MousePositionOnUIInverted;
            if (dragOffset != null)
            {
                vector -= dragOffset.Value;
            }

            if (vector.x + this.InitialSize.x > (float)UI.screenWidth)
            {
                vector.x = (float)UI.screenWidth - this.InitialSize.x;
            }
            if (vector.y + this.InitialSize.y > (float)UI.screenHeight)
            {
                vector.y = (float)UI.screenHeight - this.InitialSize.y;
            }
            this.windowRect = new Rect(vector.x, vector.y, this.InitialSize.x, this.InitialSize.y);
        }

        public override void WindowUpdate()
        {
            if (Input.GetMouseButtonUp(0))
            {
                Close();
                return;
            }

            SetInitialSizeAndPosition();
            base.WindowUpdate();
        }

        public override void PostClose()
        {
            settings.DraggedElementDropped();
            base.PostClose();
        }
    }

    internal class WorkTypeFloatingElement : FloatingElement
    {
        public WorkTypeFloatingElement(PersonalWorkCategoriesSettings settings, Rect rect, string name) : base(settings, rect, Category.Types, name)
        {
        }

        public override void DoWindowContents(Rect inRect)
        {
            settings.DrawWorkTypeContent(inRect, element.name);
        }

        public override DragReaction DoDragReaction(Category category, string name, ElementStatus status)
        {
            if (category == Category.Types) return DragReaction.Spread;

            return DragReaction.Nothing;
        }
    }

    internal class WorkGiverFloatingElement : FloatingElement
    {
        public WorkGiverFloatingElement(PersonalWorkCategoriesSettings settings, Rect rect, string name) : base(settings, rect, Category.Givers, name)
        {
        }

        public override void DoWindowContents(Rect inRect)
        {
            settings.DrawWorkGiverContent(inRect, element.name);
        }

        public override DragReaction DoDragReaction(Category category, string name, ElementStatus status)
        {
            if (category == Category.Types)
            {
                if (status == ElementStatus.Available)
                {
                    settings.selectedWorkType = name;
                    return DragReaction.Insert;
                }
            }

            if (category == Category.Givers) return DragReaction.Spread;

            return DragReaction.Nothing;
        }
    }
}
