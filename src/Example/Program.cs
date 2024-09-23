using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Example
{
    public class Program
    {
        public static void Main(string [] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services
                .AddDbContext<TodoContext>(o => o.UseSqlite("Data Source=db.sqlite"))
                .AddNanoDbProfiler();

            var app = builder.Build();

            app.MapGet("/insert", (HttpContext h, [FromServices] TodoContext db) =>
            {
                var e = new Todo { Title = Guid.NewGuid().ToString() };
                db.Todos.Add(e);
                db.SaveChanges();
                return Results.Ok(e.Title);
            });

            app.MapGet("/select/scalar", (HttpContext h, [FromServices] TodoContext db) => Results.Ok(db.Todos.Select(x => x.Id)));

            app.MapGet("/select/single/1", (HttpContext h, [FromServices] TodoContext db) => Results.Ok(db.Todos.FirstOrDefault()));

            app.MapGet("/select/single/2", (HttpContext h, [FromServices] TodoContext db) => Results.Ok(db.Todos.Skip(1).FirstOrDefault()));

            app.MapGet("/select/all", (HttpContext h, [FromServices] TodoContext db) => Results.Ok(db.Todos));

            app.MapGet("/update", (HttpContext h, [FromServices] TodoContext db) =>
            {
                var e = db.Todos.First();
                e.Title += "*";
                db.Todos.Update(e);
                db.SaveChanges();
                return Results.Ok(e.Title);
            });

            app.MapGet("/delete/single", (HttpContext h, [FromServices] TodoContext db) =>
            {
                var e = db.Todos.First();
                db.Remove(e);
                db.SaveChanges();
                return Results.Ok();
            });

            app.MapGet("/delete/all", async (HttpContext h, [FromServices] TodoContext db) =>
            {
                await db.Todos.ExecuteDeleteAsync();
                return Results.Ok();
            });

            app.MapGet("/", (HttpContext h) => Results.Text("Hello World!", "text/html"));

            app.Services.CreateScope().ServiceProvider.GetRequiredService<TodoContext>().Database.EnsureCreated();

            app.UseNanodbProfilerToolbar();

            app.Run();
        }
    }
}

public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos { get; set; }
}


public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; }
};