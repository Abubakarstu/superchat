using API;
using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var baileysRegistered = false;
app.Use(async (ctx, next) =>
{
    if (!baileysRegistered && ctx.Request.Host.HasValue)
    {
        baileysRegistered = true;
        var apiUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        var baileysUrl = builder.Configuration.GetSection("BaileysService")["BaseUrl"] ?? "http://localhost:3001";
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                await client.PostAsJsonAsync($"{baileysUrl}/register", new { apiUrl });
            }
            catch { }
        });
    }
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHub<MessageHub>("/hubs/messages");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
