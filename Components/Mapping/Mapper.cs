﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Mapping
{
    public static class Mapper
    {
 public static Tek MapToTek(this PostTeksItemArgs value)
        {
            return new Tek
            {
                KeyData = Convert.FromBase64String(value.KeyData),
                RollingPeriod = value.RollingPeriod,
                RollingStartNumber = value.RollingStartNumber,
            };
        }

        //Read
        public static Tek? MapToTek(this TekEntity? value)
        {
            if (value == null)
                return null;

            return new Tek
            {
                KeyData = value.KeyData,
                RollingPeriod = value.RollingPeriod,
                RollingStartNumber = value.RollingStartNumber,
                PublishingState = value.PublishingState,
                PublishAfter  = value.PublishAfter,
                Region = value.Region
            };
        }

        public static TekEntity MapToEntity(Tek value)
        {
            return new TekEntity
            {
                KeyData = value.KeyData,
                RollingPeriod = value.RollingPeriod,
                RollingStartNumber = value.RollingStartNumber,
                PublishingState = value.PublishingState,
                PublishAfter = value.PublishAfter,
                Region = value.Region
            };
        }
    }
}