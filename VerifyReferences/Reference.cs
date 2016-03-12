using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VerifyReferences
{
	class Reference
    {
        public string include { get; set; }
        public string shortinclude { get; set; }
        public string hintpath { get; set; }
        public string path { get; set; }
    }
}
