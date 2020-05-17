# Codebot.Web

A simple framework to create websites usign ASP.NET core. To create a new website simply write this code:

```csharp
using Codebot.Web;

namespace Test
{
    [DefaultPage("home.html")]
    public class Hello
    {
        public static void Main(string[] args)
        {
            Website.Run(args);
        }
    }
}
```
