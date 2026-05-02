using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexus.Application.Constants
{
    public class AppConstants
    {
    }

    public class CustomStatusCodes
    {
        public const int NotFound = 2000;
        public const int PaymentIssue = 2100;
        public const int RefundIssue = 2200;
        public const int ExistIssue = 2300;
    }

    public static class InspectionSlotConstants
    {
        public const int AvailabilityWindowDays = 90;
        public const int AvailableSlotsMaxResults = 50;
    }
}