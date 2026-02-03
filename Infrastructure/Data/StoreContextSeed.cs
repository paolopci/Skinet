using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data
{
    public class StoreContextSeed
    {
        public static async Task SeedAsync(
            StoreContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            if (!context.Products.Any())
            {
                var productData = await File.ReadAllTextAsync("../Infrastructure/Data/SeedData/products.json");
                var products = JsonSerializer.Deserialize<List<Product>>(productData);

                if (products == null)
                {
                    return;
                }

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            var adminSection = configuration.GetSection("AdminSeed");
            var adminEmail = adminSection["Email"];
            var adminPassword = adminSection["Password"];
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var roleNames = adminSection.GetSection("Roles")
                .GetChildren()
                .Select(section => section.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();
            if (roleNames.Length == 0)
            {
                roleNames = ["Admin"];
            }
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                        throw new InvalidOperationException($"Creazione role '{roleName}' fallita: {errors}");
                    }
                }
            }

            var existingUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingUser == null)
            {
                var adminUser = new AppUser
                {
                    FirstName = adminSection["FirstName"] ?? "Admin",
                    LastName = adminSection["LastName"] ?? "User",
                    Email = adminEmail,
                    UserName = adminEmail,
                    PhoneNumber = adminSection["PhoneNumber"],
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Creazione utente admin fallita: {errors}");
                }

                existingUser = adminUser;
            }

            foreach (var roleName in roleNames)
            {
                if (!await userManager.IsInRoleAsync(existingUser, roleName))
                {
                    var roleAssignResult = await userManager.AddToRoleAsync(existingUser, roleName);
                    if (!roleAssignResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleAssignResult.Errors.Select(error => error.Description));
                        throw new InvalidOperationException($"Assegnazione role '{roleName}' fallita: {errors}");
                    }
                }
            }
        }
    }
}
