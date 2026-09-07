using AB1.Components;
using AB1.Data;
using AB1.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("Default") ??
                       throw new InvalidOperationException("Задайте ConnectionStrings:Default и запустите приложение.");

await DatabaseInitializer.EnsureCreatedAsync(connectionString);
var serverVersion = ServerVersion.AutoDetect(connectionString);

builder.Services
    .AddDbContextFactory<AppDbContext>(options => options.UseMariaDb(connectionString, serverVersion))
    .AddScoped<UrlShortener>();

var app = builder.Build();

// Чтобы для запуска хватило строки подключения: создаём БД и накатываем схему сами.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// 301 браузер закэширует: счётчик перестанет расти, а правка длинного URL перестанет применяться.
app.MapGet("/s/{*hash}", async (string hash, UrlShortener shortener) =>
{
    var originalUrl = await shortener.ResolveAndCountAsync(hash);
    return originalUrl is null
        ? Results.NotFound()
        : Results.Redirect(originalUrl);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();