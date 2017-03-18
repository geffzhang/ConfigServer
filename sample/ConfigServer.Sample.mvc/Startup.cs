﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ConfigServer.Sample.mvc.Models;
using ConfigServer.Core;
using ConfigServer.Server;
using ConfigServer.FileProvider;
using System.Linq;
using System.Collections.Generic;
using ConfigServer.Client.ResourceServer;

namespace ConfigServer.Sample.mvc
{
    public class Startup
    {
        private readonly IHostingEnvironment enviroment;
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            enviroment = env;
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var applicationId = "3E37AC18-A00F-47A5-B84E-C79E0823F6D4";
            var application2Id = "6A302E7D-05E9-4188-9612-4A2920E5C1AE";
            // Add framework services.
            services.AddMvc();
            services.AddConfigServer()
                .UseConfigSet<SampleConfigSet>()
                .UseFileConfigProvider(new FileConfigRespositoryBuilderOptions { ConfigStorePath = enviroment.ContentRootPath + "/Store/Configs" })
                .UseFileResourceProvider(new FileResourceRepositoryBuilderOptions { ResourceStorePath = enviroment.ContentRootPath + "/Store/Resources" })
                .UseLocalConfigServerClient(applicationId, new Uri("http://localhost:58201/Config"))
                .WithConfig<SampleConfig>()
                .WithCollectionConfig<OptionFromConfigSet>();

            services.AddTransient<IOptionProvider, OptionProvider>();
            var options1 = new List<OptionFromConfigSet>
            {
                new OptionFromConfigSet { Id =1, Description ="One", Value = 2.4},
                new OptionFromConfigSet { Id =2, Description ="Two", Value = 12.4}
            };
            var options2 = new List<OptionFromConfigSet>
            {
                new OptionFromConfigSet { Id =1, Description ="One", Value = 24.4},
                new OptionFromConfigSet { Id =2, Description ="Two", Value = 12.4}
            };
            var optionProvider = new OptionProvider();
            var config = new SampleConfig
            {
                LlamaCapacity = 23,
                Name = "Name",
                Decimal = 23.47m,
                StartDate = new DateTime(2013, 10, 10),
                IsLlamaFarmer = false,
                Option = optionProvider.GetOptions().First(),
                MoarOptions = optionProvider.GetOptions().Take(2).ToList(),
                ListOfConfigs = new List<ListConfig>
                {
                    new ListConfig { Name = "Value One", Value = 1 },
                    new ListConfig { Name = "Value Two", Value = 2 }
                },
                OptionFromConfigSet = options1[1],
                MoarOptionFromConfigSet = new List<OptionFromConfigSet> { options1[0] }
            };
            var config2 = new SampleConfig
            {
                LlamaCapacity = 41,
                Name = "Name 2",
                Decimal = 41.47m,
                StartDate = new DateTime(2013, 11, 11),
                Choice = Choice.OptionThree,
                IsLlamaFarmer = true,
                Option = optionProvider.GetOptions().First(),
                MoarOptions = optionProvider.GetOptions().Take(2).ToList(),
                OptionFromConfigSet = options2[0],
                MoarOptionFromConfigSet = new List<OptionFromConfigSet> { options2[1] }
            };
            var serviceProvider = services.BuildServiceProvider();
            var configRepo = serviceProvider.GetService<IConfigRepository>();
            configRepo.UpdateClientAsync(new ConfigurationClient { ClientId = applicationId, Name = "Mvc App", Description = "Embeded Application" }).Wait();
            configRepo.UpdateClientAsync(new ConfigurationClient { ClientId = application2Id, Name = "Mvc App 2", Description = "Second Application" }).Wait();
            configRepo.UpdateConfigAsync(new ConfigCollectionInstance<OptionFromConfigSet>(options1, applicationId)).Wait();
            configRepo.UpdateConfigAsync(new ConfigCollectionInstance<OptionFromConfigSet>(options2, application2Id)).Wait();
            configRepo.UpdateConfigAsync(new ConfigInstance<SampleConfig>(config, applicationId)).Wait();
            configRepo.UpdateConfigAsync(new ConfigInstance<SampleConfig>(config2, application2Id)).Wait();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            

            app.UseStaticFiles();
            app.Map("/Config", configSrv => configSrv.UseConfigServer(new ConfigServerOptions {
                ServerAuthenticationOptions = new ConfigServerAuthenticationOptions { RequireAuthentication = false },
                ManagerAuthenticationOptions = new ConfigServerAuthenticationOptions { RequireAuthentication = false } 
            }));
            app.Map("/Resource", innerApp => innerApp.UseResourceServer());
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

    }
}
