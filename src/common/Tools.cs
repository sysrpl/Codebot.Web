namespace Codebot.Web;

public static class Tools
{
	/// <summary>
	/// Returns the content type for a file
	/// </summary>
	public static string MapContentType(string fileName)
	{
		string ext = fileName.Split('.').Last().ToLower();
        return ext switch
        {
            "7z" => "application/x-7z-compressed",
            "aac" => "audio/aac",
            "avi" => "video/avi",
            "bmp" => "image/bmp",
            "c" => "text/c",
            "cpp" => "text/cpp",
            "css" => "text/css",
            "csv" => "text/csv",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "gif" => "image/gif",
            "glsl" => "image/glsl",
            "htm" or "html" => "text/html",
            "jpeg" or "jpg" => "image/jpeg",
            "js" => "application/javascript",
            "json" => "application/json",
            "mov" => "video/quicktime",
            "m4a" => "audio/mp4a",
            "mp3" => "audio/mpeg",
            "m4v" or "mp4" => "video/mp4",
            "mpeg" or "mpg" => "video/mpeg",
            "ogg" => "audio/ogg",
            "ogv" => "video/ogv",
            "pas" => "text/pascal",
            "pdf" => "application/pdf",
            "png" => "image/png",
            "ppt" => "application/vnd.ms-powerpoint",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "qt" => "video/quicktime",
            "svg" => "image/svg+xml",
            "swf" => "application/x-shockwave-flash",
            "tif" or "tiff" => "image/tiff",
            "ini" or "cfg" or "cs" or "pas" or "txt" => "text/plain",
            "wav" => "audio/x-wav",
            "wma" => "audio/x-ms-wma",
            "wmv" => "audio/x-ms-wmv",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "xml" => "text/xml",
            "zip" => "application/zip",
            _ => "application/octet-stream",
        };
    }

    static readonly DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long UnixTime(DateTime d) => (long)(d - epoch).TotalMilliseconds;
}
