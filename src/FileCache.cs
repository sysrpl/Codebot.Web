namespace Codebot.Web;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// The FileCache class stores cached copies of text tiles
/// </summary>
public static class FileCache
{
    private static readonly object locker = new object();
    private static readonly Dictionary<string, DateTime> fileDates = new Dictionary<string, DateTime>();
    private static readonly Dictionary<string, string> fileContent = new Dictionary<string, string>();

    /// <summary>
    /// Read from a file and keep a cached copy of its content
    /// </summary>
    public static string Read(string fileName)
    {
        lock (locker)
        {
            if (File.Exists(fileName))
            {
                string content;
                var a = DateTime.UnixEpoch;
                if (fileDates.ContainsKey(fileName))
                    a = fileDates[fileName];
                else
                    fileDates.Add(fileName, a);
                var b = File.GetLastWriteTimeUtc(fileName);
                if (a != b)
                {
                    fileDates[fileName] = b;
                    content = File.ReadAllText(fileName).Trim();
                    fileContent[fileName] = content;
                }
                else
                    content = fileContent[fileName];
                return content;
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Write to a file
    /// </summary>
    public static void Write(string fileName, string contents)
    {
        lock (locker)
            File.WriteAllText(fileName, contents);
    }
}
