using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    class FileSystem
    {
        public static long GetDirectorySize(string directoryFullPath, string searchPattern = "*")
        {
            var fileSizeBytes = new DirectoryInfo(directoryFullPath)
                    .EnumerateFileSystemInfos(searchPattern, SearchOption.AllDirectories)
                    .Where(fi => !fi.Attributes.HasFlag(FileAttributes.Directory)) // check if it is a file
                    .Sum(fi => new FileInfo(fi.FullName).Length);
            return fileSizeBytes;
        }
    }
}
