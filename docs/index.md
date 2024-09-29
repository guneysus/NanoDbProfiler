

# NanoDbProfiler for EF Core 8.x


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
