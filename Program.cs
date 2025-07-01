using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MonitorApi.Data;
using MonitorApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<MonitorDbContext>(options =>
    options.UseInMemoryDatabase("MonitorDb"));
builder.Services.AddScoped<DataForwardingService>();
builder.Services.AddControllers();
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();

// Configure forwarding options
builder.Services.Configure<ForwardingOptions>(
    builder.Configuration.GetSection("Forwarding"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();
app.Run();