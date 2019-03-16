using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeSolutions
{
    class Project
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public string Path { get; set; }
        public string Fullname { get; set; }
        public List<string> Projrows { get; set; }
    }
}
