using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.IO
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
}
