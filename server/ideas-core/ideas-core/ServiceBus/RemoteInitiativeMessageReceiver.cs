﻿using CoE.Ideas.Core.Services;
using EnsureThat;
using Microsoft.Azure.ServiceBus;
using System;
using System.Security.Claims;

namespace CoE.Ideas.Core.ServiceBus
{
    internal class RemoteInitiativeMessageReceiver : InitiativeMessageReceiver
    {
        public RemoteInitiativeMessageReceiver(ISubscriptionClient subscriptionClient,
                Serilog.ILogger logger,
                IServiceProvider serviceProvider) : base(subscriptionClient, logger)
        {
            EnsureArg.IsNotNull(serviceProvider);
            _serviceProvider = serviceProvider;
        }

        private readonly IServiceProvider _serviceProvider;

        protected override IInitiativeRepository GetInitiativeRepository(ClaimsPrincipal owner)
        {
            var remoteInitiativeRepository = _serviceProvider.GetService(typeof(RemoteInitiativeRepository)) as RemoteInitiativeRepository;
            if (remoteInitiativeRepository == null)
                throw new InvalidOperationException("Unable to find RemoteInitiativeRepository in ServiceProvider");
            remoteInitiativeRepository.SetUser(owner);
            return remoteInitiativeRepository;
        }
    }
}