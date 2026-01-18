using NirUsb.Infrastructure.DataAccess;
using Scalar.AspNetCore;

namespace NirUsb.Api;

public static class Program {
    public static async Task Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<AppDbContext>();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment()) {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}