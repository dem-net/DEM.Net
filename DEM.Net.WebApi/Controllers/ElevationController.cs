using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http;
using DEM.Net.WebApi.Models;
using DEM.Net.WebApi.Utility;
using DEM.Net.Lib;
using DEM.Net.Lib.Services;
using GeoJSON.Net.Feature;

namespace DEM.Net.WebApi.Controllers
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
			_elevationService = new ElevationService(new GeoTiffService(dataDirectory));
		}


		[HttpGet]
		[LocationArrayInput("path", Separator = '|')]
		//[Route("api/elevation/{path}/{samples}/{format}", Name = "GetPathElevation")]
		[Route("api/elevation/path/{path:locations}/{samples:int?}", Name = "GetPathElevation")]
		public IHttpActionResult GetPathElevation(Location[] path, int samples = 100)
		{
			try
			{
				var geoPoints = ModelFactory.Create(path);
				var geom = GeometryService.ParseGeoPointAsGeometryLine(geoPoints);
				_elevationService.DownloadMissingFiles(DEMDataSet.AW3D30, geom.GetBoundingBox());
				geoPoints = _elevationService.GetLineGeometryElevation(geom, DEMDataSet.AW3D30, InterpolationMode.Bilinear);
				ElevationMetrics metrics = GeometryService.ComputeMetrics(ref geoPoints);

				if (samples > 2)
				{
					double ratio = 4 / 2;
					double tolerance = (metrics.MaxElevation - metrics.MinElevation) / (samples / ratio);
					geoPoints = DouglasPeucker.DouglasPeuckerReduction(geoPoints, tolerance);
				}


				// Model
				ElevationMetricsModel metricsModel = ModelFactory.CreateElevationMetricsModel(geoPoints, metrics);
				var feature = ModelFactory.CreateFeature(geoPoints, metricsModel);
				return Ok(feature);

			}
			catch (Exception ex)
			{
				return InternalServerError(new Exception(ex.Message));
			}
		}

		[HttpPost]
		[Route("api/elevation/path", Name = "PostPathElevation")]
		public IHttpActionResult PostPathElevation([FromBody] string geojson)
		{
			try
			{
				return Ok(geojson);
				//path.
				//var geoPoints = ModelFactory.Create(path);
				//var geom = GeometryService.ParseGeoPointAsGeometryLine(geoPoints);
				//_elevationService.DownloadMissingFiles(DEMDataSet.AW3D30, geom.GetBoundingBox());
				//geoPoints = _elevationService.GetLineGeometryElevation(geom, DEMDataSet.AW3D30, InterpolationMode.Bilinear);
				//ElevationMetrics metrics = GeometryService.ComputeMetrics(ref geoPoints);

				//if (samples > 2)
				//{
				//	double ratio = 4 / 2;
				//	double tolerance = (metrics.MaxElevation - metrics.MinElevation) / (samples / ratio);
				//	geoPoints = DouglasPeucker.DouglasPeuckerReduction(geoPoints, tolerance);
				//}


				//// Model
				//ElevationMetricsModel metricsModel = ModelFactory.CreateElevationMetricsModel(geoPoints, metrics);
				//var feature = ModelFactory.CreateFeature(geoPoints, metricsModel);
				//return Ok(feature);

			}
			catch (Exception ex)
			{
				return InternalServerError(new Exception(ex.Message));
			}
		}

		[LocationArrayInput("locations", Separator = '|')]
		[Route("api/elevation/locations/{locations:locations}", Name = "GetLocationElevation")]
		public IHttpActionResult GetLocationElevation(Location[] locations)
		{
			try
			{
				return Ok("Test OK");
			}
			catch (Exception ex)
			{
				return InternalServerError(new Exception(ex.Message));
			}
		}






	}


}
