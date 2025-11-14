using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DartsAPI.Data;
using DartsAPI.Dtos;
using DartsAPI.Models;
using DartsAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace DartsAPI.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this WebApplication app){
        var group = app.MapGroup("players");

        //GET /players
        group.MapGet("/", (PlayerContext dbContext) => {
            return dbContext.Players.Select(p => new { p.Id, p.Username });
        }).RequireAuthorization();

        // POST /players/register - Register a new player
        group.MapPost("/register", async (LoginRegisterDto registerDto, PlayerContext dbContext) =>
        {   
            var existingUser = await dbContext.Players
                .FirstOrDefaultAsync(p => p.Username == registerDto.Username);

            if (existingUser != null)
            {
                return Results.BadRequest("Username already taken.");
            }

            var newUser = new Player
            {
                Username = registerDto.Username,
                Password = registerDto.Password
            };

            dbContext.Players.Add(newUser);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { Message = "Registration successful", User = newUser });
        });

        // POST /players/login
        group.MapPost("/login", async (LoginRegisterDto loginDto, PlayerContext dbContext, IConfiguration config) =>
        {

            var user = await dbContext.Players
                .FirstOrDefaultAsync(p => p.Username == loginDto.Username);
            if (user == null || !user.Password.Equals(loginDto.Password))
            {
                return Results.Unauthorized();
            }

            var token = JwtTokenService.GenerateJwtToken(user, config);
            return Results.Ok(new { Message = "Login successful", Token = token, User = new { user.Id, user.Username } });
        });


        return group;
    }
}
