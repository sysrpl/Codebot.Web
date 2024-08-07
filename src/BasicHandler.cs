﻿#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

namespace Codebot.Web;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

/// <summary>
/// BasicHandler performs everything you need to handle a response. You
/// only need to override Run into to make your derived class process requests
/// </summary>
public abstract class BasicHandler : IHttpHandler
{
	public delegate string WriteConverter(object item);
	public delegate object QuerySectionsFunc(BasicHandler handler);
	public delegate object FindObjectFunc(string key);

	private static readonly Dictionary<string, object> objects = [];
	private static readonly Dictionary<string, DateTime> includeLog = [];
	private static readonly Dictionary<string, string> includeData = [];

	/// <summary>
	/// The HttpContext associated with the handler
	/// </summary>
	public HttpContext Context { get; private set; }

	/// <summary>
	/// The HttpRequest associated with the handler
	/// </summary>
	public HttpRequest Request => Context.Request;

	/// <summary>
	/// The HttpResponse associated with the handler
	/// </summary>
	public HttpResponse Response => Context.Response;

	/// <summary>
	/// Returns true if the request is uses the POST method
	/// </summary>
	public bool IsPost => Context.Request.Method.Equals("POST", StringComparison.CurrentCultureIgnoreCase);

	/// <summary>
	/// Returns true if the request is uses the GET method
	/// </summary>
	public bool IsGet => Context.Request.Method.Equals("GET", StringComparison.CurrentCultureIgnoreCase);

	/// <summary>
	/// Returns true if there is request contains a QUERY
	/// </summary>
	public bool IsQuery => Context.Request.Query.Count > 0;

	/// <summary>
	/// Returns true if a request contains a FORM
	/// </summary>
	public bool IsForm => IsPost && Context.Request.Form.Count > 0;

	/// <summary>
	/// Returns true if the request is plain, that is not a POST, QUERY, or FORM
	/// </summary>
	public bool IsPlainRequest => !IsPost && !IsQuery && !IsForm;

	/// <summary>
	/// Returns true if the request comes from a local network address
	/// </summary>
	public bool IsLocal
	{
		get
		{
			var address = IpAddress;
			return address.StartsWith("192.168.0.") || address.StartsWith("192.168.1.");
		}
	}

	/// <summary>
	/// Returns true if the user is an administrator
	/// </summary>
	public virtual bool IsAdmin => IsLocal;

	/// <summary>
	/// Returns true if a scheme to authenticate has detected a user
	/// </summary>
	public virtual bool IsAuthenticated => IsLocal;

	private static readonly string[] platforms = ["windows phone", "windows",
		"macintosh", "linux", "iphone", "android", "ipad"];

	/// <summary>
	/// Returns the platform name of a few known operating systems
	/// </summary>
	public string Platform
	{
		get
		{
			var agent = UserAgent.ToLower();
			foreach (var platform in platforms)
				if (agent.Contains(platform))
					return platform;
			return String.Empty;
		}
	}

	/// <summary>
	/// Shortcut to setting or getting the Response ContentType
	/// </summary>
	public string ContentType
	{
		get => Response.ContentType;
		set => Response.ContentType = value;
	}

	/// <summary>
	/// Convert a string to type T
	/// </summary>
	public static T Convert<T>(string value) => Converter.Convert<string, T>(value);

	/// <summary>
	/// Try to convert a string to type T capturing the result
	/// </summary>
	public static bool TryConvert<T>(string value, out T result) => Converter.TryConvert<string, T>(value, out result);

	/// <summary>
	/// Returns true if query request contains key with a value
	/// </summary>
	public bool QueryKeyExists(string key)
	{
		if (!IsQuery)
			return false;
		string s = Context.Request.Query[key];
		return !string.IsNullOrWhiteSpace(s);
	}

	/// <summary>
	/// Returns true if form request contains key with a value
	/// </summary>
	public bool FormKeyExists(string key)
	{
		if (!IsForm)
			return false;
		string s = Context.Request.Form[key];
		return !string.IsNullOrWhiteSpace(s);
	}

	/// <summary>
	/// Reads an environment variable
	/// </summary>
	public string ReadVar(string key)
	{
		return Context.GetServerVariable(key);
	}

