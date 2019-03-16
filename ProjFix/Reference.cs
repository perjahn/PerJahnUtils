using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjFix
{
    class Reference
    {
        public string Include { get; set; }
        public string Shortinclude { get; set; }  // Project file name/Assembly name
        public string Name { get; set; }

        // This list are used when loading, before validating/fixing, and compacting them to single items.
        public List<string> Names { get; set; }
    }
}
