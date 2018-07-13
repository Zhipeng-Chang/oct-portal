﻿using CoE.Issues.Core.Data;
using CoE.Issues.Core.ServiceBus;
using CoE.Issues.Core.Services;
using EnsureThat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using CoE.Ideas.Shared.People;
using System.Diagnostics;

namespace CoE.Issues.Remedy.SbListener
{
    public class RemedyIssueCreatedListener
    {
        public RemedyIssueCreatedListener(
            IIssueMessageReceiver issueMessageReceiver,
            IIssueRepository issueRepository,
            IPersonRepository userRepository,
            Serilog.ILogger logger)
        {
            EnsureArg.IsNotNull(issueRepository);
            EnsureArg.IsNotNull(issueMessageReceiver);
            EnsureArg.IsNotNull(logger);
            _issueRepository = issueRepository ?? throw new ArgumentException("issueRepository");
            _issueMessageReceiver = issueMessageReceiver?? throw new ArgumentException("issueMessageReceiver");
            _logger = logger ?? throw new ArgumentException("logger");
            _userRepository = userRepository;
            issueMessageReceiver.ReceiveMessages(
                issueCreatedHandler: OnIssueCreated);

        }

        private readonly IIssueRepository _issueRepository;
        private readonly Serilog.ILogger _logger;
        private readonly IIssueMessageReceiver _issueMessageReceiver;
        private readonly IPersonRepository _userRepository;



        protected virtual async Task OnIssueCreated(IssueCreatedEventArgs args, CancellationToken token)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            if (string.IsNullOrWhiteSpace(args.ReferenceId))
                throw new ArgumentException("ReferenceId cannot be empty");

            using (LogContext.PushProperty("IssueId", args.ReferenceId))
            {
                int? userId;
                try
                {
                    userId = string.IsNullOrWhiteSpace(args.RequestorEmail)
                    ? null : await GetOrCreateUserIdAsync(args.RequestorEmail, args.RequestorGivenName,args.RequestorSurnName,args.RequestorTelephone);

                }
                catch (Exception err)
                {
                    userId = null;
                    _logger.Warning(err, "Error retrieving user details for {EmailAddress}", args.RequestorEmail);
                }

                int ownerPersonId = userId.GetValueOrDefault();

                try
                {
                    var issue = Issue.Create(args.Title, args.Description, args.ReferenceId, args.RemedyStatus, args.RequestorDisplayName, args.AssigneeEmail, args.CreatedDate, ownerPersonId);
                    _logger.Information("Saving Issue {IssueId} to database", args.ReferenceId);
                    await _issueRepository.AddIssueAsync(issue, token);

                }
                catch (Exception err)
                {
                    _logger.Error(err, "Unable to set work item id to Issue. Will retry later. Error was: {ErrorMessage}",
                        err.Message);
                    throw;
                }
            }


        }

        private async Task<int?> GetOrCreateUserIdAsync(string email, string givenname, string surnname, string telephone)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            int? userId = await _userRepository.GetPersonIdByEmailAsync(email);



            if (userId == null || userId <= 0)
            {
                PersonData userInfo = new PersonData();
                userInfo.Email = email;
                userInfo.GivenName = givenname;
                userInfo.Surname = surnname;
                userInfo.Telephone = telephone;
                if (userInfo == null)
                {
                    _logger.Error("User not found with email {EmailAddress}", email);
                }
                else
                {
                    _logger.Information("Creating new WordPress user '{UserName}' with email {EmailAddress}", userInfo.DisplayName, email);
                    var newUserInfo = await _userRepository.CreatePerson(userInfo.GivenName, userInfo.Surname, email, userInfo.Telephone);
                    userId = newUserInfo.Id;
                }
            }

            return userId;
        }


    }
}

