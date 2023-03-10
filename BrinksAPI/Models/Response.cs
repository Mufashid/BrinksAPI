using System.Text.Json;

namespace BrinksAPI.Models
{
    public class Response
    {
        public string? RequestId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }

    }
    public class ErrorResponse
    {
        public int Status { get; set; }
        public string? Message { get; set; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

    }
    public class ShipemtResponse
    {
        public string? HawbNum { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
    public class RevenueResponse
    {
        public string? HawbNum { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }

    public class PayableInvoiceResponse
    {
        public string? InvoiceNum { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
}