	/// <summary>
	/// Tries to reads T from the request with a default value
	/// </summary>
	public bool TryRead<T>(string key, out T result, T defaultValue = default)
	{
		var s = string.Empty;
		if (IsQuery)
			s = Context.Request.Query[key];
		if (string.IsNullOrEmpty(s) && IsForm)
			s = Context.Request.Form[key];
		s = string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
		if (s.Equals(string.Empty))
		{
			result = defaultValue;
			return false;
		}
		if (TryConvert(s, out result))
		{
			return true;
		}
		else
		{
			result = defaultValue;
			return false;
		}
	}

	/// <summary>
	/// Reads T from the request with a default value
	/// </summary>
	public T Read<T>(string key, T defaultValue = default)
	{
		TryRead(key, out T result, defaultValue);
		return result;
	}

	/// <summary>
	/// Reads a bool from the request with a default value
	/// </summary>
	public bool ReadBool(string key, bool defaultValue = default)
	{
		TryRead(key, out string result, defaultValue ? "true" : "false");
		result = result.ToLower();
		return result switch
		{
			"true" or "t" or "yes" or "y" or "1" => true,
			_ => false,
		};
	}

	/// <summary>
	/// Reads an int from the request with a default value
	/// </summary>
	public int ReadInt(string key, int defaultValue = default)
	{
		TryRead(key, out int result, defaultValue);
		return result;
	}

	/// <summary>
	/// Reads an long from the request with a default value
	/// </summary>
	public long ReadLong(string key, long defaultValue = default)
	{
		TryRead(key, out long result, defaultValue);
		return result;
	}

	/// <summary>
	/// Reads a float from the request with a default value
	/// </summary>
	public float ReadFloat(string key, float defaultValue = default)
	{
		TryRead(key, out float result, defaultValue);
		return result;
	}

	/// <summary>
	/// Reads a string from the request with a default value
	/// </summary>
	public string ReadString(string key, string defaultValue = "")
	{
		return Read(key, defaultValue);
	}

	/// <summary>
	/// Reads a the first value from request or return empty string if none exists
	/// </summary>
	public string ReadAny(params string[] keys)
	{
		foreach (string key in keys)
			if (TryRead(key, out string result))
				return result;
		return string.Empty;
	}

	/// <summary>
	/// Reads a string from the request with a default value
	/// </summary>
	public string Read(string key, string defaultValue = "")
	{
		TryRead(key, out string result, defaultValue);
		return result;
	}

	/// <summary>
	/// Reads the entire body as text
	/// </summary>
	public string ReadBody()
	{
		Request.EnableBuffering();
		using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
		var task = reader.ReadToEndAsync();
		task.Wait();
		var body = task.Result;
		Request.Body.Position = 0;
		return body;
	}

	/// <summary>
	/// Writes text to the response
	/// </summary>
	public void Write(string s) => Context.Response.WriteAsync(s);

	/// <summary>
	/// Writes an array of items to the response
	/// </summary>
	public void Write(string s, params object[] args) => Write(string.Format(s, args));

	/// <summary>
	/// Writes object to the response
	/// </summary>
	public void Write(object obj) => Write(obj.ToString());

	/// <summary>
	/// Writes an array of items to the response using a converter
	/// </summary>
	public void Write(WriteConverter converter, params object[] items)
	{
		foreach (object item in items)
			Write(converter(item));
	}

	/// <summary>
	/// Writes an array of bytes to the response and switch content type
	/// </summary>
	/// <param name="buffer">The buffer of bytes to transmit.</param>
	/// <param name="contentType">Optionally use a content type</param>
	public void Write(byte[] buffer, string contentType = "application/octet-stream")
	{
		if (buffer.Length > 0)
		{
			if (!string.IsNullOrWhiteSpace(contentType))
				Response.ContentType = contentType;
			Response.Body.WriteAsync(buffer, 0, buffer.Length);
		}
	}

	/// <summary>
	/// The current requested path
	/// </summary>
	public string RequestPath { get => Context.Request.Path.Value ?? ""; }

	/// <summary>
	/// Map a path to application file path
	/// </summary>
	public static string AppPath(string path) => App.AppPath(path);

	/// <summary>
	/// Map a web request path to a physical file path
	/// </summary>
	public string MapPath(string path) => App.MapPath(Context, path);

	/// <summary>
	/// The current ip address of the client
	/// </summary>
	public string IpAddress
	{
		get => Context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
	}

