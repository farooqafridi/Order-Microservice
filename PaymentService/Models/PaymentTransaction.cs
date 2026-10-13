using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.Models
{
    public sealed class PaymentTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrderId { get; set; }

        public string PaymentId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = "Succeeded";

        public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;

        public string? FailureReason { get; set; }
    }
}
