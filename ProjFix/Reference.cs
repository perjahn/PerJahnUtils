using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjFix
{
	class Reference
	{
		public string include { get; set; }
		public string shortinclude { get; set; }  // Project file name/Assembly name
		public string name { get; set; }

		// This list are used when loading, before validating/fixing, and compacting them to single items.
		public List<string> names { get; set; }
	}
}