	/// <summary>
	/// The current user agent of the client
	/// </summary>
	public string UserAgent
	{
		get => Context.Request.Headers.UserAgent.FirstOrDefault()?.ToString() ?? "Unknown";
	}

	/// <summary>
	/// Finds an object based on a key and find function
	/// </summary>
	/// <returns>The object either from cache or the find function</returns>
	/// <param name="key">The key used to cache the object</param>
	/// <param name="find">The function used to find the object</param>
	public static object FindObject(string key, FindObjectFunc find)
	{
		lock (objects)
		{
			if (objects.ContainsKey(key))
				return objects[key];
			var item = find(key);
			objects.Add(key, item);
			return item;
		}
	}

	/// <summary>
	/// Escapes html characters
	/// </summary>
	/// <param name="s">The string to escape</param>
	/// <returns>Returns a string with html characters escaped</returns>
	static public string HtmlEscape(string s)
	{
		s = WebUtility.HtmlEncode(s);
		s = s.Replace("&#39;", "'");
		return s;
	}

	/// <summary>
	/// Unescapes html characters
	/// </summary>
	/// <param name="s">The string to unescape</param>
	/// <returns>Returns a string with html characters</returns>
	static public string HtmlUnescape(string s)
	{
		s = WebUtility.HtmlDecode(s);
		s = s.Replace("&#39;", "'");
		return s;
	}

	/// <summary>
	/// Returns true if a file mapped under the wwwroot path exists
	/// </summary>
	/// <param name="fileName">The filename to be mapped</param>
	public bool FileExists(string fileName) => File.Exists(MapPath(fileName));

	/// <summary>
	/// Delete a file mapped under the wwwroot path
	/// </summary>
	/// <param name="fileName">The filename to be deleted</param>
	public void FileDelete(string fileName) => File.Delete(MapPath(fileName));


	/// <summary>
	/// Returns the text contents of a file mapped under the wwwroot path
	/// </summary>
	/// <param name="fileName">The filename to be mapped</param>
	public string FileReadText(string fileName) => File.ReadAllText(MapPath(fileName));


	/// <summary>
	/// Write text content to a file mapped under the wwwroot path
	/// </summary>
	/// <param name="fileName">The filename to be mapped</param>
	public void FileWriteText(string fileName, string contents) => File.WriteAllText(MapPath(fileName), contents);

	/// <summary>
	/// Returns true if a folder mapped under the wwwroot path exists
	/// </summary>
	/// <param name="folder">The folder to be mapped</param>
	public bool FolderExists(string folder) => Directory.Exists(MapPath(folder));

	/// <summary>
	/// Read and caches the contents of a file without includes or templates
	/// </summary>
	/// <param name="fileName">The file to read and cache</param>
	/// <param name="changed">A output bool indicating if the file has changed</param>
	/// <returns>Returns the exact contents of a file</returns>
	public string IncludeReadDirect(string fileName, out bool changed)
	{
		string data = string.Empty;
		fileName = MapPath(fileName);
		lock (includeLog)
		{
			DateTime change = File.GetLastWriteTime(fileName);
			if (includeLog.ContainsKey(fileName))
			{
				if (includeLog[fileName].Equals(change))
				{
					changed = false;
					data = includeData[fileName];
				}
				else
				{
					changed = true;
					data = File.ReadAllText(fileName);
					includeLog[fileName] = change;
					includeData[fileName] = data;
				}
			}
			else
			{
				changed = true;
				data = File.ReadAllText(fileName);
				includeLog.Add(fileName, change);
				includeData.Add(fileName, data);
			}
		}
		return data;
	}

	/// <summary>
	/// Read and caches the contents of a file without includes or templates
	/// </summary>
	/// <param name="fileName">The file to read and cache</param>
	/// <returns>Returns the exact contents of a file</returns>
	public string IncludeReadDirect(string fileName)
	{
		return IncludeReadDirect(fileName, out _);
	}

	/// <summary>
	/// Read and caches the contents of a file with includes and no templating
	/// </summary>
	/// <param name="fileName">The file to read and cache</param>
	/// <param name="args">An optional list or items to format</param>
	/// <returns>Returns the contents of a file with include files</returns>
	public string IncludeRead(string fileName, params object[] args)
	{
		string include = IncludeReadDirect(fileName, out _);
		int start = include.IndexOf("<%include file=\"");
		int stop;
		while (start > -1)
		{
			stop = include.IndexOf("\"%>", start);
			if (stop < start)
				break;
			string head = include.Substring(0, start);
			string tail = include.Substring(stop + 3);
			start += "<%include file=\"".Length;
			stop -= start;
			string insert = include.Substring(start, stop);
			include = head + IncludeReadDirect(insert, out _) + tail;
			start = include.IndexOf("<%include file=\"");
		}
		if (args.Length > 0)
			include = string.Format(include, args);
		return include;
	}

