using System;
using System.Collections.Generic;
using System.Text;

namespace Payment.Application.DTO
{
    public class ProcessPaymentRepository
    {
        public string BookingId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string Currency { get; set; } = "ARS";
    }
}
