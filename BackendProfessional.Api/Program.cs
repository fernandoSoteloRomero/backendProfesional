using BackendProfessional.Infrastructure.Identity;
using BackendProfessional.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Servicios mínimos
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO: cuando añadamos DbContext/Identity/Mapster, los registraremos aquí.

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => { options.User.RequireUniqueEmail = true; })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// * Seeder para usuario admin y normal
using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;

    try
    {
        var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@local.test";
        var adminPassword = builder.Configuration["Seed:AdminPassword"] ?? "P@ssw0rd123!";
        await IdentityDataSeeder
            .SeedAsync(sp, adminEmail, adminPassword);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error ejecutando el seeder de Identity.");
        // no lanzamos para que app intente continuar; revisa logs si falla.
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANTE: cuando registremos autenticación:
// app.UseAuthentication();  // antes de UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();