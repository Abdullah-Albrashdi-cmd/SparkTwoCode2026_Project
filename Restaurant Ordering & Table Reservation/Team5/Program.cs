using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Team5;
using Team5.Models;
using Team5.Services;

namespace Team5
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            //DbContext
            builder.Services.AddDbContext<ProjectContext>(options =>
             options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            ////////////////
            ///
            builder.Services.AddControllers(options =>
            {
                // Navigation properties (e.g. MenuCategory.MenuItems) are non-nullable
                // reference types, which ASP.NET Core would otherwise treat as implicitly
                // [Required] during model validation. That would make it impossible to
                // create/update an entity without also sending its related collections.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // =====================================
            // JWT Authentication
            // =====================================

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!
                        )
                    )
                };
            });


            // =====================================
            // Authorization
            // =====================================

            builder.Services.AddAuthorization();


            // =====================================
            // Email Service
            // =====================================

            builder.Services.AddScoped<EmailService, EmailService>();
            // builder.Services.AddScoped<EmailService, EmailService>(); same as builder.Services.AddScoped<EmailService>();

            // JWT token generator (required by AuthController)
            builder.Services.AddScoped<JwtService>();

            // =====================================
            // Swagger
            // =====================================

            //swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token in the box below"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            new List<string>()
        }
    });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProjectContext>();
                db.Database.Migrate();

                var passwordHasher = new PasswordHasher<User>();

                if (!db.Users.Any())
                {
                    db.Users.AddRange(
                        new User
                        {
                            UserName = "Admin",
                            UserEmail = "admin@spoons.com",
                            PhoneNumber = "1234567890",
                            Role = "Admin",
                            CreatedAt = DateTime.UtcNow,
                            PasswordHash = passwordHasher.HashPassword(null!, "admin123")
                        },
                        new User
                        {
                            UserName = "Sara Smith",
                            UserEmail = "sara@spoons.com",
                            PhoneNumber = "9876543210",
                            Role = "Customer",
                            CreatedAt = DateTime.UtcNow,
                            PasswordHash = passwordHasher.HashPassword(null!, "sara123")
                        }
                    );
                }

                if (!db.MenuCategories.Any())
                {
                    db.MenuCategories.AddRange(
                        new MenuCategory { MenuCategoryName = "Starters", MenuCategoryDescription = "Light appetizers and small plates.", DisplayOrder = 1 },
                        new MenuCategory { MenuCategoryName = "Mains", MenuCategoryDescription = "Signature mains and house favorites.", DisplayOrder = 2 },
                        new MenuCategory { MenuCategoryName = "Desserts", MenuCategoryDescription = "Sweet finishes for every meal.", DisplayOrder = 3 },
                        new MenuCategory { MenuCategoryName = "Drinks", MenuCategoryDescription = "Fresh drinks, coffee, and mocktails.", DisplayOrder = 4 }
                    );
                }

                if (!db.Tables.Any())
                {
                    db.Tables.AddRange(
                        new Table { TableNumber = "T1", Capacity = 2, Location = "Indoor", IsActive = true },
                        new Table { TableNumber = "T2", Capacity = 4, Location = "Indoor", IsActive = true },
                        new Table { TableNumber = "T3", Capacity = 6, Location = "Patio", IsActive = true },
                        new Table { TableNumber = "T4", Capacity = 2, Location = "Window", IsActive = true }
                    );
                }

                if (!db.Ingredients.Any())
                {
                    db.Ingredients.AddRange(
                        new Ingredient { IngredientName = "Tomato", UnitOfMeasure = "kg", QuantityInStock = 10m, ReorderLevel = 3m },
                        new Ingredient { IngredientName = "Basil", UnitOfMeasure = "bunch", QuantityInStock = 8m, ReorderLevel = 2m },
                        new Ingredient { IngredientName = "Chicken", UnitOfMeasure = "kg", QuantityInStock = 12m, ReorderLevel = 4m },
                        new Ingredient { IngredientName = "Cheese", UnitOfMeasure = "kg", QuantityInStock = 9m, ReorderLevel = 3m },
                        new Ingredient { IngredientName = "Cocoa", UnitOfMeasure = "kg", QuantityInStock = 6m, ReorderLevel = 2m }
                    );
                }

                db.SaveChanges();

                if (!db.MenuItems.Any())
                {
                    var categories = db.MenuCategories.OrderBy(c => c.DisplayOrder).ToList();
                    var starters = categories.First(c => c.MenuCategoryName == "Starters");
                    var mains = categories.First(c => c.MenuCategoryName == "Mains");
                    var desserts = categories.First(c => c.MenuCategoryName == "Desserts");
                    var drinks = categories.First(c => c.MenuCategoryName == "Drinks");

                    db.MenuItems.AddRange(
                        new MenuItem { MenuItemName = "Crispy Calamari", MenuItemDescription = "Golden fried calamari with lemon aioli.", Price = 12.50m, ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80", IsAvailable = true, MenuCategoryId = starters.MenuCategoryId },
                        new MenuItem { MenuItemName = "Garden Salad", MenuItemDescription = "Fresh greens, tomato, cucumber, and herb vinaigrette.", Price = 9.75m, ImageUrl = "https://images.unsplash.com/photo-1546793665-c74683f339c1?auto=format&fit=crop&w=800&q=80", IsAvailable = true, MenuCategoryId = starters.MenuCategoryId },
                        new MenuItem { MenuItemName = "Grilled Chicken Bowl", MenuItemDescription = "Herb chicken, rice, greens, and roasted vegetables.", Price = 18.90m, ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80", IsAvailable = true, MenuCategoryId = mains.MenuCategoryId },
                        new MenuItem { MenuItemName = "Margherita Pizza", MenuItemDescription = "Wood-fired pizza with tomato, basil, and mozzarella.", Price = 16.50m, ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80", IsAvailable = true, MenuCategoryId = mains.MenuCategoryId },
                        new MenuItem { MenuItemName = "Chocolate Lava Cake", MenuItemDescription = "Warm chocolate cake with a molten center.", Price = 8.25m, ImageUrl = "https://images.unsplash.com/photo-1551024601-bec78aea704b?auto=format&fit=crop&w=800&q=80", IsAvailable = true, MenuCategoryId = desserts.MenuCategoryId },
                        new MenuItem { MenuItemName = "Fresh Lemonade", MenuItemDescription = "House-made citrus lemonade with mint.", Price = 4.50m, ImageUrl = "https://images.unsplash.com/photo-1546173159-315724a31696?auto=format&fit=crop&w=800&q=80", IsAvailable = true, MenuCategoryId = drinks.MenuCategoryId }
                    );
                }

                db.SaveChanges();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("FrontendPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}