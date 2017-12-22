using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http;
using DEM.Net.Api.Models;
using DEM.Net.Api.Utility;
using DEM.Net.Lib;
using DEM.Net.Lib.Services;

namespace DEM.Net.Api.Controllers
{
	public class ElevationController : ApiController
	{
		readonly ElevationService _elevationService;
		public ElevationController()
		{
			string dataDirectory = ConfigurationManager.AppSettings["DataDir"];
			if (!Path.IsPathRooted(dataDirectory))
			{
				dataDirectory = Path.Combine(HostingEnvironment.MapPath("~"), dataDirectory);
			}
			_elevationService = new ElevationService(new GeoTiffService(GeoTiffReaderType.LibTiff, dataDirectory));
		}

		[LocationArrayInput("path", Separator = '|')]
		public IHttpActionResult Get(Location[] path)
		{
			try
			{
				var geoPoints = ModelFactory.Create(path);
				var geom = GeometryService.ParseGeoPointAsGeometryLine(geoPoints);
				geoPoints = _elevationService.GetLineGeometryElevation(geom, DEMDataSet.AW3D30, InterpolationMode.Bilinear);

				return Ok(ModelFactory.CreateElevationResults(geoPoints));
			}
			catch (Exception ex)
			{
				return InternalServerError(new Exception(ex.Message));
			}
		}
		
	}
}
