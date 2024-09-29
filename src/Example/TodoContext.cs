using Microsoft.EntityFrameworkCore;

public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<Todo>      Todos { get; set; }
}
