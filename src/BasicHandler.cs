#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Codebot.Web
{
    /// <summary>
    /// BasicHandler performs evertyhing you need to handle a response. You
    /// only need to override Run into to make your derived class process requests
    /// </summary>
    public abstract class BasicHandler
    {
        public delegate string WriteConverter(object item);
        public delegate object QuerySectionsFunc(BasicHandler handler);
        public delegate object FindObjectFunc(string key);

        private static readonly Dictionary<string, object> objects;
        private static readonly Dictionary<string, DateTime> includeLog;
        private static readonly Dictionary<string, string> includeData;

        /// <summary>
        /// Initialize the BasicHandler class type
        /// </summary>
        static BasicHandler()
        {
            objects = new Dictionary<string, object>();
            includeLog = new Dictionary<string, DateTime>();
            includeData = new Dictionary<string, string>();
        }

        /// <summary>
        /// The HttpContext associated with the handler
        /// </summary>
        public HttpContext Context { get; private set; }

        /// <summary>
        /// The HttpRequest associated with the handler
        /// </summary>
        public HttpRequest Request { get { return Context.Request; } }

        /// <summary>
        /// The HttpResponse associated with the handler
        /// </summary>
        public HttpResponse Response { get { return Context.Response; } }

        /// <summary>
        /// Returns true if the request is uses the POST method
        /// </summary>
        public bool IsPost
        {
            get
            {
                return Context.Request.Method.Equals("POST", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Returns true if the request is uses the GET method
        /// </summary>
        public bool IsGet
        {
            get
            {
                return Context.Request.Method.Equals("GET", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Returns true if there is request contains a QUERY
        /// </summary>
        public bool IsQuery
        {
            get
            {
                return Context.Request.Query.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if a request contains a FORM
        /// </summary>
        public bool IsForm
        {
            get
            {
                return IsPost ? Context.Request.Form.Count > 0 : false;
            }
        }

        /// <summary>
        /// Returns true if the request is plain, that is not a POST, QUERY, or FORM 
        /// </summary>
        public bool IsPlainRequest
        {
            get
            {
                return !IsPost && !IsQuery & !IsForm;
            }
        }

        /// <summary>
        /// Returns true if the request comes from a local network address
        /// </summary>
        public bool IsLocal
        {
            get
            {
                var address = Context.Connection.RemoteIpAddress.ToString();
                return address.Equals("127.0.0.1") || address.StartsWith("192.168.0.") || address.StartsWith("192.168.1.");
            }
        }

        /// <summary>
        /// Returns true if the user is an administrator
        /// </summary>
        public virtual bool IsAdmin
        {
            get
            {
                return IsLocal;
            }
        }

        /// <summary>
        /// Returns true if a scheme to authenticate has detected a user
        /// </summary>
        public virtual bool IsAuthenticated
        {
            get
            {
                return IsLocal;
            }
        }

        private static readonly string[] platforms = { "windows phone", "windows",
            "macintosh", "linux", "iphone", "android" };

        /// <summary>
        /// Returns the platform name of a few known operating systems
        /// </summary>
        public string Platform
        {
            get
            {
                string agent = Request.Headers["User-Agent"].ToString().ToLower();
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
            get
            {
                return Response.ContentType;
            }
            set
            {
                Response.ContentType = value;
            }
        }

        /// <summary>
        /// Convert a string to type T
        /// </summary>
        public static T Convert<T>(string value)
        {
            return Converter.Convert<string, T>(value);
        }

        /// <summary>
        /// Try to convert a string to type T capturing the result
        /// </summary>
        public static bool TryConvert<T>(string value, out T result)
        {
            return Converter.TryConvert<string, T>(value, out result);
        }

        /// <summary>
        /// Returns true if query request contains key with a value
        /// </summary>
        public bool QueryKeyExists(string key)
        {
            if (!IsQuery)
                return false;
            string s = Context.Request.Query[key];
            return !String.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// Returns true if form request contains key with a value
        /// </summary>
        public bool FormKeyExists(string key)
        {
            if (!IsForm)
                return false;
            string s = Context.Request.Form[key];
            return !String.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// Reads an environment variable
        /// </summary>
        public string ReadVar(string key)
        {
            return Context.GetServerVariable(key);
        }

        /// <summary>
        /// Trys to reads T from the request with a default value
        /// </summary>
        public bool TryRead<T>(string key, out T result, T defaultValue = default(T))
        {
            string s = string.Empty;
            if (IsQuery)
                s = Context.Request.Query[key];
            if (String.IsNullOrEmpty(s) && IsForm)
                s = Context.Request.Form[key];
            s = String.IsNullOrEmpty(s) ? String.Empty : s.Trim();
            if (s.Equals(String.Empty))
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
        public T Read<T>(string key, T defaultValue = default(T))
        {
            TryRead(key, out T result, defaultValue);
            return result;
        }

        /// <summary>
        /// Reads an int from the request with a default value
        /// </summary>
        public int ReadInt(string key, int defaultValue = default(int))
        {
            TryRead(key, out int result, defaultValue);
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
            string result = String.Empty;
            foreach (var key in keys)
                if (TryRead(key, out result))
                    return result;
            return String.Empty;
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
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        /// <summary>
        /// Writes text to the response
        /// </summary>
        public void Write(string s)
        {
            Context.Response.WriteAsync(s);
        }

        /// <summary>
        /// Writes an array of items to the response
        /// </summary>
        public void Write(string s, params object[] args)
        {
            Context.Response.WriteAsync(String.Format(s, args));
        }

        /// <summary>
        /// Writes object to the response
        /// </summary>
        public void Write(object obj)
        {
            Context.Response.WriteAsync(obj.ToString());
        }

        /// <summary>
        /// Writes an array of items to the response using a converter
        /// </summary>
        public void Write(WriteConverter converter, params object[] items)
        {
            foreach (object item in items)
                Write(converter(item));
        }

        /// <summary>
        /// Writes an array of bytes to the response and switch content type to octet stream.
        /// </summary>
        /// <param name="buffer">The buffer of bytes to transmit.</param>
        public void Write(byte[] buffer)
        {
            if (buffer.Length > 0)
            {
                Response.ContentType = "application/octet-stream";
                Response.Body.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Returns the content type for a file
        /// </summary>
        private static string MapContentType(string fileName)
        {
            string ext = fileName.Split('.').Last().ToLower();
            switch (ext)
            {
                case "7z":
                    return "application/x-7z-compressed";
                case "aac":
                    return "audio/aac";
                case "avi":
                    return "video/avi";
                case "bmp":
                    return "image/bmp";
                case "css":
                    return "text/css";
                case "csv":
                    return "text/csv";
                case "doc":
                    return "application/msword";
                case "docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "gif":
                    return "image/gif";
                case "htm":
                case "html":
                    return "text/html";
                case "jpeg":
                case "jpg":
                    return "image/jpeg";
                case "js":
                    return "application/javascript";
                case "json":
                    return "application/json";
                case "mov":
                    return "video/quicktime";
                case "m4a":
                    return "audio/mp4a";
                case "mp3":
                    return "audio/mpeg";
                case "m4v":
                case "mp4":
                    return "video/mp4";
                case "mpeg":
                    return "video/mpeg";
                case "mpg":
                    return "video/mpeg";
                case "ogg":
                    return "audio/ogg";
                case "ogv":
                    return "video/ogv";
                case "pdf":
                    return "application/pdf";
                case "png":
                    return "image/png";
                case "ppt":
                    return "application/vnd.ms-powerpoint";
                case "pptx":
                    return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case "qt":
                    return "video/quicktime";
                case "swf":
                    return "application/x-shockwave-flash";
                case "tif":
                case "tiff":
                    return "image/tiff";
                case "ini":
                case "cfg":
                case "cs":
                case "pas":
                case "txt":
                    return "text/plain";
                case "wav":
                    return "audio/x-wav";
                case "wma":
                    return "audio/x-ms-wma";
                case "wmv":
                    return "audio/x-ms-wmv";
                case "xls":
                    return "application/vnd.ms-excel";
                case "xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "xml":
                    return "text/xml";
                case "zip":
                    return "application/zip";
                default:
                    return "application/octet-stream";
            }
        }

        /// <summary>
        /// Forwards the request to another server using a url and optional write the result back.
        /// </summary>
        /// <param name="url">The url of the server location to forward input to.</param>
        /// <param name="forwardResponse">If forward response is true, then write back the output.</param>
        public void ForwardRequest(string url, bool forwardResponse)
        {
            var items = Request
                .Query
                .Keys
                .Select(key => $"{key}={Request.Query[key].ToString()}");
            var query = string.Join("&", items);
            var request = WebRequest.Create($"{url}/?{query}");
            request.Method = Request.Method;
            if (Request.ContentLength > 0)
            {
                request.ContentType = Request.ContentType;
                request.ContentLength = Request.ContentLength.Value;
                using (var stream = request.GetRequestStream())
                    Request.Body.CopyTo(stream);
            }
            var response = request.GetResponse();
            Response.ContentType = response.ContentType;
            using (var stream = response.GetResponseStream())
                if (forwardResponse)
                    stream.CopyTo(Response.Body);
                else
                    stream.ReadByte();
        }

        /// <summary>
        /// Finds an object based on a key and find function
        /// </summary>
        /// <returns>The object either from cache or the find function</returns>
        /// <param name="key">The key used to chache the object</param>
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
        public string HtmlEscape(string s)
        {
            s = WebUtility.HtmlEncode(s);
            s = s.Replace("&#39;", "'");
            return s;
        }

        /// <summary>
        /// Returns true if a file exists
        /// </summary>
        /// <param name="fileName">The filename to be mapped</param>
        public bool FileExists(string fileName)
        {
            return File.Exists(WebState.MapPath(fileName));
        }

        /// <summary>
        /// Returns true if a folder exists
        /// </summary>
        /// <param name="folder">The folder to be mapped</param>
        public bool FolderExists(string folder)
        {
            return Directory.Exists(WebState.MapPath(folder));
        }

        /// <summary>
        /// Read the contents a cached file without substitutes
        /// </summary>
        /// <param name="fileName">The file to read</param>
        /// <param name="changed">A out bool indicating if the file has changed</param>
        /// <returns>Returns the exact contents of a file without sustitues</returns>
        public static string IncludeReadDirect(string fileName, out bool changed)
        {
            string data = string.Empty;
            fileName = WebState.MapPath(fileName);
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
        /// Read the contents a cached file without substitutes
        /// </summary>
        /// <param name="fileName">The file to read</param>
        /// <returns>Returns the exact contents of a file without sustitues</returns>
        public string IncludeReadDirect(string fileName)
        {
            return IncludeReadDirect(fileName, out bool changed);
        }

        /// <summary>
        /// Read the contents a cached file with include files
        /// </summary>
        /// <param name="fileName">The file to read</param>
        /// <param name="args">An optional list or items to format</param>
        /// <returns>Returns the contents of a file with include files</returns>
        public string IncludeRead(string fileName, params object[] args)
        {
            string include = IncludeReadDirect(fileName, out bool changed);
            int start = include.IndexOf("<%include file=\"");
            int stop = 0;
            while (start > -1)
            {
                stop = include.IndexOf("\"%>", start);
                if (stop < start)
                    break;
                string head = include.Substring(0, start);
                string tail = include.Substring(stop + 3);
                start = start + "<%include file=\"".Length;
                stop = stop - start;
                string insert = include.Substring(start, stop);
                include = head + IncludeReadDirect(insert, out changed) + tail;
                start = include.IndexOf("<%include file=\"");
            }
            if (args.Length > 0)
                include = string.Format(include, args);
            return include;
        }

        /// <summary>
        /// Read the contents a cached file with include files and formated 
        /// </summary>
        /// <param name="fileName">File to include</param>
        /// <returns>Returns the contents of a file with include files and formatted</returns>
        public string IncludeReadObject(string fileName, object item)
        {
            string s = IncludeRead(fileName);
            return s.FormatObject(item).ToString();
        }

        /// <summary>
        /// Includes a cached file
        /// </summary>
        /// <param name="fileName">File to include</param>
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
            if (url == "")
                url = Request.Headers["Referer"].ToString();
            Context.Response.Redirect(url, false);
        }
        /// <summary>
        /// Clears the response and transmits a file using a content type and attachment
        /// </summary>
        private long SendFileData(string fileName, string contentType, bool attachment)
        {
            if (!File.Exists(fileName))
                fileName = WebState.MapPath(fileName);
            Context.Response.Clear();
            Context.Response.ContentType = contentType;
            Context.Response.Headers.Add("Content-Length", new FileInfo(fileName).Length.ToString());
            var disposition = "";
            if (attachment)
                disposition = "attachment; ";
            var name = Path.GetFileName(fileName);
            Context.Response.Headers.Add("Content-Disposition", $"{disposition}fileName=\"{name}\"");
            long responseLength = 0;
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                const int bufferSize = 32 * 1024;
                byte[] buffer = new byte[bufferSize];
                long bytesRead = 0;
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    responseLength += bytesRead;
                    if (bytesRead == bufferSize)
                        Context.Response.Body.Write(buffer);
                    else if (bytesRead > 0)
                    {
                        byte[] small = new byte[bytesRead];
                        Array.Copy(buffer, small, bytesRead);
                        Context.Response.Body.Write(small);
                    }
                } while (bytesRead == bufferSize);
            }
            return responseLength;
        }

        /// <summary>
        /// Clears the response and transmits an attachment file
        /// </summary>
        public long SendAttachment(string fileName, string contentType = null)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = MapContentType(fileName);
            return SendFileData(fileName, contentType, true);
        }

        /// <summary>
        /// Clears the response and transmits a file as a page
        /// </summary>
        public long SendFile(string fileName, string contentType = null)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = MapContentType(fileName);
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
        public string ClassName
        {
            get
            {
                return GetType().ToString().Split('.').Last();
            }
        }

        /// <summary>
        /// Gets the url of the refering page
        /// </summary>
        public string Referer
        {
            get
            {
                return Request.Headers["Referer"].ToString();
            }
        }

        /// <summary>
        /// Normally a reference to this handler
        /// </summary>
        public object Page { get; set; }

        /// <summary>
        /// Gets the title of the response
        /// </summary>
        public virtual string Title { get => "Blank"; }

        /// <summary>
        /// Gets the content of the response
        /// </summary>
        public virtual string Content { get => "Blank"; }

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
        /// Attaches a handler instance to an http context
        /// </summary>
        public void Attach(HttpContext context)
        {
            Context = context;
        }

        /// <summary>
        /// The entry point by the main program
        /// </summary>
        /// <param name="HttpContext">Http context.</param>
        public void ProcessRequest(HttpContext HttpContext)
        {
            Attach(HttpContext);
            Render();
        }
    }
}