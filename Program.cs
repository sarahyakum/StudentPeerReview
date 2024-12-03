/*
    Created by Kiara Vaz for CS 4485.0W1, Senior Design Project, Started October 20, 2024.
    Net ID: kmv200000

    Updated by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file serves as the application's backbone, setting up necessary services, middleware, and routing for the application to function correctly.
    Key Functions:
        1. Configures Razor Pages for UI rendering
        2. Sets up the database context using Entity Framework and MySQL 
        3. Enables session management for storing user-specific data 
        4. Configures routing, static file handling, HTTP security settings, etc.
        5. Defines default route as the Login Page
*/


using Microsoft.EntityFrameworkCore;
using StudentPeerReview.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the containers
builder.Services.AddRazorPages();

// DARYA UPDATE: Added ApplicationDbContext and configured it to use MySQL 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 39)) // Adjust to match your MySQL version
    ));

builder.Services.AddSession();
var app = builder.Build();

app.UseSession();

// Configure the HTTP request pipeline
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
