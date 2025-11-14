using System;
using DartsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DartsAPI.Data;

public class PlayerContext(DbContextOptions<PlayerContext> options) 
    : DbContext(options)
{
    public DbSet<Player> Players =>Set<Player>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>().HasData(
            new{Id = 1, Username = "Tesz1", Password = "asd"},
            new{Id = 2, Username = "Tesz2", Password = "asd"},
            new{Id = 3, Username = "Teszt3", Password = "asd"},
            new{Id = 4, Username = "Teszt4", Password = "asd"}
        );
    }

}
