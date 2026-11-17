using Krypton.Web.Infrastructure;
using Krypton.Web.Services.Implementations;
using Krypton.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;

if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await CryptoSelfTestRunner.RunAllTestsAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Configuração de limites de upload para arquivos de até 500 MB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524_288_000; // 500 MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524_288_000; // 500 MB
});

// Registro dos serviços de criptografia (AES-256-GCM + PBKDF2-HMAC-SHA512)
builder.Services.AddSingleton<IKeyDerivationService, KeyDerivationService>();
builder.Services.AddSingleton<ICryptoService, CryptoService>();

// ASP.NET Core MVC (.NET 10)
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
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
