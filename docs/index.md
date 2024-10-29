

# NanoDbProfiler for EF Core 8.x

<a href='https://github.com/guneysus/NanoDbProfiler' style='position:fixed;padding:5px 45px;width:128px;top:50px;left:-50px;-webkit-transform:rotate(315deg);-moz-transform:rotate(315deg);-ms-transform:rotate(315deg);transform:rotate(315deg);box-shadow:0 0 0 3px #080707;text-shadow:0 0 0 #555555;background-color:#080707;color:#555555;font-size:13px;font-family:sans-serif;text-decoration:none;font-weight:bold;border:2px dotted #555555;-webkit-backface-visibility:hidden;letter-spacing:.5px;'>Fork me on GitHub</a>


## Install

```
dotnet add package NanoDbProfiler.AspNetCore --version 0.1.23-alpha-g1a0a4554d2 --source https://www.myget.org/F/guneysu/api/v3/index.json 
```

```csharp
// Configure service
services.AddNanoDbProfiler();


// Configure App
app.UseNanodbProfilerToolbar();
```


Toolbar will inject itself any html page.

![screenshot](./img/chrome_qKlLJE0ANE.png)
