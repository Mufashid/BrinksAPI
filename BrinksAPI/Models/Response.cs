namespace BrinksAPI.Models
{
    public class Response
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Data { get; set; }
        public string? RequestId { get; set; }
    }
    public class OrganizationResponse
    {
        public string? RequestId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public Organization? Data { get; set; }

    }
    public class MawbResponse
    {
        public string? RequestId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public Mawb? Data { get; set; }

    }
    public class DocumentResponse
    {
        public string? RequestId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public BrinksDocument? Data { get; set; }

    }
    public class ShipemtHistoryResponse
    {
        public string? RequestId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public Shipment.History? Data { get; set; }
    }
}
