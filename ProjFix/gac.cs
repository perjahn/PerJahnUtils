using System.Collections.Generic;
using System.Linq;
using System.GACManagedAccess;

namespace ProjFix
{
    class Gac
    {
        static List<string> SystemAssemblies = null;

        private static void InitIsSystemAssembly()
        {
            SystemAssemblies = [];

            AssemblyCacheEnum asmEnum = new(null);
            string nextAsm;
            while ((nextAsm = asmEnum.GetNextAssembly()) != null)
            {
                SystemAssemblies.Add(nextAsm.Split(',')[0]);
            }

            var count1 = SystemAssemblies.Count;
            SystemAssemblies = [.. SystemAssemblies.Distinct().OrderBy(a => a)];
            var count2 = SystemAssemblies.Count;

            ConsoleHelper.WriteLine($"Found {count1} GAC assemblies, {count2} unique names.", true);

            foreach (var gacass in SystemAssemblies)
            {
                ConsoleHelper.WriteLine($"'{gacass}'", true);
            }
        }

        public static bool IsSystemAssembly(string assemblyname, out string firstMatchedValue, bool ignoreCase)
        {
            if (SystemAssemblies == null)
            {
                InitIsSystemAssembly();
            }

            if (SystemAssemblies.Any(a => string.Compare(assemblyname, a, ignoreCase) == 0))
            {
                firstMatchedValue = SystemAssemblies.First(a => string.Compare(assemblyname, a, ignoreCase) == 0);
                return true;
            }

            firstMatchedValue = null;

            return false;
        }
    }
}
