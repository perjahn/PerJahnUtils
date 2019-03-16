using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RemoveMissingFiles
{
    class FileHelper
    {
        public static void RemoveRO(string filename)
        {
            FileAttributes fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
            }
        }
    }
}
