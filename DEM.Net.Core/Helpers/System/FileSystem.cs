// FileSystem.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
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

    public static class StreamReaderExtensions
    {
        public static void Skip(this StreamReader sr, int numLines)
        {
            for (int i = 1; i <= numLines; i++)
            {
                sr.ReadLine();
            }
        }
        public static string[] ReadUntil(this StreamReader sr, Predicate<string> match)
        {
            List<string> lines = new List<string>();
            string line;
            do
            {
                line = sr.ReadLine();
                lines.Add(line);
            }
            while (match(line) == false && !sr.EndOfStream);

            return lines.ToArray();
        }

    }
}
