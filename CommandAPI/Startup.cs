using CommandAPI.Brokers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Npgsql;
using System;

namespace CommandAPI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            
        }


        public void ConfigureServices(IServiceCollection services)
        {

            var builder = new NpgsqlConnectionStringBuilder
            {
                ConnectionString = Configuration.GetConnectionString("PostgreSqlConnection"),
                Username = Configuration["UserID"],
                Password = Configuration["Password"]
            };


            // Need to reconfigure Azure for PROD AAD
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
            { 
                opt.Audience = Configuration["ResourceId"];
                opt.Authority = $"{Configuration["Instance"]}{Configuration["TenantId"]}";
            });
            services.AddDbContext<StorageBroker>(opt => opt.UseNpgsql(builder.ConnectionString));
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddControllers().AddNewtonsoftJson(s =>
            {
                s.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            services.AddScoped<IStorageBroker, StorageBroker>();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, StorageBroker context)
        {
            context.Database.Migrate();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

            });
        }
    }
}
