# Codebot.Web

A simple framework to create websites using ASP.NET core. To create a new website simply write this code:

In Test.csproj place:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>
```
In Hello.cs place:

```csharp
using Codebot.Web;

namespace Test
{
    [DefaultPage("home.html")]
    public class Hello : PageHandler
    {
        public static void Main(string[] args)
        {
            Website.Run(args);
        }
    }
}
```

In a console type:

```console
mkdir wwwroot
echo "Test.Hello, Test" > wwwroot/home.ashx
echo "Hello World" > wwwroot/home.html
dotnet add reference ../Codebot.Web/Codebot.Web.csproj
dotnet run --urls=http://0.0.0.0:5000/
```

# Directory and File Structure Explained

Using the simple example above you would have the following directory and file structure:

```console
+- Codebot.Web
|  |
|  + Codebot.Web.csproj
|
+- Test
   |
   +- Test.csproj 
   |
   +- Hello.cs 
   |
   +- wwwroot
      |
      +- home.ashx
      |
      +- home.html
```

In this arrangement ``Codebot.Web folder`` is a sibling of the ``Test`` folder. The ``Codebot.Web`` folder contains a copy of git repository and the ``Test`` folder contains your website project.

The ``Test/wwwroot`` folder contains the content of your website including any static files and subfolder you want to serve. Whenever a client web browser requests a page or resource the web server will look for those resources starting in the ``wwwroot`` folder.

Whenever a request is made to a folder this framework will look for a file named ``home.ashx`` and read its contents. The contents of ``home.ashx`` with contain the name of the class used to handle the incomming request. In our case, the name of the class is ``Test.Hello, Test``, where ``Test.Hello`` is the namespace qualified name of the class, and ``, Test`` reference the the assembly name where the class is located.

# Serving Pages from Your Class

A simple way to serve a web page from your class is to decorate it with ``DefaultPage`` attribute. This will cause the class to look for a file resource starting in the ``wwwroot`` folder that matches your name. In the code example at the top of this document that file resource is ``home.html``.

```csharp
    [DefaultPage("home.html")]
    public class Hello : PageHandler
```

The ``DefaultPage`` attribute is completely optional. If you wanted to generate the page yourself through code you could write the following:

```csharp
using Codebot.Web;

namespace Test
{
    public class Hello : PageHandler
    {
        protected override void EmptyPage()
        {
            Write("Hello World");
        }
        
        public static void Main(string[] args)
        {
            Website.Run(args);
        }
    }
}
```

This would result in the same response content being sent back to the client, but the ``Hello World`` would be output from your code rather than a file.
