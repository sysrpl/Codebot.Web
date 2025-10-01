using System.Reflection;
using System.Data.Common;

namespace Codebot.Data;

public delegate void DataRead(DbDataReader reader);

public static class DataConnect
{
    public const int DefaultTimeout = 30;

    public static string SplitCommands { get; set; }

    public static Func<DbConnection> CreateConnection { get; set; }

    public static string ConnectionString { get; set; }

    public static string LoadResourceText(Assembly assembly, string name)
    {
        var resource = assembly.GetManifestResourceNames().First(item =>
            item.EndsWith(name, StringComparison.Ordinal));
        using var s = assembly.GetManifestResourceStream(resource);
        using var reader = new StreamReader(s);
        return reader.ReadToEnd();
    }

    public static string LoadResourceText(string name)
    {
        return LoadResourceText(Assembly.GetCallingAssembly(), name);
    }

    public static void ExecuteReader(DataRead read, string query, bool resource = false, DataParameters parameters = null, int timeout = DefaultTimeout)
    {
        if (resource)
            query = LoadResourceText(Assembly.GetCallingAssembly(), query);
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = query;
        if (timeout > 0)
            command.CommandTimeout = timeout;
        DataParameters.Build(command, parameters);
        using var reader = command.ExecuteReader();
        while (reader.Read())
            read(reader);
    }

    static readonly object blocker = new();
    
    public static int ExecuteNonQuery(string query, bool resource = false, DataParameters parameters = null, int timeout = DefaultTimeout)
    {
        if (resource)
            query = LoadResourceText(Assembly.GetCallingAssembly(), query);
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        if (timeout > 0)
            command.CommandTimeout = timeout;
        if (string.IsNullOrEmpty(SplitCommands))
        {
            command.CommandText = query;
            DataParameters.Build(command, parameters);
            lock (blocker)
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
            lock (blocker)
                result += command.ExecuteNonQuery();
        }
        return result;
    }

    public static IEnumerable<T> ExecuteComposer<T>(Func<DbDataReader, T> composer,
        string query, bool resource = false, DataParameters parameters = null, int timeout = DefaultTimeout)
    {
        IEnumerable<T> list = null;
        if (resource)
            query = LoadResourceText(Assembly.GetCallingAssembly(), query);
        ExecuteReader(r => list = r.Compose(composer), query, false, parameters, timeout);
        return list;
    }
}