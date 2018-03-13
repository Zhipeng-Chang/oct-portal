﻿using CoE.Ideas.Shared.People;
using CoE.Ideas.Shared.Security;
using CoE.Ideas.Shared.WordPress;
using EnsureThat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoE.Ideas.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWordPressSecurity(this IServiceCollection services,
            string wordPressDbConnectionString,
            IConfigurationSection wordPressConfigurationSection)
        {
            EnsureArg.IsNotNullOrWhiteSpace(wordPressDbConnectionString);
            EnsureArg.IsNotNull(wordPressConfigurationSection);

            string wordPressUrl = wordPressConfigurationSection["Url"];
            EnsureArg.IsNotNullOrWhiteSpace(wordPressUrl, "wordPressConfigurationSection", 
                o => o.WithMessage("wordPressConfigurationSection must contain a valid Url"));

            services.AddDbContext<WordPressContext>(options =>
                options.UseMySql(wordPressDbConnectionString));

            services.Configure<WordPressUserSecurityOptions>(wordPressConfigurationSection);
            services.AddScoped<IWordPressUserSecurity, WordPressUserSecurity>();

            services.AddScoped<IWordPressRepository, WordPressRepository>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = WordPressCookieAuthenticationDefaults.AuthenticationScheme;
            }).AddWordPressCookie(options =>
            {
                options.WordPressUrl = wordPressConfigurationSection["Url"];
            });

            return services;
        }

        public static IServiceCollection AddPeopleService(this IServiceCollection services,
            string peopleServiceUrl)
        {
            if (string.IsNullOrWhiteSpace(peopleServiceUrl))
                throw new ArgumentNullException("peopleServiceUrl");

            Uri peopleServiceUri;
            try
            {
                peopleServiceUri = new Uri(peopleServiceUrl);
            }
            catch (Exception err)
            {
                throw new InvalidOperationException($"peopleServiceUrl is not a valid url: { peopleServiceUrl }", err);
            }

            services.Configure<PeopleServiceOptions>(x =>
            {
                x.ServiceUrl = peopleServiceUri;
            });
            services.AddSingleton<IPeopleService, PeopleService>();

            return services;
        }
    }
}
