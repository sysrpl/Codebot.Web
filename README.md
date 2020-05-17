# Codebot.Web

A simple framework to create websites usign ASP.NET core. To create a new website simply write this code:

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

In a terminal type:

```
mkdir wwwroot
echo "Test.Hello, Test" > wwwroot/home.ashx
echo "Hello World" > wwwroot/home.html
dotnet add reference ../Codebot.Web/Codebot.Web.csproj
dotnet run --urls=http://0.0.0.0:5000/
```
