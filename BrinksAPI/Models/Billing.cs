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
            public string? site_id { get; set; }
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

        public class PayableInvoice
        {
            [Required]
            [StringLength(20)]
            public string? invoice_number { get; set; }
            [Required]
            [StringLength(20)]
            public string? invoice_gcc { get; set; }
            [Required]
            public string? invoice_date { get; set; }
            [Required]
            public string? exchange_date { get; set; }
            [Required]
            [StringLength(4)]
            public string? currency_code { get; set; }
            [Required]
            [StringLength(40)]
            public string? exchange_rate { get; set; }
            [Required]
            [StringLength(4)]
            public string? origin_site_code { get; set; }
            public  List<PayableRevenue>? revenues { get; set; }

        }
        public class PayableRevenue
        {
            [Required]
            [StringLength(10)]
            public string? category_code { get; set; }
            [Required]
            [StringLength(50)]
            public string? description { get; set; }
            [Required]
            [StringLength(4)]
            public string? invoice_currency { get; set; }
            [Required]
            [Precision(18, 2)]
            public string? invoice_amount { get; set; }
            [Required]
            [Precision(18, 2)]
            public string? invoice_tax_amount { get; set; }
            [Required]
            [StringLength(20)]
            public string? tax_code { get; set; }
            [Required]
            [StringLength(4)]
            public string? billed_from_site_code { get; set; }
            [Required]
            [StringLength(11)]
            public string? revenue_hawb_number { get; set; }
        }

    }
}
