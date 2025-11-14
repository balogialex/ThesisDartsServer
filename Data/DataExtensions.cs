using System;
using Microsoft.EntityFrameworkCore;

namespace DartsAPI.Data;

public static class DataExtensions
{
    public static IApplicationBuilder MigrateDb(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlayerContext>();
        db.Database.Migrate();
        return app;
    }
}
