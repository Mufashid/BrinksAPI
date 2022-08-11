﻿using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class TransportMode
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? BrinksCode { get; set; }
        [Required]
        public string? CWCode { get; set; }
    }
}
