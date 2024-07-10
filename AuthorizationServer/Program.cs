using AuthorizationServer;
using AuthorizationServer.Services.Interfaces;
using AuthorizationServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using X_LabDataBase.Context;
using X_LabDataBase.Entityes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.ConfigureDatabase();
builder.Services.ConfigureIdentity();
builder.Services.ConfigureOpenIddict();
builder.Services.ConfigureCors();
builder.Services.ConfigureGrantTypeHandlers();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

await InitializeData(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();

static async Task InitializeData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<ClientSeeder>();
    await seeder.AddClients();
    await seeder.AddScopes();
}

public static class ServiceExtensions
{
    public static void ConfigureDatabase(this IServiceCollection services)
    {
        services.AddDbContext<DataBaseContext>(options =>
        {
            options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "openiddict-velusia-client.sqlite3")}");
            options.UseOpenIddict();
        });
    }

    public static void ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<Person, IdentityRole>()
            .AddEntityFrameworkStores<DataBaseContext>()
            .AddDefaultTokenProviders();
    }

    public static void ConfigureOpenIddict(this IServiceCollection services)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<DataBaseContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetLogoutEndpointUris("/connect/logout")
                       .SetTokenEndpointUris("/connect/token");

                options.AllowAuthorizationCodeFlow()
                       .AllowClientCredentialsFlow()
                       .AllowRefreshTokenFlow()
                       .AllowPasswordFlow();

                options.AddEncryptionKey(new SymmetricSecurityKey(
                    Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

                options.DisableAccessTokenEncryption();

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableLogoutEndpointPassthrough()
                       .EnableTokenEndpointPassthrough();

                options.SetAccessTokenLifetime(TimeSpan.FromSeconds(30));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });
    }

    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }

    public static void ConfigureGrantTypeHandlers(this IServiceCollection services)
    {
        services.AddTransient<ClientSeeder>();
        services.AddScoped<PasswordGrantTypeHandler>();
        services.AddScoped<RefreshGrantTypeHandler>();
        services.AddScoped<IGrantTypeHandlerFactory, GrantTypeHandlerFactory>();
    }
}
