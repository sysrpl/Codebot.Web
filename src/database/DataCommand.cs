using System.Data.Common;
using System.Reflection;
using Map = System.Collections.Generic.Dictionary<string, object>;

namespace Codebot.Data;

public class DataCommand
{
    private string query;
    private int timeout;
    private DataParameters parameters;

    public static DataCommand Prepare(string query, bool resource = false)
    {
        return new DataCommand
        {
            query = resource ? DataConnect.LoadResourceText(Assembly.GetCallingAssembly(), query) : query,
            parameters = null,
            timeout = 30
        };
    }

    public DataCommand Add(string name, object value)
    {
        (parameters ??= new DataParameters()).Add(name, value);
        return this;
    }

    public DataCommand Timeout(int value)
    {
        timeout = value;
        return this;
    }

    public bool ExecuteHasRows()
    {
        bool rows = false;
        ExecuteReader(r => rows = r.HasRows);
        return rows;
    }

    public void ExecuteReader(DataRead read)
    {
        DataConnect.ExecuteReader(read, query, false, parameters, timeout);
    }

    public void ExecuteReader(DataSequence sequence, DataRead read)
    {
        sequence.ExecuteReader(read, query, false, parameters, timeout);
    }

    public IEnumerable<Map> ExecuteMaps()
    {
        var maps = new List<Map>();
        ExecuteReader(reader =>
        {
            var columns = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));
            while (reader.Read())
            {
                var map = new Map();
                foreach (var column in columns)
                    map.Add(column, reader[column]);
                maps.Add(map);
            }
        });
        return maps;
    }

    public IEnumerable<T> Compose<T>(Func<DbDataReader, T> selector)
    {
        List<T> list = null;
        DataConnect.ExecuteReader(reader => list = reader.Compose(selector).ToList(), query, false, parameters, timeout);
        return list;
    }

    public bool ExecuteSingleResult<T>(out T result)
    {
        bool b = false;
        T r = default;
        DataConnect.ExecuteReader(reader =>
        {
            if (reader.Read())
            {
                r = reader.Read<T>(0);
                b = true;
            }
            else
            {
                b = false;
            }
        }, query, false, parameters, timeout);
        result = r;
        return b;
    }

    public int ExecuteNonQuery()
    {
        return DataConnect.ExecuteNonQuery(query, false, parameters, timeout);
    }
}
