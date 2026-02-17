using System.Text.Json; 
using System.IO;   
using Microsoft.EntityFrameworkCore;    

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=glowflow.db"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Replace with your actual Angular URL
        policy.WithOrigins("http://localhost:4200") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "Welcome to the Glowflow API! ✨");


app.MapGet("/makeup/{category}/{id}", async (string category, string id, AppDbContext context) =>
{
    
    var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);

    // If the product is found, return it. Otherwise, 404.
    if (product is null)
    {
        return Results.NotFound(new { message = $"Product {id} not found." });
    }

    return Results.Ok(product);
});

app.MapGet("/makeup/{category}", async (string category, AppDbContext context) =>
{
    // 1. Query the database for matching products
    var products = await context.Products
        .Where(p => p.Category == category)
        .ToListAsync();

    // 2. Return the results (or a 404 if the category is empty/wrong)
    return products.Any() ? Results.Ok(products) : Results.NotFound();
});



// This creates a temporary 'scope' to get the database manager
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    
    // Run the seeding logic
    await DbInitializer.SeedAsync(context);
}

app.Run();



public class Product 
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public List<string> SkinType { get; set; } = new();
    public string Formulation { get; set; } = string.Empty;
    public List<string> Color { get; set; } = new();
    public string CountryOfOrigin { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    
    // Some products have coverage, some don't (nullable string)
    public string? Coverage { get; set; } 
}


public class MakeupData : Dictionary<string, Dictionary<string, Product>> { }

// 📂 This class manages our database connection
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }

    // This 'OnModelCreating' is where we give the manager special instructions
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // We tell EF how to handle the List properties for SQLite
        modelBuilder.Entity<Product>()
            .Property(p => p.SkinType)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

        modelBuilder.Entity<Product>()
            .Property(p => p.Color)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

        modelBuilder.Entity<Product>()
            .Property(p => p.Images)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
    }
}

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Only seed if the database is empty
        if (await context.Products.AnyAsync()) return;

        // 2. Read the JSON file
        string filePath = "makeup.json";
        if (!File.Exists(filePath)) return;

        string jsonText = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<MakeupData>(jsonText, options);

        if (data != null)
        {
            foreach (var categoryEntry in data)
{
    string categoryName = categoryEntry.Key; // e.g., "face-primer"

    foreach (var productEntry in categoryEntry.Value)
    {
        var product = productEntry.Value;
        product.Category = categoryName; // 👈 Assign the category here
        context.Products.Add(product);
    }
}
            // 3. Save everything to the glowflow.db file
            await context.SaveChangesAsync();
        }
    }
}