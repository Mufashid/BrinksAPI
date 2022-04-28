using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    public class DB
    {
        public class ServiceLevel
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [MaxLength(10)]
            public string CWCode { get; set; }

            [Required]
            [MaxLength(10)]
            public string BrinksCode { get; set; }

            [MaxLength(30)]
            public string Description { get; set; }

        }
    public class DocumentType
        {
            [Key]
            public int Id { get; set; }
            [Required]
            public string BrinksCode { get; set; }
            [Required]
            public string CWCode { get; set; }
        }
    }
}
