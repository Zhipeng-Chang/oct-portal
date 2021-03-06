﻿using CoE.Ideas.Core.Data;
using CoE.Ideas.Core.ServiceBus;
using CoE.Ideas.Core.Services;
using CoE.Ideas.Remedy.Watcher.RemedyServiceReference;
using CoE.Ideas.Shared.People;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoE.Ideas.Remedy.Watcher
{
    public class RemedyChecker : IRemedyChecker
    {
        public RemedyChecker(IRemedyService remedyService,
            IInitiativeRepository initiativeRepository,
            IInitiativeMessageSender initiativeMessageSender,
            IPeopleService peopleService,
            IInitiativeStatusEtaService initiativeStatusEtaService,
            Serilog.ILogger logger,
            IOptions<RemedyCheckerOptions> options)
        {
            _remedyService = remedyService ?? throw new ArgumentNullException("remedyService");
            _initiativeRepository = initiativeRepository ?? throw new ArgumentNullException("initiativeRepository");
            _initiativeMessageSender = initiativeMessageSender ?? throw new ArgumentNullException("initiativeMessageSender");
            _peopleService = peopleService ?? throw new ArgumentNullException("peopleService");
            _initiativeStatusEtaService = initiativeStatusEtaService ?? throw new ArgumentNullException("initiativeStatusEtaService");
            _logger = logger ?? throw new ArgumentException("logger");

            if (options == null || options.Value == null)
                throw new ArgumentNullException("options");
            _options = options.Value;
        }


        private readonly IRemedyService _remedyService;
        private readonly IInitiativeRepository _initiativeRepository;
        private readonly IInitiativeMessageSender _initiativeMessageSender;
        private readonly IPeopleService _peopleService;
        private readonly IInitiativeStatusEtaService _initiativeStatusEtaService;
        private readonly Serilog.ILogger _logger;
        private RemedyCheckerOptions _options;

        private const string ResultFilePrefix = "RemedyCheckerLog";

        private DateTime lastPollTimeUtc;
        private void TryReadLastPollTime()
        {
            bool success = false;
            if (Directory.Exists(_options.TempDirectory))
            {
                // get the latest file starting with "RemedyCheckerLog"
                var latest = new DirectoryInfo(_options.TempDirectory)
                    .GetFiles("RemedyCheckerLog*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(x => x.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (latest != null)
                {
                    try
                    {
                        using (StreamReader file = File.OpenText(latest.FullName))
                        {
                            var lastPollResult = (RemedyPollResult)(new JsonSerializer()
                                .Deserialize(file, typeof(RemedyPollResult)));
                            lastPollTimeUtc = lastPollResult.EndTimeUtc;
                            success = true;
                        }
                    }
                    catch (Exception err)
                    {
                        // TODO: keep going through files until we find a good one?
                        _logger.Error($"Unable to get last time we polled remedy for work item changes: { err.Message }");
                    }
                }
            }
            if (!success)
                lastPollTimeUtc = new DateTime(2018, 1, 1); //DateTime.Now.AddDays(-3);
        }

        public async Task<RemedyPollResult> Poll()
        {
            TryReadLastPollTime();
            _logger.Information("Using last poll time of {PollTime}", lastPollTimeUtc);

            var result = await PollAsync(lastPollTimeUtc);
            SaveResult(result);
            return result;
        }


        public async Task<RemedyPollResult> PollAsync(DateTime fromUtc)
        {
            Stopwatch watch = new Stopwatch();

            var result = new RemedyPollResult(fromUtc);
            IEnumerable<OutputMapping1GetListValues> workItemsChanged = null;
            try { workItemsChanged = await _remedyService.GetRemedyChangedWorkItems(fromUtc); }
            catch (Exception err)
            {
                result.ProcessErrors.Add(new ProcessError() { ErrorMessage = err.Message });
            }
            if (workItemsChanged != null && workItemsChanged.Any())
            {
                await ProcessWorkItemsChanged(workItemsChanged, result, fromUtc);
            }
            result.EndTimeUtc = result.RecordsProcesed.Any()
                ? result.RecordsProcesed.Max(x => x.Last_Modified_Date)
                : lastPollTimeUtc;

            _logger.Information($"Finished Polling Remedy in { watch.Elapsed.TotalSeconds}s");

            return result;
        }

        private void SaveResult(RemedyPollResult result)
        {
            var filename = Path.Combine(_options.TempDirectory, $"{ResultFilePrefix}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log");
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, result);
            }
        }



        private async Task ProcessWorkItemsChanged(IEnumerable<OutputMapping1GetListValues> workItemsChanged,
            RemedyPollResult result, DateTime timestampUtc)
        {
            Stopwatch watch =  new Stopwatch();
            watch.Start();

            int count = 0;
            foreach (var workItem in workItemsChanged)
            {
                _logger.Information("Processing workItemChanged for id {InstanceId}", workItem.InstanceId);

                // We can do the next line because this service will always be in the same time zone as Remedy
                DateTime lastModifiedDateUtc = workItem.Last_Modified_Date.ToUniversalTime();

                if (lastModifiedDateUtc <= timestampUtc)
                {
                    _logger.Warning($"WorkItem { workItem.WorkOrderID } has a last modified date less than or equal to our cutoff time, so ignoring ({ lastModifiedDateUtc } <= { timestampUtc }");
                    continue;
                }

                Exception error = null;
                try
                {
                    await TryProcessWorkItemChanged(workItem);
                }
                catch (Exception err)
                {
                    error = err;
                }

                if (error == null)
                {
                    result.RecordsProcesed.Add(workItem);
                }
                else
                {
                    result.ProcessErrors.Add(new ProcessError()
                    {
                        WorkItem = workItem,
                        ErrorMessage = error.Message
                    });
                }
                count++;
            }
            TimeSpan avg = count > 0 ? watch.Elapsed / count : TimeSpan.Zero;
            _logger.Information($"Processed { count } work item changes in { watch.Elapsed }. Average = { avg.TotalMilliseconds }ms/record ");
        }

        protected virtual async Task<WorkOrderUpdatedEventArgs> TryProcessWorkItemChanged(
            OutputMapping1GetListValues workItem)
        {
            // we need to convert the Assignee 3+3 to an email so Octava can use it
            // from manual inspection in looks like this field is the "assignee 3+3":
            string assignee3and3 = workItem.ASLOGID;

            PersonData assignee = null;
            if (string.IsNullOrWhiteSpace(assignee3and3))
            {
                _logger.Information("Assignee is empty");
            }
            else
            {
                _logger.Information("Looking up assignee with 3+3 {User3and3}", assignee3and3);
                try { assignee = await _peopleService.GetPersonAsync(assignee3and3); }
                catch (Exception err) { _logger.Warning(err, "Unable to get email for Remedy Work Order Assignee {User3and3}: {ErrorMessage}", assignee3and3, err.Message); }
            }

            InitiativeStatus? newInitiativeStatus = GetInitiativeStatusForRemedyStatus(workItem.Status);
            if (newInitiativeStatus == null)
            {
                _logger.Information("Abondining updated work item because an appropriate InitiativeStatus could not be determined from the Remedy Status {WorkItemStatus}", workItem.Status);
                return null;
            }

            DateTime? etaUtc = null;

            try
            {
                etaUtc = await _initiativeStatusEtaService.GetStatusEtaFromNowUtcAsync(newInitiativeStatus.Value);
            }
            catch (Exception err) { _logger.Warning(err, "Unable to get an updated ETA for initiative status {InitiativeStatus}: {ErrorMessage}", newInitiativeStatus.Value, err.Message); }

            try
            {
                // Note the ToUniversalTime on the Last_Modified_Date:
                // this works because this service runs in the same time zone as Remedy.
                var args = new WorkOrderUpdatedEventArgs()
                {
                    WorkOrderId = workItem.InstanceId,
                    UpdatedDateUtc = workItem.Last_Modified_Date.ToUniversalTime(),
                    RemedyStatus = workItem.Status.ToString(),
                    UpdatedStatus = newInitiativeStatus.Value.ToString(),
                    AssigneeEmail = assignee?.Email,
                    AssigneeDisplayName = assignee?.DisplayName,
                    EtaUtc = etaUtc
                };
                await _initiativeMessageSender.SendWorkOrderUpdatedAsync(args);
                return args;
            }
            catch (Exception e)
            {
                Guid correlationId = Guid.NewGuid();
                _logger.Error(e, $"Unable to process work item changed (correlationId {correlationId}): {e.Message}");
                _logger.Debug($"Work item change that caused processing error (correlationId {correlationId}): { workItem }");
                throw;
            }
        }


        protected virtual async Task<Initiative> GetInitiativeForWorkOrderId(string workOrderId)
        {
            return await _initiativeRepository.GetInitiativeByWorkOrderIdAsync(workOrderId);
        }


        protected virtual InitiativeStatus? GetInitiativeStatusForRemedyStatus(StatusType remedyStatusType)
        {
            // here we have the business logic of translating Remedy statuses into our statuses
            InitiativeStatus newIdeaStatus;
            switch (remedyStatusType)
            {
                case StatusType.Assigned:
                    newIdeaStatus = InitiativeStatus.Submit;
                    break;
                case StatusType.Cancelled:
                    newIdeaStatus = InitiativeStatus.Cancelled;
                    break;
                case StatusType.Planning:
                    newIdeaStatus = InitiativeStatus.Review;
                    break;
                case StatusType.InProgress:
                    newIdeaStatus = InitiativeStatus.Collaborate;
                    break;
                case StatusType.Completed:
                    newIdeaStatus = InitiativeStatus.Deliver;
                    break;
                case StatusType.Closed:
                case StatusType.Pending:
                case StatusType.Rejected:
                case StatusType.WaitingApproval:
                default:
                    return null; // no change
            }
            return newIdeaStatus;
        }
    }
}
