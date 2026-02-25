using BackendProfessional.Application.Services;
using BackendProfessional.Infrastructure.Identity;
using BackendProfessional.Infrastructure.Persistence;
using BackendProfessional.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// JWT config (lee Jwt:Key desde appsettings)
var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("Jwt:Key missing"));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });


// * Servicios
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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