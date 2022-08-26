using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrinksAPI.Entities
{
    public class TransportBooking
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        public string? ShipmentNumber { get; set; }
        [Required]
        public string? TBNumber { get; set; }
        [Required]
        public string? HawbNumber { get; set; }

    }
}
