﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Services;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ExposureKeySetsEngine
{
    public class SnapshotEksInputMk1 : ISnapshotEksInput
    {
        private readonly ILogger<SnapshotEksInputMk1> _Logger;

        private readonly ITransmissionRiskLevelCalculation _TransmissionRiskLevelCalculation;
        private readonly IUtcDateTimeProvider _DateTimeProvider; //Being used as a stopwatch...

        private readonly WorkflowDbContext _WorkflowDbContext;
        private readonly Func<PublishingJobDbContext> _PublishingDbContextFactory;

        public SnapshotEksInputMk1(ILogger<SnapshotEksInputMk1> logger, ITransmissionRiskLevelCalculation transmissionRiskLevelCalculation, IUtcDateTimeProvider dateTimeProvider, WorkflowDbContext workflowDbContext, Func<PublishingJobDbContext> publishingDbContextFactory)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _TransmissionRiskLevelCalculation = transmissionRiskLevelCalculation ?? throw new ArgumentNullException(nameof(transmissionRiskLevelCalculation));
            _DateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _WorkflowDbContext = workflowDbContext ?? throw new ArgumentNullException(nameof(workflowDbContext));
            _PublishingDbContextFactory = publishingDbContextFactory ?? throw new ArgumentNullException(nameof(publishingDbContextFactory));
        }

        private DateTime _SnapshotStart;

        public async Task<SnapshotEksInputResult> Execute(DateTime snapshotStart)
        {
            _Logger.LogDebug("Snapshot publishable TEKs.");

            _SnapshotStart = snapshotStart;

            var stopwatchStart = _DateTimeProvider.Now();

            const int pagesize = 10000;
            var index = 0;

            using var tx = _WorkflowDbContext.BeginTransaction();
            var page = ReadTeksFromWorkflow(index, pagesize);

            while (page.Count > 0)
            {
                var db = _PublishingDbContextFactory();
                await db.BulkInsertAsync2(page.ToList(), new SubsetBulkArgs());

                index += page.Count;
                page = ReadTeksFromWorkflow(index, pagesize);
            }

            var result = new SnapshotEksInputResult
            {
                SnapshotSeconds = (_DateTimeProvider.Now() - stopwatchStart).TotalSeconds,
                TekInputCount = index
            };

            _Logger.LogInformation($"TEKs to publish - Count:{index}.");

            return result;
        }

        private IList<EksCreateJobInputEntity> ReadTeksFromWorkflow(int index, int pageSize)
        {
            var temp = _WorkflowDbContext.TemporaryExposureKeys
                .Where(x => (x.Owner.AuthorisedByCaregiver != null)
                            && x.Owner.DateOfSymptomsOnset != null
                            && x.PublishingState == PublishingState.Unpublished
                            && x.PublishAfter <= _SnapshotStart
                )
                .Skip(index)
                .Take(pageSize)
                .Select(x => new {
                    x.Id,
                    D = x.KeyData,
                    S = x.RollingStartNumber,
                    //P = x.RollingPeriod, //iOS xxx requires all RP to be 144
                    DateOfSymptomsOnset = x.Owner.DateOfSymptomsOnset.Value
                }).ToList();

            var result = temp
                .Select(x => new EksCreateJobInputEntity
                {
                    TekId = x.Id,
                    RollingStartNumber = x.S,
                    RollingPeriod = 144, //TODO constant cos iOS xxx requires all RP to be 144
                    KeyData = x.D,
                    TransmissionRiskLevel = _TransmissionRiskLevelCalculation.Calculate(x.S, x.DateOfSymptomsOnset),
                }).ToList();

            return result;
        }
    }
}