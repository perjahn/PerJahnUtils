using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeSolutions
{
    class Project
    {
        public string name { get; set; }
        public string guid { get; set; }
        public string path { get; set; }
        public string fullname { get; set; }
        public List<string> projrows { get; set; }
    }

}
