using Microsoft.EntityFrameworkCore;
using MyStreeT.Api.Auth;
using MyStreeT.Api.Entities;

namespace MyStreeT.Api.Data;

/// <summary>Seeds demo products and demo accounts. Passwords come from configuration and are hashed with BCrypt.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, IConfiguration config, ILogger logger)
    {
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@mystreet.com";
        var adminPassword = config["Seed:AdminPassword"];
        var demoEmail = config["Seed:DemoUserEmail"] ?? "demo@mystreet.com";
        var demoPassword = config["Seed:DemoUserPassword"];

        if (!string.IsNullOrWhiteSpace(adminPassword) && !await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new User { Id = Guid.NewGuid(), Email = adminEmail, PasswordHash = hasher.Hash(adminPassword), IsAdmin = true });
            logger.LogInformation("Seeded admin user {Email}", adminEmail);
        }

        if (!string.IsNullOrWhiteSpace(demoPassword) && !await db.Users.AnyAsync(u => u.Email == demoEmail))
        {
            db.Users.Add(new User { Id = Guid.NewGuid(), Email = demoEmail, PasswordHash = hasher.Hash(demoPassword), IsAdmin = false });
            logger.LogInformation("Seeded demo user {Email}", demoEmail);
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(Products());
            logger.LogInformation("Seeded demo products");
        }

        await db.SaveChangesAsync();
    }

    private static Product P(string name, string brand, decimal price, string sizes, int stock, string image, string description) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Brand = brand, Price = price, SizesCsv = sizes,
            StockQty = stock, ImageUrl = $"/sneakers/{image}.svg", Description = description
        };

    private static IEnumerable<Product> Products() => new[]
    {
        P("Air Max 90", "Nike", 119.99m, "7,8,9,10", 50, "air-max-90", "A street classic with a visible Air unit, waffle outsole and layered leather upper."),
        P("Air Force 1 Low", "Nike", 109.99m, "6,7,8,9,10,11", 60, "air-force-1", "The clean court icon: durable leather, cupped sole and all-day cushioning."),
        P("Dunk Low Retro", "Nike", 114.99m, "7,8,9,10,11", 40, "dunk-low", "Basketball heritage turned everyday staple, with a padded collar and bold colour blocking."),
        P("Ultraboost Light", "Adidas", 139.99m, "7,8,9,10,11", 35, "ultraboost", "Responsive Boost cushioning and a sock-like Primeknit upper for long city miles."),
        P("Samba OG", "Adidas", 99.99m, "6,7,8,9,10", 45, "samba", "Terrace-born, suede-toed and gum-soled. Understated, always in rotation."),
        P("Gazelle", "Adidas", 94.99m, "7,8,9,10", 30, "gazelle", "Soft suede, a slim profile and the unmistakable three stripes."),
        P("574 Core", "New Balance", 89.99m, "7,8,9,10,11", 55, "nb-574", "Heritage runner silhouette with ENCAP midsole support for everyday comfort."),
        P("990v6", "New Balance", 199.99m, "8,9,10,11", 20, "nb-990", "Premium suede and mesh with a cushioned ENCAP and FuelCell midsole."),
        P("Chuck 70 High", "Converse", 89.99m, "6,7,8,9,10", 70, "chuck-70", "Heavier canvas, cushioned insole and vintage detailing on the iconic high-top."),
        P("Old Skool", "Vans", 74.99m, "6,7,8,9,10,11", 80, "old-skool", "The original side-stripe skate shoe with a durable suede and canvas upper."),
        P("Suede Classic XXI", "Puma", 79.99m, "7,8,9,10", 38, "puma-suede", "Soft suede, a formstrip and a legacy built on the 60s basketball court."),
        P("Classic Leather", "Reebok", 84.99m, "7,8,9,10,11", 42, "reebok-classic", "Soft garment leather and a die-cut EVA midsole in a timeless low-top profile.")
    };
}
