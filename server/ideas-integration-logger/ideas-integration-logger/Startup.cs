﻿using CoE.Ideas.Core;
using CoE.Ideas.Core.ServiceBus;
using CoE.Ideas.Core.WordPress;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoE.Ideas.Integration.Logger
{
    public class Startup
    {
        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
            var services = ConfigureServices(new ServiceCollection());

            var serviceProvider = services.BuildServiceProvider();

            // Ensure NewIdeaListener is created to start the message pump (in it's constructor)
            serviceProvider.GetRequiredService<NewIdeaListener>();
        }

        private readonly IConfigurationRoot Configuration;

        private IServiceCollection ConfigureServices(IServiceCollection services)
        {
            // basic stuff
            services.AddOptions();

            // Add logging 
            services.AddSingleton(new LoggerFactory()
                .AddConsole(Configuration)
                .AddDebug()
                .AddSerilog());
            services.AddLogging();

            // configure application specific logging
            services.AddSingleton<Serilog.ILogger>(x => new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Initiatives")
                .Enrich.WithProperty("Module", "Logging")
                .ReadFrom.Configuration(Configuration)
                .CreateLogger());


            services.AddRemoteIdeaConfiguration(Configuration["IdeasApi"],
                Configuration["WordPressUrl"]);

            services.AddInitiativeMessaging(Configuration["ServiceBus:ConnectionString"],
                Configuration["ServiceBus:TopicName"],
                Configuration["ServiceBus:Subscription"]);

            services.AddSingleton<IIdeaLogger, IIdeaLogger>(x =>
            {
                return new GoogleSheetIdeaLogger(
                    Configuration["Logger:serviceAccountPrivateKey"],
                    Configuration["Logger:serviceAccountEmail"],
                    Configuration["Logger:spreadsheetId"],
                    Configuration["IdeasApi"]);
            });


            return services;
        }
    }
}