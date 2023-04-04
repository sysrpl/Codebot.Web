using System.Data;
using System.Data.Common;

namespace Codebot.Data
{
    public static class DataParams
    {
        public static DataParameters Add(string key, object value)
        {
            return new DataParameters().Add(key, value);
        }
    }

    public class DataParameters
    {
        private static readonly Dictionary<Type, DbType> map;

        static DataParameters()
        {
            map = new Dictionary<Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(byte[])] = DbType.Binary,
                [typeof(byte?)] = DbType.Byte,
                [typeof(sbyte?)] = DbType.SByte,
                [typeof(short?)] = DbType.Int16,
                [typeof(ushort?)] = DbType.UInt16,
                [typeof(int?)] = DbType.Int32,
                [typeof(uint?)] = DbType.UInt32,
                [typeof(long?)] = DbType.Int64,
                [typeof(ulong?)] = DbType.UInt64,
                [typeof(float?)] = DbType.Single,
                [typeof(double?)] = DbType.Double,
                [typeof(decimal?)] = DbType.Decimal,
                [typeof(bool?)] = DbType.Boolean,
                [typeof(char?)] = DbType.StringFixedLength,
                [typeof(Guid?)] = DbType.Guid,
                [typeof(DateTime?)] = DbType.DateTime,
                [typeof(DateTimeOffset?)] = DbType.DateTimeOffset
            };
        }

        public static void Build(DbCommand command, DataParameters parameters)
        {
            command.Parameters.Clear();
            if (parameters == null)
                return;
            foreach (var pair in parameters.values)
            {
                var p = command.CreateParameter();
                p.DbType = map[pair.Key.GetType()];
                p.ParameterName = pair.Key;
                p.Value = pair.Value;
                command.Parameters.Add(p);
            }
        }

        private readonly Dictionary<string, object> values;

        public DataParameters()
        {
            values = new Dictionary<string, object>();
        }

        public object this[string key]
        {
            get
            {
                return values[key];
            }
            set
            {
                Add(key, value);
            }
        }

        public DataParameters Add(string key, object value)
        {
            values[key] = value;
            return this;
        }
    }
}