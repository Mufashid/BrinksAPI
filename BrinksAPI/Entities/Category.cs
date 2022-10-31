using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? BrinksCode { get; set; }
        public string? BrinksDescription { get; set; }
        [Required]
        public string? CWCode { get; set; }
        public string? CWDescrption{ get; set; }
    }
}
