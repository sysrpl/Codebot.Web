namespace Codebot.Web;

using System;

public class WebCustomException : Exception
{
    private const string problem = "There was an error processing your request";

    public WebCustomException() : base(problem)
    {
    }

    public WebCustomException(string message) : base(message)
    {
    }

    public WebCustomException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public Exception Inner { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Reason { get; set; }
    public string Remedy { get; set; }
}
