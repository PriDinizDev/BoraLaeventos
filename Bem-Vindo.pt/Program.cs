// Adicionado para podermos usar o try-catch no topo
using System;
using Bem_vindo.pt.Models;
using Bem_vindo.pt.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication; // Adicionado para IAuthenticationSchemeProvider
using Microsoft.AspNetCore.Authentication.Google; // Necessário para o AddGoogle
using Microsoft.Extensions.DependencyInjection;
using Bem_Vindo.pt.Models; // Necessário para IServiceCollection

// ===== O TRY COMEÇA AQUI, A ENVOLVER TODO O ARRANQUE =====
try
{
    Console.WriteLine("A iniciar a configuração...");

    var builder = WebApplication.CreateBuilder(args);

    // --- Configuração dos Serviços ---
    Console.WriteLine("A configurar serviços...");
    builder.Services.AddControllersWithViews();

    Console.WriteLine("A configurar DbContext...");
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("ERRO FATAL: Connection string 'DefaultConnection' não foi encontrada no ficheiro appsettings.json.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    Console.WriteLine("A configurar PasswordHasher...");
    builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

    Console.WriteLine("A configurar EmailSender...");
    // Apenas o SendGridEmailSender deve ser usado para produção, mas ConsoleEmailSender para debug
    builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();

    // INÍCIO DO NOVO BLOCO DE AUTENTICAÇÃO
    Console.WriteLine("A configurar Autenticação...");
    builder.Services.AddAuthentication(options =>
    {
        // Define o esquema padrão
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        // Configuração da Cookie Authentication
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    // ADICIONAR O GOOGLE AUTHENTICATION AQUI
    .AddGoogle(options =>
    {
        // As chaves serão lidas do ficheiro secrets.json
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        // BÓNUS: Se quiser aceder a mais informações do perfil, adicione scopes
        // options.Scope.Add("profile"); 
    });
    // FIM DO NOVO BLOCO DE AUTENTICAÇÃO


    Console.WriteLine("Serviços configurados. A construir a aplicação...");
    var app = builder.Build();

    // --- Configuração do Pipeline ---
    Console.WriteLine("A configurar o pipeline...");

    // Configuração Rotativa (se tiver os binários)
    var env = app.Services.GetService<IWebHostEnvironment>();
    if (env == null) throw new InvalidOperationException("IWebHostEnvironment não foi registado.");
    Rotativa.AspNetCore.RotativaConfiguration.Setup(env.WebRootPath, "Rotativa");

    // Páginas de erro customizadas (desenvolvimento E produção)
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

    if (!app.Environment.IsDevelopment())
    {
        // app.UseHsts();
    }

    // Comentar HttpsRedirection para forçar HTTP em localhost, evitando erros SSL
    // app.UseHttpsRedirection(); 

    app.UseStaticFiles();
    app.UseRouting();

    // ISTO É CRUCIAL: Deve ser chamado ANTES de UseAuthorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    Console.WriteLine("Pipeline configurado. A iniciar a aplicação...");
    app.Run(); // Esta linha só será atingida se tudo correr bem até aqui

}
catch (Exception ex)
{
    // SE HOUVER UM ERRO DURANTE A CONFIGURAÇÃO OU ARRANQUE, ELE SERÁ APANHADO E EXIBIDO AQUI
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n!!!!!!!!!! ERRO FATAL AO CONFIGURAR OU INICIAR A APLICAÇÃO !!!!!!!!!!\n");
    Console.WriteLine("Verifique as configurações (appsettings.json, Program.cs) e as dependências.");
    Console.WriteLine("\n--- DETALHES DO ERRO ---");
    Console.WriteLine(ex.ToString()); // Mostra o erro completo e a stack trace
    Console.ResetColor();
}
finally
{
    // Garante que a consola não fecha imediatamente se houver um erro
    // Console.WriteLine("\nPressione qualquer tecla para fechar a consola...");
    // Console.ReadKey();
}
// ===== FIM DO BLOCO TRY-CATCH =====