using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.Events
{
    public sealed record PaymentResult(
     Guid OrderId,
     string PaymentId,
     string Status,
     decimal Amount,
     DateTime ProcessedAtUtc,
     string? FailureReason);
}
