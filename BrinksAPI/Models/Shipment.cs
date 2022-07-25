using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    public class Shipment
    {
        #region HISTORY
        public class History
        {
            public string? RequestId { get; set; }

            [Required]
            [StringLength(16)]
            public string? TrackingNumber { get; set; }
            [StringLength(50)]
            public string? UserId { get; set; }
            [Required]
            [StringLength(5)]
            public string? ActionType { get; set; }
            [StringLength(5)]
            public string? AreaType { get; set; }
            [Required]
            [StringLength(2000)]
            public string? HistoryDetails { get; set; }
            [Required]
            public string? HistoryDate { get; set; }
            [Required]
            [StringLength(4)]
            public string? SiteCode { get; set; }
            [Required]
            [StringLength(5)]
            public string? ServerId { get; set; }
            [StringLength(11)]
            [Required]
            public string? HawbNumber { get; set; }
        } 

        public enum ActionType
        {
            PICK,
            DLVD
        }
        #endregion
    }
}
