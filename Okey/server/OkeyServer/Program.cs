using Microsoft.EntityFrameworkCore;
using OkeyServer.Data;
using OkeyServer.Hubs;
using OkeyServer.Misc;
using OkeyServer.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ServerDbContext>(options =>
{
    options.UseMySQL(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException()
    );
});

builder.Services.AddSignalR();

builder.Services.AddScoped<ServerDbContext>();

builder.Services.AddScoped<JWTCheck>();

builder.Services.AddSingleton<IRoomManager, RoomManager>();

var app = builder.Build();

app.MapHub<OkeyHub>("OkeyHub");

app.Run();
