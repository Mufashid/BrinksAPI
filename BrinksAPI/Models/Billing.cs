using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    public class Billing
    {
        public class Revenue
        {
            [Required]
            [StringLength(20)]
            public string? customer_gcc { get; set; }
            [Required]
            [StringLength(10)]
            public string? category_code { get; set; }
            [Required]
            [StringLength(20)]
            public string? tax_code { get; set; }
            [Required]
            [StringLength(50)]
            public string? description { get; set; }
            [Required]
            [Precision(18, 2)]
            public decimal invoice_amount { get; set; }
            [Required]
            [Precision(18, 2)]
            public decimal invoice_tax_amount { get; set; }
            [Required]
            [StringLength(11)]
            public string? hawb_number { get; set; }
        }
    }
}