	/// <summary>
	/// Read and cache the contents a file with includes and templating
	/// </summary>
	/// <param name="fileName">File to read and cache</param>
	/// <param name="item">Object to use with templating</param>
	/// <returns>Returns the contents of a file templated using an object</returns>
	public string IncludeReadObject(string fileName, object item = null)
	{
		if (item is null)
			item = this;
		string s = IncludeRead(fileName);
		return s.FormatObject(item).ToString();
	}

	/// <summary>
	/// Include file in the response optionally templating the contents
	/// </summary>
	/// <param name="fileName">The file to read and cache</param>
	/// <param name="isTemplate">Optionally format the include as a template of the current handler</param>
	public void Include(string fileName, bool isTemplate = false)
	{
		string s = IncludeRead(fileName);
		if (isTemplate)
			s = s.FormatObject(this).ToString();
		Write(s);
	}

	/// <summary>
	/// Ends the response and redirects the client to a new page
	/// </summary>
	public void Redirect(string url = "")
	{
		if (string.IsNullOrWhiteSpace(url))
			url = Request.Headers["Referer"].ToString();
		Context.Response.Redirect(url, false);
	}

	/// <summary>
	/// Clears the response and transmits a file using a content type and attachment
	/// </summary>
	private long SendFileData(string fileName, string contentType, bool attachment)
	{
		if (!File.Exists(fileName))
			fileName = MapPath(fileName);
		if (!File.Exists(fileName))
		{
			Response.StatusCode = (int)HttpStatusCode.NotFound;
			return 0;
		}
		var info = new FileInfo(fileName);
		var length = info.Length;
		Context.Response.Clear();
		Context.Response.ContentType = contentType;
		Context.Response.Headers.Append("Content-Length", length.ToString());
		var disposition = "";
		if (attachment)
			disposition = "attachment; ";
		var name = Path.GetFileName(fileName);
		Context.Response.Headers.Append("Content-Disposition", $"{disposition}fileName=\"{name}\"");
		using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read,
			FileShare.Read, 4096, FileOptions.Asynchronous);
		stream.CopyToAsync(Context.Response.Body).Wait();
		return length;
	}

	/// <summary>
	/// Transmits a file using seekable range
	/// </summary>
	/// <remarks>See http://stackoverflow.com/questions/5429947/
	/// supporting-resumable-http-downloads-through-an-ashx-handler</remarks>
	public long TransmitFile(string fileName, bool attachment = false)
	{
		#pragma warning disable ASP0015 // Suggest using IHeaderDictionary properties

		string ReadHeader(string key)
		{
			string result = null;
			if (Request.Headers.TryGetValue(key, out var value))
				result = value.FirstOrDefault();
			return string.IsNullOrWhiteSpace(result) ? null : result;
		}

		void Transmit(string filePath, long fileLength, long offset, long length)
		{
			var remainingBytes = fileLength - offset;
			var actualLength = length > 0 ? Math.Min(length, remainingBytes) : remainingBytes;
			using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
				FileShare.Read, 4096, FileOptions.Asynchronous);
			stream.Seek(offset, SeekOrigin.Begin);
			var buffer = new byte[actualLength];
			int sent = 0;
			do
			{
				sent = stream.ReadAsync(buffer, 0, (int)actualLength).Result;
				Response.Body.WriteAsync(buffer).AsTask().Wait();
				actualLength -= sent;
			}
			while (actualLength > 0);
		}

		if (!File.Exists(fileName))
			fileName = MapPath(fileName);
		if (!File.Exists(fileName))
		{
			Response.StatusCode = (int)HttpStatusCode.NotFound;
			return 0;
		}
		var fileInfo = new FileInfo(fileName);
		var responseLength = fileInfo.Exists ? fileInfo.Length : 0;
		var startIndex = 0;
		var etag = Read("v");
		var ifMatch = ReadHeader("If-Match");
		var ifMatchNone = ReadHeader("If-None-Matc");
		var range = ReadHeader("Range");
		var ifRange = ReadHeader("If-Range");
		if (ifMatch is not null && ifMatch != "*" && ifMatch != etag)
		{
			Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
			return 0;
		}
		if (ifMatchNone is not null && ifMatchNone == etag)
		{
			Response.StatusCode = (int)HttpStatusCode.NotModified;
			return 0;
		}
		if (range is not null && ifRange is not null || ifRange == etag)
		{
			var match = Regex.Match(Request.Headers["Range"], @"bytes=(\d*)-(\d*)");
			startIndex = Convert<int>(match.Groups[1].Value);
			responseLength = (Convert<int?>(match.Groups[2].Value) + 1 ?? fileInfo.Length) - startIndex;
			Response.StatusCode = (int)HttpStatusCode.PartialContent;
			Response.Headers["Content-Range"] = "bytes " + startIndex + "-" + (startIndex + responseLength - 1) + "/" + fileInfo.Length;
		}
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.Headers["Content-Disposition"] = (attachment ? "attachment; " : "") + "filename=" + Path.GetFileName(fileName);
		Response.Headers["Content-Type"] = Tools.MapContentType(fileName);
		Response.Headers["Content-Length"] = responseLength.ToString();
		Response.Headers["Cache-Control"] = "public, max-age=600";
		Response.Headers["Expires"] = DateTime.UtcNow.AddMinutes(10).ToString("R");
		Response.Headers["ETag"] = etag;
		Transmit(fileName, fileInfo.Length, startIndex, responseLength);
		return responseLength;
		#pragma warning restore ASP0015 // Suggest using IHeaderDictionary properties
	}

	/// <summary>
	/// Clears the response and transmits an attachment file
	/// </summary>
	public long SendAttachment(string fileName, string contentType = null)
	{
		if (string.IsNullOrWhiteSpace(contentType))
			contentType = Tools.MapContentType(fileName);
		return SendFileData(fileName, contentType, true);
	}

	/// <summary>
	/// Clears the response and transmits a file as a page
	/// </summary>
	public long SendFile(string fileName, string contentType = null)
	{
		if (string.IsNullOrWhiteSpace(contentType))
			contentType = Tools.MapContentType(fileName);
		return SendFileData(fileName, contentType, false);
	}

	/// <summary>
	/// Process a template into a buffer
	/// </summary>
	protected void InsertTemplate<T>(StringBuilder buffer) where T : TemplateHandler, new()
	{
		T handler = new T();
		handler.ProcessTemplate(Context, buffer);
	}

	/// <summary>
	/// Process a template returning a string
	/// </summary>
	protected StringBuilder InsertTemplate<T>() where T : TemplateHandler, new()
	{
		var buffer = new StringBuilder();
		InsertTemplate<T>(buffer);
		return buffer;
	}

	/// <summary>
	/// Process a template writing it to the response
	/// </summary>
	protected void WriteTemplate<T>() where T : TemplateHandler, new()
	{
		var buffer = InsertTemplate<T>();
		Write(buffer);
	}

	/// <summary>
	/// Gets the name of the class
	/// </summary>
	public string PathName
	{
		get
		{
			var items = Context.Request.GetDisplayUrl();
			if (items.Length == 1)
				return "home";
			return "home" + String.Join("-", items, 0, items.Length - 1);
		}
	}

	/// <summary>
	/// Gets the name of the class
	/// </summary>
	public string ClassName => GetType().ToString().Split('.').Last();

	/// <summary>
	/// Gets the url of the referring page
	/// </summary>
	public string Referer => Request.Headers["Referer"].ToString();

	/// <summary>
	/// Normally a reference to this handler
	/// </summary>
	public object Page { get; set; }

	/// <summary>
	/// Gets the title of the response
	/// </summary>
	public virtual string Title => "Blank";

	/// <summary>
	/// Gets the content of the response
	/// </summary>
	public virtual string Content => "Blank";

	/// <summary>
	/// Run is invoked by the Render() method
	/// </summary>
	protected abstract void Run();

	/// <summary>
	/// Render is invoked by ProcessRequest() and in turn invokes Run()
	/// </summary>
	protected virtual void Render()
	{
		Page = this;
		Run();
	}

	/// <summary>
	/// ProcessRequest takes ownership of an HttpContext and generates a response
	/// </summary>
	public void ProcessRequest(HttpContext context)
	{
		Context = context;
		Render();
	}
}
