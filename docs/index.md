

# NanoDbProfiler for EF Core 8.x


## Install

```
dotnet add package NanoDbProfiler.AspNetCore --version 0.1.11-alpha-g61f85a4952 --source https://www.myget.org/F/guneysu/api/v3/index.json 
```

```csharp
// Configure service
services.AddNanoDbProfiler();


// Configure App
app.UseNanodbProfilerToolbar();
```


Toolbar will inject itself any html page.

![screenshot](./img/chrome_qKlLJE0ANE.png)
