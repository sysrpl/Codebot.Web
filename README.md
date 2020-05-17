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

The ``Test/wwwroot`` folder contains the content of your website including any static files and subfolders might want to serve. When a client web browser requests a resource the web server will search for them begining in the ``wwwroot`` folder.

If a request is made to a folder the framework will search for a special file named ``home.ashx`` and read its contents. The contents of ``home.ashx`` should contain the name of the class used to handle the incomming request by this framework. In our case, the name of the class is ``Test.Hello, Test``, where ``Test.Hello`` is the namespace qualified name of the class, and ``, Test`` reference the the assembly name where the class is located. This class should derived from BasicHandler or one of its descendants. An instance of that class type will be created by the framework and invoked passing it the current ``HttpContext``.

Using this design of folders containing a ``home.ashx`` file you can easily design a website with one or more varying page handler types.

## Serving Pages from Your Class

A simple way to serve a web page from your handler class is to decorate it with ``DefaultPage`` attribute. This will cause the handler to look for a file resource starting in the ``wwwroot`` folder matching the filename adorned to your attribute. In the code example at the top of this document that file resource is ``home.html``.

```csharp
    [DefaultPage("home.html")]
    public class Hello : PageHandler
```

It should be noted that the ``DefaultPage`` attribute is completely optional. If you wanted to generate the page yourself through code. You could override the ``EmptyPage`` method and write a response manually using the following:

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

This would result in the same response content being sent back to the client, but the ``Hello World!`` would be output from your code rather than from a file resource.

## Using Templates

Instead of using your ``DefaultPage`` to serve a static file, it might be useful to user as a template. A template fills out a response partially based on properties of your handler object. To use a template  simply add ``IsTemplate = true`` to the ``DefaultPage`` attribute decoration. Next put the property name or multiple names in your default page file to have it act as a template.

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

Different handlers can use the same tempalte resulting in different response results. Additionally, properties templated by curly braces ``{ }`` can be of any type and are not required to be be strings. For example if you had a ``User`` class with properties like Name, Birthday, and Role, it could be templated in your file resource like so:

```html
<html>
  <body>
	<h1>Welcome {CurrentUser.Name} of the {CurrentUser.Role} group!</h1>
	<p>Your birthday is on {CurrentUser.Birthday}!</p>
  </body>
</html>
```

To make this work your handler would need to have a property named CurrentUser:

```csharp
    public User CurrentUser { get; private set; }
```

### Formatting Templates

In addition to inserting tempaltes into your pages, you can also use format specifiers to control how those properties are formatted. For example if you have a property on your handler called ``DontatedAmount`` of type ``double`` it could be formatted like so:

```html
	<p>We've received a total of {DonatedAmount:C2}!</p>
```

And if DonatedAmount was ``10157.5`` then the response would include ``We've received a total of $10,157.50!``

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
### Processing Web Methods Arguments

In the web method example above we simply returned some static text. A dynamic result can be produced using arguments from a ``GET`` or ``POST`` request, which typically might orginate from a ``<form>`` element on your page. To use those arguments you can use any number of ``Read`` methods. Here is an example:

```csharp
    [MethodPage("purchase")]
    public void PurchaseMethod()
    {
      string userId = ReadInt("userid");
      string product = ReadString("item");
      int quantity = ReadInt("qty");
      DateTime deliverOn = Read<DateTime>("deliveryDate"); 
      var json = SumbitOrder(product, quantity, deliverOn);
      ContentType = "text/json";
      Write(json);
    }
```

Note the various ``Read`` methods at your disposal. Also note that a response in generated in json format using whatever backend technology you desire. In the example above ``SumbitOrder`` supposably does some work and returns json text. However you want to take action on a web method request is up to you. This framework just provides a simple way to accept those requests.

The invoker of our ``PurchaseMethod`` might come from a web page using a form element like so:

```html
	<form action="?method=purchase" method="POST">
	  <input type="text" name="userid">
	  <input type="text" name="item">
	  <input type="text" name="qty">
	  <input type="text" name="deliveryDate">
	  <input type="submit">
	</form>
```

If you wanted to invoke our purchase method example without using a ``<form>`` element but through JavaScript instead you might write the following:

```javascript
    let data = new FormData();
    data.append("userid", 1);
    data.append("item", "bananas");
    data.append("qty", "12");
    data.append("deliveryDate", "1/15/2020");
    let request = new XMLHttpRequest();
    request.open("POST", "?method=purchase");
    request.send(data);
````
## Other Examples

Here are a few other examples of tasks which cna be accomplished using this framework.

Sending a file to the client based on some criteria:

```csharp
    [MethodPage("download")]
    public void DownloadMethod()
    {
        string fileName = MapPath("../private/" + Read("filename"));
        if (FileExists(fileName) && UserAuthorized)
          SendAttachment(fileName);
        else
          Redirect("/unauthorized");
    }
```

Serving different templates based on some state of your website:

```csharp
    public override void EmptyPage()
    {
        if (StoreIsOpened)
          // If we are opened include the storefront and format it as a template
          Include("/templates/storefront.html", true);
        else          
          // Otherwise send the static we're closed page
          Include("/templates/wereclosed.html");
    }
```

Handling json data assuming the entire request body is a json object:

```csharp
    [MethodPage("search")]
    public void SearchMethod()
    {
        var criteria = JsonSerializer.Deserialize<SearchCriteria>(ReadBody());
        var results = PerformSearch(criteria);
        ContentType = 'text/json';
        Write(JsonSerializer.Serialize(results));
    }
```

