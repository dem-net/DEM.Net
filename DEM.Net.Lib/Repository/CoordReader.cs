using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;

namespace DEM.Net.Lib
{
	internal class CoordReader : IDataReader
	{
		private HeightMap heightmap;
		private int _fileId;

		private List<string> _columns = new List<string>() { "FileId", "PointGeom4326" };
		private int _index = 0;

		public CoordReader(HeightMap heightmap, int fileId)
		{
			this.heightmap = heightmap;
			this._fileId = fileId;
		}


		public int FieldCount
		{
			get
			{
				return _columns.Count;
			}
		}


		public int GetOrdinal(string name)
		{
			return _columns.IndexOf(name);
		}


		public object GetValue(int i)
		{
			switch (i)
			{
				case 0:
					return _fileId;
				case 1:
					double lon = heightmap.Coordinates[_index - 1].Longitude;
					double lat = heightmap.Coordinates[_index - 1].Latitude;
					float alt = heightmap.Coordinates[_index - 1].Altitude;
					CultureInfo ci = CultureInfo.InvariantCulture;
					return SqlGeometry.STPointFromText(new SqlChars(new SqlString($"POINT( {lon.ToString(ci)} {lat.ToString(ci)} {alt.ToString(ci)})")), 4326);
				default:
					throw new ArgumentException($"GetValue: value not found at index {i}.");
			}
		}

		public bool Read()
		{
			if (_index < heightmap.Coordinates.Count)
			{
				_index++;
				return true;
			}
			else
				return false;
		}


		public void Close()
		{

		}

		public void Dispose()
		{

		}

		#region Not implemented


		public object this[string name]
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public object this[int i]
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int Depth
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		public bool IsClosed
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int RecordsAffected
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		public bool GetBoolean(int i)
		{
			throw new NotImplementedException();
		}

		public byte GetByte(int i)
		{
			throw new NotImplementedException();
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		public char GetChar(int i)
		{
			throw new NotImplementedException();
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		public IDataReader GetData(int i)
		{
			throw new NotImplementedException();
		}

		public string GetDataTypeName(int i)
		{
			throw new NotImplementedException();
		}

		public DateTime GetDateTime(int i)
		{
			throw new NotImplementedException();
		}

		public decimal GetDecimal(int i)
		{
			throw new NotImplementedException();
		}

		public double GetDouble(int i)
		{
			throw new NotImplementedException();
		}

		public Type GetFieldType(int i)
		{
			throw new NotImplementedException();
		}

		public float GetFloat(int i)
		{
			throw new NotImplementedException();
		}

		public Guid GetGuid(int i)
		{
			throw new NotImplementedException();
		}

		public short GetInt16(int i)
		{
			throw new NotImplementedException();
		}

		public int GetInt32(int i)
		{
			throw new NotImplementedException();
		}

		public long GetInt64(int i)
		{
			throw new NotImplementedException();
		}

		public string GetName(int i)
		{
			throw new NotImplementedException();
		}
		public DataTable GetSchemaTable()
		{
			throw new NotImplementedException();
		}

		public string GetString(int i)
		{
			throw new NotImplementedException();
		}
		public int GetValues(object[] values)
		{
			throw new NotImplementedException();
		}

		public bool IsDBNull(int i)
		{
			throw new NotImplementedException();
		}

		public bool NextResult()
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}