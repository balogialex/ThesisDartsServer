using DartsAPI.Data;
using Microsoft.IdentityModel.Tokens;
using DartsAPI.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using DartsAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("Player");
builder.Services.AddSqlite<PlayerContext>(connString);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });;

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://10.0.2.2:5016",
                "http://192.168.1.6:5016",
                "http://localhost:7076"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAll");

app.MapHub<LobbyHub>("/lobbyHub");
app.MapHub<GameHub>("/gameHub"); 

app.MapPlayersEndpoints();
app.MapLobbyEndpoints();
app.MapGameEndpoints();

app.MigrateDb();

app.Run();
