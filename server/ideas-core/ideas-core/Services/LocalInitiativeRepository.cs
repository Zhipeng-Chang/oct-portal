﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoE.Ideas.Core.Data;
using CoE.Ideas.Shared;
using CoE.Ideas.Shared.Data;
using CoE.Ideas.Shared.Security;
using EnsureThat;
using Microsoft.EntityFrameworkCore;

namespace CoE.Ideas.Core.Services
{
    internal class LocalInitiativeRepository : IInitiativeRepository, IHealthCheckable
    {
        public LocalInitiativeRepository(InitiativeContext initiativeContext,
            Serilog.ILogger logger)
        {
            EnsureArg.IsNotNull(initiativeContext);
            EnsureArg.IsNotNull(logger);

            _initiativeContext = initiativeContext;
            _logger = logger;
        }

        private readonly InitiativeContext _initiativeContext;
        private readonly Serilog.ILogger _logger;


        public async Task<Initiative> AddInitiativeAsync(Initiative initiative,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureArg.IsNotNull(initiative);

            _logger.Debug("Adding to Ideas database");
            _initiativeContext.Initiatives.Add(initiative);
            await _initiativeContext.SaveChangesAsync(cancellationToken);

            return initiative;
        }

        public Task<Initiative> GetInitiativeAsync(Guid id)
        {
            return _initiativeContext.Initiatives
                .Include(x => x.StatusHistories)
                .SingleOrDefaultAsync(x => x.Uid == id);
        }

        public Task<Initiative> GetInitiativeAsync(int id)
        {
            return _initiativeContext.Initiatives
				.Include(x => x.StatusHistories)
				.SingleOrDefaultAsync(x => x.Id == id);
        }

        private static IQueryable<InitiativeInfo> CreateInitiativeInfoQuery(IQueryable<Initiative> query, int pageNumber, int pageSize)
        {
            return query
                .Include(x => x.StatusHistories)
				.Skip((pageNumber - 1)*pageSize)
				.Take(pageSize)
				.OrderByDescending(x => x.CreatedDate)
				.Select(x => InitiativeInfo.Create(x));
        }

        public async Task<PagedResultSet<InitiativeInfo>> GetInitiativesAsync(int pageNumber, int pageSize)
        {
            var initiatives = await CreateInitiativeInfoQuery(_initiativeContext.Initiatives, pageNumber, pageSize)
                .ToListAsync();

            var initiativeCount = await _initiativeContext.Initiatives.CountAsync();

			return PagedResultSet.Create(initiatives, pageNumber, pageSize, initiatives.Count(), initiativeCount);
        }


        public async Task<PagedResultSet<InitiativeInfo>> GetInitiativesByStakeholderPersonIdAsync(int personId, int pageNumber, int pageSize)
        {
            var query = _initiativeContext.Initiatives
                    .Where(x => x.Stakeholders.Any(y => y.PersonId == personId));

            var initiatives = await(CreateInitiativeInfoQuery(query, pageNumber, pageSize))
                .ToListAsync();

            var initiativeCount = await query.CountAsync();

            return PagedResultSet.Create(initiatives, pageNumber, pageSize, initiatives.Count(), initiativeCount);
        }

        public async Task<Initiative> UpdateInitiativeAsync(Initiative initiative)
        {
            _initiativeContext.Entry(initiative).State = EntityState.Modified;
            await _initiativeContext.SaveChangesAsync();
            return initiative;
        }

        public Task<Initiative> GetInitiativeByWorkOrderIdAsync(string workOrderId)
        {
            return _initiativeContext.Initiatives
                .Include(x => x.StatusHistories)
                .FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId);
        }

        Task<IDictionary<string, object>> IHealthCheckable.HealthCheckAsync()
        {
            IDictionary<string, object> returnValue = new Dictionary<string, object>();
            System.Data.Common.DbConnection dbConnection = null;
            try { dbConnection = _initiativeContext?.Database?.GetDbConnection(); }
            catch (Exception err) { returnValue["dbError"] = err.Message; }

            if (dbConnection != null)
            {
                try { returnValue["host"] = dbConnection.DataSource; } catch (Exception err) { returnValue["host"] = err.Message; }
                try { returnValue["database"] = dbConnection.Database; } catch (Exception err) { returnValue["database"] = err.Message; }

                try
                {
                    _initiativeContext.Database.OpenConnection();
                    try { returnValue["serverVersion"] = dbConnection.ServerVersion; } catch (Exception err) { returnValue["serverVersion"] = err.Message; }

                    // The following should produce the same as serverVersion, but to be sure we'll run it again.
                    // Also, we'll time it and supply that result

                    using (var cmd = dbConnection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT @@VERSION";
                        var watch = new Stopwatch();
                        watch.Start();
                        var result = cmd.ExecuteScalar();
                        watch.Stop();
                        returnValue["version"] = result;
                        returnValue["pingMilliseconds"] = watch.Elapsed.TotalMilliseconds;

                        cmd.CommandText = "SELECT TOP (1) [MigrationId] FROM[CoeIdeas].[dbo].[__EFMigrationsHistory] ORDER BY MigrationId DESC";
                        returnValue["CurrentMigration"] = cmd.ExecuteScalar();
                    }
                }
                catch (Exception) { /* eat the exception */ }
                finally
                {
                    _initiativeContext.Database.CloseConnection();
                }
            }

            return Task.FromResult(returnValue);
        }

        public Task<Initiative> GetInitiativeByApexId(int apexId)
        {
            return _initiativeContext.Initiatives.FirstOrDefaultAsync(x => x.ApexId == apexId);
        }
    }
}
