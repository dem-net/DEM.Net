using Dapper.Contrib.Extensions;
using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Repository
{
	internal class SqlPoints : List<SqlPoint>
	{
		public SqlPoints(IEnumerable<SqlPoint> collection) : base(collection)
		{
		}
	}
	internal class SqlPoint
	{
		[Key]
		public long PointId { get; set; }

		public string FileName { get; set; }

		public double Latitude { get; set; }

		public double Longitude{ get; set; }

		public float Altitude { get; set; }
	}
}
