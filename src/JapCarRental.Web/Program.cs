using JapCarRental.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JapCarRental.Web.Services;
using JapCarRental.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Every POST/PUT/DELETE must carry a valid anti-forgery token, even if an action
    // forgets the attribute. Safe methods (GET) are not checked.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. See the README, section Configuration.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Injectable clock so services (and tests) do not depend on DateTime.Today directly.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<IContractService, ContractService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();

// Apply migrations and seed demo data (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Turns empty 404/400 responses (for example NotFound() in a controller) into the same friendly page.
// Re-executing keeps the original status code, so clients and tests still see a real 404.
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();