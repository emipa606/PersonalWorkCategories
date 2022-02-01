using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HandyUI_PersonalWorkCategories.Utils
{
    public static class CommonUtils
    {
        public static void CheckNameForUnique(ref string name, List<string> list, bool addSpace = true)
        {
            string compareName = name;
            int i = 1;
            while (list.Contains(compareName))
            {
                i++;
                compareName = name + (addSpace ? " " : "") + i;
            }
            name = compareName;
        }
    }
}
