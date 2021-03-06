﻿using EnsureThat;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoE.Ideas.Shared.People
{
    internal class PeopleService : IPeopleService
    {
        public PeopleService(IOptions<PeopleServiceOptions> options,
            IMemoryCache memoryCache,
            ILogger logger)
        {
            EnsureArg.IsNotNull(memoryCache);
            EnsureArg.IsNotNull(logger);
            _memoryCache = memoryCache;
            _logger = logger;

            if (options == null || options.Value == null)
                throw new ArgumentNullException("options");

            _serviceUrl = options.Value.ServiceUrl;
            if (_serviceUrl == null)
                throw new ArgumentOutOfRangeException("PeopleServiceOptions ServiceUrl must be specified");

            // ensure _serviceUrl ends with "/", because we'll be appending 
            if (!_serviceUrl.ToString().EndsWith("/"))
                _serviceUrl = new Uri(_serviceUrl + "/");
        }

        private readonly Uri _serviceUrl;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;

        public async Task<PersonData> GetPersonAsync(string user3and3)
        {
            if (string.IsNullOrWhiteSpace(user3and3))
                throw new ArgumentNullException("user3and3");

            return await _memoryCache.GetOrCreateAsync("3_" + user3and3, async cacheEntry =>
            {
                _logger.Information("User with NetworkId {NetworkId} not found in cache, retrieving from web service...", user3and3);
                string userDataString;
                using (var client = GetHttpClient())
                {

                    try
                    {
                        userDataString = await client.GetStringAsync($"OrganizationUnits/NetworkId/{user3and3}");
                    }
                    catch (Exception err)
                    {
                        throw new InvalidOperationException($"Unable to get data for user {user3and3}: {err.Message}", err);
                    }
                }

                if (string.IsNullOrWhiteSpace(userDataString))
                    throw new InvalidOperationException($"Unable to get data for user {user3and3}");

                return JsonConvert.DeserializeObject<PersonData>(userDataString);
            });
        }

        public async Task<PersonData> GetPersonByEmailAsync(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                throw new ArgumentNullException("emailAddress");

            return await _memoryCache.GetOrCreateAsync("E_" + emailAddress, async cacheEntry =>
            {
                _logger.Information("User with Email {EmailAddress} not found in cache, retrieving from web service...", emailAddress);
                string userDataString;
                using (var client = GetHttpClient())
                {
                    try
                    {
                        userDataString = await client.GetStringAsync($"OrganizationUnits/Email/{emailAddress}");
                    }
                    catch (Exception err)
                    {
                        throw new InvalidOperationException($"Unable to get data for user {emailAddress}: {err.Message}", err);
                    }
                }

                if (string.IsNullOrWhiteSpace(userDataString))
                    throw new InvalidOperationException($"Unable to get data for user {emailAddress}");

                return JsonConvert.DeserializeObject<PersonData>(userDataString);
            });
        }
        protected virtual HttpClient GetHttpClient()
        {

            var client = new HttpClient
            {
                BaseAddress = _serviceUrl
            };

            // easy - no credentials to set :)
            return client;
        }
    }
}
