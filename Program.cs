using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// ADD SERVICES TO THE CONTAINER
// ==========================================

builder.Services.AddControllersWithViews();

// ==========================================
// DATABASE CONNECTION
// ==========================================

builder.Services.AddDbContext<VolunteerHubContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// SESSION
// ==========================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// CONFIGURE HTTP REQUEST PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ==========================================
// HTTPS
// ==========================================

app.UseHttpsRedirection();

// ==========================================
// STATIC FILES
// ==========================================

app.UseStaticFiles();

// ==========================================
// ROUTING
// ==========================================

app.UseRouting();

// ==========================================
// SESSION
// ==========================================

app.UseSession();

// ==========================================
// AUTHORIZATION
// ==========================================

app.UseAuthorization();

// ==========================================
// DEFAULT ROUTE
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();