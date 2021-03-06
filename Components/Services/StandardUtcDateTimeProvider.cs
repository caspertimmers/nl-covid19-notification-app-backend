﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Services
{
    public class StandardUtcDateTimeProvider : IUtcDateTimeProvider
    {
        public StandardUtcDateTimeProvider()
        {
            TakeSnapshot();
        }

        public DateTime Now() => DateTime.UtcNow;

        public DateTime TakeSnapshot()
        {
            Snapshot = Now();
            return Snapshot;
        }

        public DateTime Snapshot { get; private set; }
    }
}
