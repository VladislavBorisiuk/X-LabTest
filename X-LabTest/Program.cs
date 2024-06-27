using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using X_LabDataBase.Context;
using X_LabDataBase.Entityes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "openiddict-velusia-client.sqlite3")}");
    options.UseOpenIddict();
});


   builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://localhost:7168/");
        options.AddAudiences("resource_server_1");

        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        options.UseSystemNetHttp();

        options.UseAspNetCore();
    });

builder.Services.AddIdentity<Person, IdentityRole>()
        .AddEntityFrameworkStores<DataBaseContext>()
        .AddDefaultTokenProviders();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://localhost:7168/connect/authorize"),
                TokenUrl = new Uri("https://localhost:7168/connect/token"),
                Scopes = new Dictionary<string, string>
            {
                { "api1", "resource server scope" }
            }
            },
        }
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id="pauth2"}
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.OAuthClientId("web-client");
    c.OAuthClientSecret("901564A5-E7FE-42CB-B18D-61EF6A8F3654");
});


app.UseDeveloperExceptionPage();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(options =>
{
    options.MapControllers();
    options.MapDefaultControllerRoute();
});
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
