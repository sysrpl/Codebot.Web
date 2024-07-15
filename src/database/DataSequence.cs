using System.Reflection;
using System.Data.Common;

namespace Codebot.Data;

public class DataSequence : IDisposable
{
    public const int DefaultTimeout = 30;

    public string SplitCommands { get; set; }

    private DbConnection connection;

    public DataSequence()
    {
        connection = DataConnect.CreateConnection();
        connection.Open();
    }

    public void Dispose()
    {
        if (connection != null)
        {
            connection.Close();
            connection.Dispose();
            connection = null;
        }
    }

    public void ExecuteReader(DataRead read, string query, bool resource = false, DataParameters parameters = null, int timeout = DefaultTimeout)
    {
        if (resource)
            query = DataConnect.LoadResourceText(Assembly.GetCallingAssembly(), query);
        using var command = connection.CreateCommand();
        command.CommandText = query;
        if (timeout > 0)
            command.CommandTimeout = timeout;
        DataParameters.Build(command, parameters);
        using var reader = command.ExecuteReader();
        read(reader);
    }

    public int ExecuteNonQuery(string query, bool resource = false, DataParameters parameters = null, int timeout = DefaultTimeout)
    {
        if (resource)
            query = DataConnect.LoadResourceText(Assembly.GetCallingAssembly(), query);
        connection.Open();
        using var command = connection.CreateCommand();
        if (timeout > 0)
            command.CommandTimeout = timeout;
        if (string.IsNullOrEmpty(SplitCommands))
        {
            command.CommandText = query;
            DataParameters.Build(command, parameters);
            return command.ExecuteNonQuery();

        }
        var commands = query.Trim().Split(SplitCommands, StringSplitOptions.RemoveEmptyEntries);
        var result = 0;
        foreach (var c in commands)
        {
            var q = c.Trim();
            if (string.IsNullOrWhiteSpace(q))
                continue;
            command.CommandText = q;
            DataParameters.Build(command, parameters);
            result += command.ExecuteNonQuery();
        }
        return result;
    }

    public IEnumerable<T> ExecuteComposer<T>(Func<DbDataReader, T> composer,
        string query, bool resource = false, DataParameters parameters = null, int timeout = DefaultTimeout)
    {
        IEnumerable<T> list = null;
        if (resource)
            query = DataConnect.LoadResourceText(Assembly.GetCallingAssembly(), query);
        ExecuteReader(r => list = r.Compose(composer), query, false, parameters, timeout);
        return list;
    }
}
