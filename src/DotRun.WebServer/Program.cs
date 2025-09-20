using Microsoft.AspNetCore.Mvc.ApplicationModels;
using DotRun.WebServer;
using DotRun.Database.Extensions;
using DotRun.WebServer.Services;
using DotRun.Runtime.Services.V2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDotRunDatabase();

// Add DotRun configuration service
builder.Services.AddSingleton<IDotRunConfigService, DotRunConfigService>();

// Ensure IWorkflowResolver is registered (if not already in AddDotRunDatabase)
// Note: AddDotRunDatabase already registers IWorkflowResolver as Scoped
// builder.Services.AddScoped<IWorkflowResolver, WorkflowResolver>();

builder.Services
    .AddControllersWithViews(options => { options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseParameterTransformer())); })
    .AddNewtonsoftJson()
    .AddRazorRuntimeCompilation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();