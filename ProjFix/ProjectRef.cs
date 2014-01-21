using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjFix
{
	// Corresponds to a <ProjectReference> tag in project file.
	class ProjectRef
	{
		public string include { get; set; }
		public string shortinclude { get; set; }  // Project file name
		public string name { get; set; }
		public string project { get; set; }
		public string package { get; set; }

		// These 3 lists are used when loading, before validating/fixing, and compacting them to single items.
		public List<string> names { get; set; }
		public List<string> projects { get; set; }
		public List<string> packages { get; set; }
	}
}
