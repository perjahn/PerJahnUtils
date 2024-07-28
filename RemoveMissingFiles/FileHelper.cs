using System.IO;

namespace RemoveMissingFiles
{
    class FileHelper
    {
        public static void RemoveRO(string filename)
        {
            var fa = File.GetAttributes(filename);
            if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
            }
        }
    }
}
