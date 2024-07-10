using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using X_LabDataBase.Context;
using X_LabDataBase.Entityes;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureMiddleware(app);
app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<DataBaseContext>(options =>
    {
        options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "openiddict-velusia-client.sqlite3")}");
        options.UseOpenIddict();
    });

    services.AddIdentity<Person, IdentityRole>()
        .AddEntityFrameworkStores<DataBaseContext>()
        .AddDefaultTokenProviders();

    ConfigureOpenIddict(services);

    services.AddControllers().
        AddOData(
        options => options.SetMaxTop(10).Count().Filter().OrderBy().Expand().Select()
        .AddRouteComponents(
            routePrefix: "resource",
            model: GetEdmModel()));
    services.AddEndpointsApiExplorer();

    ConfigureSwagger(services);

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7168/";
        options.Audience = "resource_server_1";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://localhost:7168/",
            ValidateAudience = true,
            ValidAudience = "resource_server_1",
            ValidateLifetime = true
        };
    });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
        });
    });

    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });
}
IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    builder.EntitySet<Person>("Persons");
    return builder.GetEdmModel();
}

void ConfigureOpenIddict(IServiceCollection services)
{
    services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                   .UseDbContext<DataBaseContext>();
        })
        .AddServer(options =>
        {
            options.SetTokenEndpointUris("/connect/token");
            options.AllowPasswordFlow();
            options.AllowRefreshTokenFlow();
            options.UseAspNetCore()
                   .EnableTokenEndpointPassthrough();
            options.AddDevelopmentSigningCertificate();
            options.RegisterScopes("offline_access");
            options.AddEncryptionKey(new SymmetricSecurityKey(
                Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));
            options.SetAccessTokenLifetime(TimeSpan.FromSeconds(30));
            options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
        })
        .AddValidation(options =>
        {
            options.SetIssuer("https://localhost:7168/");
            options.AddAudiences("resource_server_1");
            options.AddEncryptionKey(new SymmetricSecurityKey(
                Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));
            options.UseSystemNetHttp();
            options.UseAspNetCore();
        });
}

void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri("https://localhost:7168/connect/Authorize"),
                    TokenUrl = new Uri("https://localhost:7168/connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "api1", "resource server scope" },
                        { "offline_access", "offline_access" }
                    }
                }
            }
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                },
                Array.Empty<string>()
            }
        });
    });
}

void ConfigureMiddleware(WebApplication app)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.OAuthClientId("web-client");
    });

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}
