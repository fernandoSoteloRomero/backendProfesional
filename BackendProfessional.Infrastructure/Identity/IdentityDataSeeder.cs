using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProfessional.Infrastructure.Identity;

public class IdentityDataSeeder
{
  public static async Task SeedAsync(IServiceProvider services,
    string adminEmail = "admin@local.test",
    string adminPassword = "P@ssw0rd123!")
  {
    using var scope = services.CreateScope();
    var sp = scope.ServiceProvider;

    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

    const string adminRoleName = "Admin";

    // 1) Crear rol Admin si no existe
    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
      var role = new ApplicationRole { Name = adminRoleName };
      var roleResult = await roleManager.CreateAsync(role);
      if (!roleResult.Succeeded)
      {
        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
        throw new Exception($"No se pudo crear el rol '{adminRoleName}': {errors}");
      }
    }

    // 2) Crear usuario admin si no existe
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
      adminUser = new ApplicationUser
      {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true,
        DisplayName = "Administrator",
        IsActive = true
      };

      var createResult = await userManager.CreateAsync(adminUser, adminPassword);
      if (!createResult.Succeeded)
      {
        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
        throw new Exception($"No se pudo crear el usuario admin: {errors}");
      }

      var addRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
      if (!addRoleResult.Succeeded)
      {
        var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
        throw new Exception($"No se pudo asignar rol al admin: {errors}");
      }
    }
  }
}
