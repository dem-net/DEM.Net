// NoLogTiffErrorHandler.cs
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

using BitMiracle.LibTiff.Classic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.IO
{
	internal class NoLogTiffErrorHandler : TiffErrorHandler
	{
		public override void ErrorHandler(Tiff tif, string method, string format, params object[] args)
		{
			//base.ErrorHandler(tif, method, format, args);
		}
		public override void ErrorHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
		{
			//base.ErrorHandlerExt(tif, clientData, method, format, args);
		}
		public override void WarningHandler(Tiff tif, string method, string format, params object[] args)
		{
			//base.WarningHandler(tif, method, format, args);
		}
		public override void WarningHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
		{
			//base.WarningHandlerExt(tif, clientData, method, format, args);
		}
	}
    internal class TraceTiffErrorHandler : TiffErrorHandler
    {
        public override void ErrorHandler(Tiff tif, string method, string format, params object[] args)
        {
            Trace.TraceError(method + " " + string.Format(format,args));
        }
        public override void ErrorHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
        {

            //Debug.WriteLine(method + " " + string.Format(format, args));
        }
        public override void WarningHandler(Tiff tif, string method, string format, params object[] args)
        {

            //Debug.WriteLine(method + " " + string.Format(format, args));
        }
        public override void WarningHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
        {

           // Debug.WriteLine(method + " " + string.Format(format, args));
        }
    }
}
