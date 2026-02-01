using CommunityApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace CommunityApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/token", (HttpContext ctx, IConfiguration config) =>
        {
            var expectedSecret = config["AuthTokens:ClientSecret"]; 
            if (!ctx.Request.Headers.TryGetValue("X-Auth-Secret", out var provided) ||
                !string.Equals(provided, expectedSecret, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            };

            var name  = ctx.Request.Query["name"].ToString();
            var group = ctx.Request.Query["group"].ToString();
            var perm = ctx.Request.Query["perms"].ToString();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, name),
                new Claim("side", group),
                new Claim("scope", perm)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return Results.Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        });
        
        return app;
    }  
}