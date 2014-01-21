using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjFix
{
	// Corresponds to a <Reference> tag in project file.
	class AssemblyRef
	{
		public string include { get; set; }
		public string shortinclude { get; set; }  // Assembly name
		public string name { get; set; }
		public string hintpath { get; set; }
		public bool? copylocal { get; set; }  // Xml tag name=Private

		// These 3 lists are used when loading, before validating/fixing, and compacting them to single items.
		public List<string> names { get; set; }
		public List<string> hintpaths { get; set; }
		public List<string> copylocals { get; set; }
	}
}
