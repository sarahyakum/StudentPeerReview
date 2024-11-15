using Microsoft.EntityFrameworkCore;
using StudentPeerReview.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add ApplicationDbContext and configure it to use MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 25)) // Adjust to match your MySQL version
    ));

builder.Services.AddSession();
var app = builder.Build();

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// Set the default route to Login.cshtml
app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;  // Return a completed task to satisfy the method signature
});

app.Run();
