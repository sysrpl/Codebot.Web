using System.Data.Common;

namespace Codebot.Data
{
	public static class DataReaderExtensions
	{
        public static bool IsNumber(this object value)
        {
            return value is sbyte
                || value is byte
                || value is short
                || value is ushort
                || value is int
                || value is uint
                || value is long
                || value is ulong
                || value is float
                || value is double
                || value is decimal;
        }

        private static bool ReadBool(object value)
        {
            if (value.IsNumber())
            {
                int i = (int)value;
                return i != 0;
            }
            var s = value.ToString().ToUpper();
            return s switch
            {
                "Y" or "YES" or "T" or "TRUE" => true,
                _ => false,
            };
        }

        public static bool ReadBool(this DbDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i))
                return false;
            else
                return ReadBool(reader.GetValue(i));
        }

        public static bool ReadBool(this DbDataReader reader, int column)
        {
            if (reader.IsDBNull(column))
                return false;
            else
                return ReadBool(reader.GetValue(column));
        }

        public static string ReadString(this DbDataReader reader, string column)
		{
			int i = reader.GetOrdinal(column);
			if (reader.IsDBNull(i))
				return string.Empty;
			else
				return reader.GetValue(i).ToString();
		}

		public static string ReadString(this DbDataReader reader, int column)
		{
			if (reader.IsDBNull(column))
				return string.Empty;
			else
				return reader.GetValue(column).ToString();
		}

		public static int ReadInt(this DbDataReader reader, string column)
		{
			int i = reader.GetOrdinal(column);
			if (reader.IsDBNull(i))
				return 0;
			else
				return reader.GetInt32(i);
		}

		public static int ReadInt(this DbDataReader reader, int column)
		{
			if (reader.IsDBNull(column))
				return 0;
			else
				return reader.GetInt32(column);
		}

        public static Int64 ReadLong(this DbDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i))
                return 0;
            else
                return reader.GetInt64(i);
        }

        public static Int64 ReadLong(this DbDataReader reader, int column)
        {
            if (reader.IsDBNull(column))
                return 0;
            else
                return reader.GetInt64(column);
        }

        public static double ReadFloat(this DbDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i))
                return 0;
            else
                return reader.GetDouble(i);
        }

        public static double ReadFloat(this DbDataReader reader, int column)
        {
            if (reader.IsDBNull(column))
                return 0;
            else
                return reader.GetDouble(column);
        }

        public static DateTime ReadDate(this DbDataReader reader, string column)
        {
            return reader.Read<DateTime>(column);
        }

        public static DateTime ReadDate(this DbDataReader reader, int column)
        {
            return reader.Read<DateTime>(column);
        }

        public static T Read<T>(this DbDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i))
                return default;
            else
                return (T)reader[i];
        }

        public static T Read<T>(this DbDataReader reader, int column)
        {
            if (reader.IsDBNull(column))
                return default;
            else
                return (T)reader[column];
        }

        static IEnumerable<T> ComposeDirect<T>(this DbDataReader reader, Func<DbDataReader, T> composer)
        {
            while (reader.Read())
                yield return composer(reader);
        }

        public static IEnumerable<T> Compose<T>(this DbDataReader reader, Func<DbDataReader, T> composer)
        {
            return reader.ComposeDirect(composer).ToList();
        }
    }
}