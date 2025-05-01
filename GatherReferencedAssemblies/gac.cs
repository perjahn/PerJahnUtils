using System.Collections.Generic;
using System.Linq;

namespace GatherReferencedAssemblies
{
    class Gac
    {
        static List<string> SystemAssemblies;

        private static void InitIsSystemAssembly()
        {
            SystemAssemblies = [];

            AssemblyCacheEnumx asmEnum = new(null);
            string nextAsm;
            while ((nextAsm = asmEnum.GetNextAssembly()) != null)
            {
                SystemAssemblies.Add(nextAsm.Split(',')[0]);
            }

            var count1 = SystemAssemblies.Count;
            SystemAssemblies = [.. SystemAssemblies.Distinct().OrderBy(a => a)];
            var count2 = SystemAssemblies.Count;
        }

        public static bool IsSystemAssembly(string assemblyname, bool ignoreCase)
        {
            if (SystemAssemblies == null)
            {
                InitIsSystemAssembly();
            }

            return SystemAssemblies.Any(a => string.Compare(assemblyname, a, ignoreCase) == 0);
        }
    }
}
