using System.Runtime.InteropServices;
// using MySqlConnector;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
using System.Text;

using CommunityApi.Endpoints;
using CommunityApi.Services;

var builder = WebApplication.CreateBuilder(args);

// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// var token = builder.Configuration["AuthTokens:MasterToken"];

builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true, 

      ValidIssuer = builder.Configuration["Jwt:Issuer"],
      ValidAudience = builder.Configuration["Jwt:Audience"],

      IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
      )
    };
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGangService, GangService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPlayerEndpoints();
app.MapAuthEndpoints();
app.MapGangEndpoints();

app.Run();