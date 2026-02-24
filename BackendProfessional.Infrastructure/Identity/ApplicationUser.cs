using System;
using Microsoft.AspNetCore.Identity;

namespace BackendProfessional.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
  public string DisplayName { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
}
