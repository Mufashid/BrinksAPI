namespace BrinksAPI.Models
{
    public class Response
    {
        public string? RequestId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }

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
