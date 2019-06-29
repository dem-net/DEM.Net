// DatasetReport.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 Xavier Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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
using System.Text;

namespace DEM.Net.Core.Model
{
    public class DatasetReport
    {
        /// <summary>
        /// Dataset name
        /// </summary>
        public string DatasetName { get; internal set; }

        /// <summary>
        /// Total files in dataset definition
        /// </summary>
        public int TotalFiles { get; internal set; }

        /// <summary>
        /// Number of files currently downloaded and stored locally
        /// </summary>
        public int DownloadedFiles { get; internal set; }

        /// <summary>
        /// File size on locally downloaded files
        /// </summary>
        public double DownloadedSizeMB { get; internal set; }

        /// <summary>
        /// Number of files with generated metadata. This should be equal to <see cref="DownloadedFiles"/>.
        /// </summary>
        public int FilesWithMetadata { get; internal set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Dataset : {this.DatasetName} report :");
            sb.AppendLine($"> {this.TotalFiles} file(s) in dataset.");
            sb.AppendLine($"> {this.DownloadedFiles} file(s) dowloaded ({this.DownloadedSizeMB:F2} MB total).");
            sb.AppendLine($"> {this.FilesWithMetadata} file(s) with DEM.Net metadata.");

            return sb.ToString();
        }
    }
}
