using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Seeds.Seeders;

public class RoleSeeder(RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task SeedAsync()
    {
        if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
            await roleManager.CreateAsync(new IdentityRole<Guid>(UserRoles.Admin));

        if (!await roleManager.RoleExistsAsync(UserRoles.Member))
            await roleManager.CreateAsync(new IdentityRole<Guid>(UserRoles.Member));
    }
}