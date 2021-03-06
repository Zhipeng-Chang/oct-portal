﻿using CoE.Ideas.Core.Data;
using CoE.Ideas.Core.ServiceBus;
using CoE.Ideas.Core.Services;
using CoE.Ideas.Remedy.Watcher.RemedyServiceReference;
using EnsureThat;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoE.Ideas.Remedy.SbListener
{
    public class RemedyItemUpdatedIdeaListener
    {
        public RemedyItemUpdatedIdeaListener(
            IInitiativeRepository ideaRepository,
            IPersonRepository personRepository,
            IInitiativeMessageReceiver initiativeMessageReceiver,
            Serilog.ILogger logger)
        {
            EnsureArg.IsNotNull(ideaRepository);
            EnsureArg.IsNotNull(personRepository);
            EnsureArg.IsNotNull(initiativeMessageReceiver);
            EnsureArg.IsNotNull(logger);

            _ideaRepository = ideaRepository;
            _personRepository = personRepository;
            _initiativeMessageReceiver = initiativeMessageReceiver;
            _logger = logger;

            initiativeMessageReceiver.ReceiveMessages(
                workOrderCreatedHandler: OnInitiativeWorkItemCreated,
                workOrderUpdatedHandler: OnWorkOrderUpdatedAsync);
        }

        private readonly IInitiativeRepository _ideaRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IInitiativeMessageReceiver _initiativeMessageReceiver;
        private readonly Serilog.ILogger _logger;

        protected virtual async Task OnInitiativeWorkItemCreated(WorkOrderCreatedEventArgs args, CancellationToken token)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            if (args.Initiative == null)
                throw new ArgumentException("Initiative cannot be null");
            if (string.IsNullOrWhiteSpace(args.WorkOrderId))
                throw new ArgumentException("WorkOrderId cannot be empty");

            using (LogContext.PushProperty("InitiativeId", args.Initiative.Id))
            {
                _logger.Information("Remedy has created a work item for initiative {InitiativeId} with remedy id {WorkOrderId}", args.Initiative.Id, args.WorkOrderId);

                try
                {
                    args.Initiative.SetWorkOrderId(args.WorkOrderId);

                    args.Initiative.UpdateStatus(InitiativeStatus.Submit, args.EtaUtc);

                    _logger.Information("Saving Initiative {InitiativeId} to database", args.Initiative.Id);
                    await _ideaRepository.UpdateInitiativeAsync(args.Initiative);
                }
                catch (Exception err)
                {
                    _logger.Error(err, "Unable to set work item id '{WorkOrderId}' to initiative id {InitiativeId}. Will retry later. Error was: {ErrorMessage}", 
                        args.WorkOrderId, args.Initiative.Id, err.Message);
                    throw;
                }
            }
        }

        protected virtual async Task<Initiative> OnWorkOrderUpdatedAsync(WorkOrderUpdatedEventArgs args, CancellationToken token)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            if (string.IsNullOrWhiteSpace(args.WorkOrderId))
                throw new ArgumentException("WorkOrderId cannot be empty");
            if (string.IsNullOrWhiteSpace(args.UpdatedStatus))
                throw new ArgumentException("UpdatedStatus cannot be empty");

            using (LogContext.PushProperty("WorkOrderId", args.WorkOrderId))
            {
                Initiative idea = await GetInitiativeByWorkOrderId(args.WorkOrderId);

                if (idea == null)
                    _logger.Warning("Remedy message received for WorkItemId {WorkOrderId} but could not find an associated initiative", args.WorkOrderId);
                else
                {
                    using (LogContext.PushProperty("InitiativeId", idea.Id))
                    {
                        var workOrderStatus = Enum.Parse<InitiativeStatus>(args.UpdatedStatus);

                        bool anyChange = await UpdateIdeaAssignee(idea, args.AssigneeEmail, args.AssigneeDisplayName);
                        anyChange = UpdateIdeaWithNewWorkOrderStatus(idea, workOrderStatus, args.UpdatedDateUtc, args.EtaUtc) || anyChange;
                        if (anyChange)
                            await _ideaRepository.UpdateInitiativeAsync(idea);
                    }
                }

                return idea;
            }
        }



        protected async Task<Initiative> GetInitiativeByWorkOrderId(string workOrderId)
        {
            Initiative idea = null;
            try
            {
                idea = await _ideaRepository.GetInitiativeByWorkOrderIdAsync(workOrderId);
            }
            catch (Exception err)
            {
                _logger.Error(err, "Received WorkItem change notification from Remedy for item with Id {WorkOrderId} but got the following error when looking it up in the Idea repository: {ErrorMessage}",
                    workOrderId, err.Message);
                idea = null;
            }
            return idea;
        }

        /// <summary>
        /// Sets the status on the initiative according to the Remedy Work Order status
        /// </summary>
        /// <param name="initiative"></param>
        /// <param name="workOrderStatus"></param>
        /// <param name="workOrderLastModifiedUtc"></param>
        /// <returns>True if the initiative status is updated, otherwise false</returns>
        protected bool UpdateIdeaWithNewWorkOrderStatus(Initiative initiative, InitiativeStatus newIdeaStatus, DateTime workOrderLastModifiedUtc, DateTime? etaUtc)
        {
            if (initiative.Status != newIdeaStatus)
            {
                _logger.Information("Updating status of initiative {InitiativeId} from {FromInitiativeStatus} to {ToIdeaStatus} because Remedy was updated on {LastModifiedDateUtc}",
                    initiative.Id, initiative.Status, newIdeaStatus, workOrderLastModifiedUtc);
                initiative.UpdateStatus(newIdeaStatus, etaUtc);
                return true;
            }
            else
            {
                _logger.Information("Not updating status because it has not changed from: {Status}", initiative.Status);
                return false;
            }
        }





        /// <summary>
        /// Updated the person assigned to the initiative
        /// </summary>
        /// <param name="idea"></param>
        /// <param name="assigneeEmail"></param>
        /// <param name="assigneeDisplayName"></param>
        /// <returns>True if the assignee was changed from its previous value</returns>
        private async Task<bool> UpdateIdeaAssignee(Initiative idea, string assigneeEmail, string assigneeDisplayName)
        {
            //Person assignee = null;
            int? assigneeId = 0;
            if (!string.IsNullOrWhiteSpace(assigneeEmail))
            {
                assigneeId = await _personRepository.GetPersonIdByEmailAsync(assigneeEmail);
                _logger.Information("PersonId for assigneeEmail {EmailAddress} is {PersonId}", assigneeEmail, assigneeId);

                // TODO: create the user if they don't exist?
            }
            else
            {
                _logger.Information("Assignee email is {EmailAddress}", assigneeEmail);
            }

            if (idea.AssigneeId != assigneeId)
            {
                _logger.Information("Updating assignee from id " + idea.AssigneeId + " to {AssigneeId}", assigneeId);
                idea.SetAssigneeId(assigneeId);
                return true;
            }
            else
            {
                _logger.Information("Not updating assignee because the AssigneeId has not changed ({AssigneeId})", assigneeId);
                return false;
            }
        }
    }
}
