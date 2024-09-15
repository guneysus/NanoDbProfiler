using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example
{
    public class Program
    {
        public static void Main (string[] args) {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services
                .AddDbContext<TodoContext>(o => o.UseSqlite("Data Source=db.sqlite"))
                .AddNanoDbProfiler();

            var app = builder.Build();

            app.MapGet("/", (HttpContext h, [FromServices] TodoContext db) => {
                var e = new Todo{ Title = Guid.NewGuid().ToString() };
                db.Todos.Add(e);
                db.SaveChanges();
                e.Title = Guid.NewGuid().ToString();
                db.SaveChanges();
                db.Remove(e);
                db.SaveChanges();

                var todos = db.Todos.Take(100).ToList();
                return Results.Text("Hello World!", "text/html");
            });

            app.Services.CreateScope().ServiceProvider.GetRequiredService<TodoContext>().Database.EnsureCreated();
            app.UseNanodbProfilerToolbar();


            app.Run();
        }
    }
}

public class TodoContext (DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos { get; set; }
}


public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; }
};