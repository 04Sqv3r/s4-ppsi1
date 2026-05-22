using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Połączenie z bazą danych MySQL


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "server=meow_db;port=3306;database=wypozyczalnia_ksiazek;user=root;password=rootpassword;";
builder.Services.AddDbContext<meow.Models.LibraryDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)), 
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));
// 2. Rejestracja usług systemowych
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<meow.Models.LibraryDbContext>();
    
    // Upewnij się, że baza jest stworzona i ma tabele
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
    else
    {
        context.Database.EnsureCreated();
    }
    Console.WriteLine("🐾 Baza danych gotowa.");
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();