using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using System.IO;
using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using DEM.Net.Lib.Repository;
using System.Data;

namespace DEM.Net.Lib
{
	public static class SqlDemRepository
	{
		private const string CONN_STRING = @"Data Source=.\MSSQL2014;Initial Catalog=DEM;Integrated Security=True;Connect Timeout=15;Encrypt=False;";

		static SqlDemRepository()
		{
			SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
		}

		public static void SaveHeightmap(HeightMap heightmap)
		{
			using (SqlConnection con = new SqlConnection(CONN_STRING))
			{
				con.Open();

				// Insert file
				string fileKey = Path.GetFileName(heightmap.FileMetadata.Filename);
				int fileId = con.Query<int>(@"INSERT INTO dbo.Files ([FileName],[FullFileName],[FileMetadataJson]) VALUES (@FileName, @FullFileName, @JsonMetadata);
																		SELECT SCOPE_IDENTITY();"
																		, new
																		{
																			FileName = fileKey,
																			FullFileName = Path.GetFullPath(heightmap.FileMetadata.Filename),
																			JsonMetadata = JsonConvert.SerializeObject(heightmap.FileMetadata, Formatting.Indented)
																		}
																		).Single();

				if (fileId > 0)
				{
					Trace.TraceInformation($"File {fileKey} inserted.");

					using (SqlBulkCopy bulk = new SqlBulkCopy(con, SqlBulkCopyOptions.TableLock, null))
					{

						bulk.BulkCopyTimeout = 60 * 500; // 5 minutes
																						 //bulk.BatchSize = 10000;
						bulk.DestinationTableName = "dbo.Points";

						//Connect to the database then retrieve the schema information.
						DataTable table = new DataTable();
						table.Columns.Add("FileId", typeof(int));
						//table.Columns.Add("Latitude", typeof(double));
						//table.Columns.Add("Longitude", typeof(double));
						//table.Columns.Add("Altitude", typeof(float));
						table.Columns.Add("PointGeom4326", typeof(SqlGeometry));

						bulk.ColumnMappings.Add("FileId", "FileId");
						//bulk.ColumnMappings.Add("Latitude", "Latitude");
						//bulk.ColumnMappings.Add("Longitude", "Longitude");
						//bulk.ColumnMappings.Add("Altitude", "Altitude");
						bulk.ColumnMappings.Add("PointGeom4326", "PointGeom4326");

						bulk.NotifyAfter = heightmap.Width * heightmap.Height / 10;
						_swBulkChunck.Start(); _swBulkTotal.Start();
						bulk.SqlRowsCopied += Bulk_SqlRowsCopied;
						bulk.WriteToServer(new CoordReader(heightmap, fileId));
						_swBulkChunck.Stop(); _swBulkTotal.Stop();
						Trace.TraceInformation($"Bulk import finished in {_swBulkTotal.Elapsed:g}.");
						bulk.SqlRowsCopied -= Bulk_SqlRowsCopied;

					}


				}
				else
				{
					throw new Exception("File was not inserted.");
				}

				//con.Execute(@"INSERT INTO dbo.Points ([PointGeom4326],[PointGeog4326],[FileId]) VALUES (@Geom4326, @Geog4326, @FileId)")
			}
		}

		private static Stopwatch _swBulkChunck = new Stopwatch();
		private static Stopwatch _swBulkTotal = new Stopwatch();
		private static void Bulk_SqlRowsCopied(object sender, SqlRowsCopiedEventArgs e)
		{
			Trace.TraceInformation($"BulkCopy: {e.RowsCopied} rows copied in {_swBulkChunck.Elapsed:g}.");
			_swBulkChunck.Restart();
		}

		public static void ClearFileData(HeightMap heightmap)
		{
			using (SqlConnection con = new SqlConnection(CONN_STRING))
			{
				con.Open();

				string fileName = Path.GetFileName(heightmap.FileMetadata.Filename);

				int fileId = con.Query<int>(@"SELECT FileId FROM dbo.Files WHERE FileName = @fileName", new
				{
					FileName = fileName
				}).SingleOrDefault();

				if (fileId == 0)
				{
					Trace.WriteLine($"{fileName} file does not exists in database.");
				}
				else {
					// Delete file points
					int numRows = con.Execute(@"DELETE FROM dbo.Points WHERE [FileId] = @fileId"
																			, new
																			{
																				fileId = fileId
																			}
																			);
					Trace.WriteLine($"{numRows} point(s) deleted for {fileName} file.");

					// Delete file
					numRows = con.Execute(@"DELETE FROM dbo.Files WHERE [FileName] = @FileName"
																			, new
																			{
																				FileName = fileName
																			}
																			);
					Trace.WriteLine($"{numRows} files(s) deleted.");
				}
			}
		}
	}
}
