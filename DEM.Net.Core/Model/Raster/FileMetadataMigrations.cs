// FileMetadata.cs
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

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace DEM.Net.Core
{
    internal static class FileMetadataMigrations
    {
        public static FileMetadata Migrate(RasterService rasterService, ILogger logger, FileMetadata oldMetadata, string dataRootDirectory, DEMDataSet dataSet)
        {
            if (oldMetadata != null)
            {
                logger.LogInformation($"Migration metadata file {oldMetadata.Filename} from {oldMetadata.Version} to {FileMetadata.FILEMETADATA_VERSION}");
                
                // 2.2 version requires regeneration
                oldMetadata = rasterService.ParseMetadata(Path.Combine(rasterService.LocalDirectory, oldMetadata.Filename), dataSet.FileFormat);

                //switch (oldMetadata.Version)
                //{
                //    case "2.0":

                //        // 2.1 : relative path
                //        // Find dataset root within path
                //        DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(oldMetadata.Filename));
                //        while (dir.Name != dataSet.Name)
                //        {
                //            dir = dir.Parent;
                //        }
                //        dir = dir.Parent;
                //        // replace directory
                //        oldMetadata.Filename = oldMetadata.Filename.Replace(dir.FullName, dataRootDirectory);
                //        Uri fullPath = new Uri(oldMetadata.Filename, UriKind.Absolute);
                //        if (!(dataRootDirectory.Last() == Path.DirectorySeparatorChar))
                //            dataRootDirectory += Path.DirectorySeparatorChar;
                //        Uri relRoot = new Uri(dataRootDirectory, UriKind.Absolute);

                //        oldMetadata.Filename = Uri.UnescapeDataString(relRoot.MakeRelativeUri(fullPath).ToString());
                //        oldMetadata.FileFormat = dataSet.FileFormat;

                //        break;

                //    case "2.1":

                //        // 2.2 : [Metadata regneration required] file format is now mapped to DEMFileDefinition, lat/lon bounds names changed for clarity, file format changed from DEMFileFormat (name + file extenstion)
                //        // 
                //        // to DEMFileDefinition
                //        oldMetadata.FileFormat = dataSet.FileFormat;

                //        break;

                //    default:

                //        // DEMFileFormat
                //        oldMetadata.FileFormat = dataSet.FileFormat;
                //        break;
                //}

                // set version and fileFormat
                oldMetadata.Version = FileMetadata.FILEMETADATA_VERSION;


            }
            return oldMetadata;
        }
    }
}
