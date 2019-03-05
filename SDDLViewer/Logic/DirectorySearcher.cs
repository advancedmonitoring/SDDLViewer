using System.Collections.Generic;
using System.IO;

namespace SDDLViewer.Logic
{
    static class DirectorySearcher
    {
        public static List<string> MyGetDirectories(string path)
        {
            return Search(path, false);
        }

        public static List<string> MyGetFiles(string path)
        {
            return Search(path, true);
        }

        private static List<string> Search(string path, bool isFiles)
        {
            var fullPath = Path.GetFullPath(path);
            var searchPath = Path.Combine(fullPath, "*.*");
            var rv = new List<string>();
            var findData = new Win32Native.WIN32_FIND_DATA();
            using (var hndFindFile = Win32Native.FindFirstFile(searchPath, findData))
            {
                if (hndFindFile == null)
                    return rv;
                var retval = true;
                while (retval)
                {
                    if ((findData.cFileName == ".") || (findData.cFileName == ".."))
                    {
                        retval = Win32Native.FindNextFile(hndFindFile, findData);
                        continue;
                    }
                    if (((findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory) != isFiles)
                        rv.Add(Path.Combine(fullPath, findData.cFileName));
                    retval = Win32Native.FindNextFile(hndFindFile, findData);
                }
            }
            return rv;
        }
    }
}
