using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Serilog;
using Serilog.Formatting.Compact;
using DiscussionBoard.Authorization;
using Microsoft.AspNetCore.Authorization;


namespace DiscussionBoard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Get the location of the Logs folder for SeriLog
            string logPath = Path.Combine(builder.Environment.ContentRootPath, "Logs",  "discussionboard-.json");

            builder.Services.AddSerilog((services, configuration) => configuration
                .WriteTo.Console()
                .WriteTo.File(
                    new RenderedCompactJsonFormatter(),
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    shared: true)
            );

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment())
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Forums");
                options.Conventions.AuthorizePage("/Discussion");
                options.Conventions.AuthorizePage("/MyProfile");

                if(builder.Environment.IsEnvironment("Testing"))
                {
                    options.Conventions.AllowAnonymousToPage("/Discussions/Details");
                }
            });

            builder.Services.AddSingleton<ILogReader, JsonLogReader>();
            builder.Services.AddScoped<IApplicationAuthorizationService, ApplicationAuthorizationService>();
            builder.Services.AddScoped<ForumService>();
            builder.Services.AddScoped<DiscussionThreadService>();
            builder.Services.AddScoped<PostService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(ApplicationAuthorizationPolicies.IsOwnerOrAdmin, policy =>
                    policy.Requirements.Add(new IsOwnerOrAdminRequirement()));
            });

            builder.Services.AddSingleton<IAuthorizationHandler, IsOwnerOrAdminHandler>();
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate(); // makes sure the tables exist

                if (!db.Forums.Any())
                {
                    db.Forums.AddRange(
                        new Forum { Name = "General", Description = "General discussion" },
                        new Forum { Name = "Help", Description = "Questions and answers" }
                    );
                    db.SaveChanges();
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days.
                // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapRazorPages()
                .WithStaticAssets();

          /*if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
            {
                if (!app.Environment.IsDevelopment())
                {
                    throw new InvalidOperationException("This tool is development only.");
                }

                await AdminBootstrapper.BootstrapAsync(
                    app.Services,
                    app.Configuration ["BootstrapAdmin:Email"]
                );
                return;
            }*/
            app.Run();
        }

    }
}
