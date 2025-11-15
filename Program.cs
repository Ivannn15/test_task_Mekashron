using Microsoft.Extensions.Options;
using test_task_Mekashron.Options;
using test_task_Mekashron.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<SoapServiceOptions>(builder.Configuration.GetSection(SoapServiceOptions.SectionName));

builder.Services.AddHttpClient<ISoapAuthService, SoapAuthService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<SoapServiceOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
