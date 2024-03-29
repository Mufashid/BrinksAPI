﻿using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    public class Mawb
    {
        public string? requestId { get; set; }
        [Required]
        [StringLength(13)]
        public string? mawbNumber { get; set; }
        [Required]
        [StringLength(800)]
        public string? historyDetails { get; set; }
        [Required]
        public string? historyDate { get; set; }
        [StringLength(5)]
        public string? serverId { get; set; }
        [Required]
        [StringLength(3)]
        public string? historyCode { get; set; }
    }
}
