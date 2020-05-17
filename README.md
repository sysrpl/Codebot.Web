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
echo "Hello World!" > wwwroot/home.html
dotnet add reference ../Codebot.Web/Codebot.Web.csproj
dotnet run --urls=http://0.0.0.0:5000/
```

## Directory and File Structure Explained

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

In this arrangement ``Codebot.Web folder`` is a sibling of the ``Test`` folder. The ``Codebot.Web`` folder contains a copy of this git repository and the ``Test`` folder contains your website project.

The ``Test/wwwroot`` folder contains the content of your website including any static files and subfolder you want to serve. Whenever a client web browser requests a page or resource the web server will look for those resources starting in the ``wwwroot`` folder.

Whenever a request is made to a folder this framework will look for a file named ``home.ashx`` and read its contents. The contents of ``home.ashx`` with contain the name of the class used to handle the incomming request. In our case, the name of the class is ``Test.Hello, Test``, where ``Test.Hello`` is the namespace qualified name of the class, and ``, Test`` reference the the assembly name where the class is located.

## Serving Pages from Your Class

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
            Write("Hello World!");
        }
        
        public static void Main(string[] args)
        {
            Website.Run(args);
        }
    }
}
```

This would result in the same response content being sent back to the client, but the ``Hello World`` would be output from your code rather than a file.

## Using Templates

Instead of using your ``DefaultPage`` to serve a static file, it can be used as a template to fill out a response based on properties of your page. To do this simply add ``IsTemplate = true`` to the ``DefaultPage`` attribute decoration. Next put the property name, multiple names for a more complete template, in your default page file.

```csharp
    [DefaultPage("home.html", IsTemplate = true)]
    public class Hello : PageHandler
```

And in ``home.html``:

```html
<html>
  <body>The title of this page is {Title}</body>
</html>
```

The template enging will see curly braces ``{ }`` with an identifier in it and substitute it with a property of your object. In the example above ``The title of this page is {Title}`` would be substituted with ``The title of this page is Blank`` because the base class of PageHandler defines ``Title`` like so: 

```csharp
    public virtual string Title { get => "Blank"; }
```

To alter the title in you class you could add this inside of your ``Hello`` class:

```csharp
    public override string Title { get => "My Home Page"; }
```

This would result in the response ``The title of this page is My Home Page`` being generated.

Properties templated by curly braces ``{ }`` can be of any type and are not required to be be strings. If you have a ``User`` class with properties like Name, Birthday, and Role, it could be templated like so:

```html
<html>
  <body>
	<h1>Welcome {CurrentUser.Name} of the {CurrentUser.Role} group!</h1>
	<p>Your birthday is on {CurrentUser.Birthday}!</p>
  </body>
</html>
```

Your class would then need to have a property named CurrentUser:

```csharp
    public User CurrentUser { get; private set; }
```

### Formatting Templates

In addition to inserting tempaltes into your pages, you can also use format specifiers to control how those properties are formatted. For example if you have a property called ``DontatedAmount`` of type ``double`` it could be formatted like so:

```html
	<p>We've received a total of {DonatedAmount:C2}!</p>
```

If DonatedAmount was ``10157.5`` then the response would include ``We've received a total of $10,157.50!``

## Responding to Web Methods

In addition to using this framework to generate templated responses, it can also be used to respond to web method requests. To create a new web method request simply define a method and adorn it with the ``MethodPage`` attribute:

```csharp
    [MethodPage("hello")]
    public void HelloMethod() { Write("Hello World!"); }
```

If the client then submits a request with a method named ``hello`` it will receive back ``Hello World!``. Here is what a request to our method would look:

```console
	http://example.com/?method=hello
```
## Processing Web Methods Arguments
