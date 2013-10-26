using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.GACManagedAccess;

namespace DllDep
{
	class gac
	{
		static List<string> SystemAssemblies = null;

		private static void InitIsSystemAssembly()
		{
			SystemAssemblies = new List<string>();

			AssemblyCacheEnum asmEnum = new AssemblyCacheEnum(null);
			string nextAsm;
			while ((nextAsm = asmEnum.GetNextAssembly()) != null)
			{
				SystemAssemblies.Add(nextAsm.Split(',')[0]);
			}

			int count1 = SystemAssemblies.Count();
			SystemAssemblies = SystemAssemblies.Distinct().OrderBy(a => a).ToList();
			int count2 = SystemAssemblies.Count();
		}

		public static bool IsSystemAssembly(string assemblyname, out string firstMatchedValue, bool ignoreCase)
		{
			if (SystemAssemblies == null)
				InitIsSystemAssembly();

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
