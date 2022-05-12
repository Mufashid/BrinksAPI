using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrinksAPI.Entities
{
    public class AuthenticationLevel
    {
        [Key]
        public int AuthId { get; set; }
        [Required]
        public string? AuthName { get; set; }
        [ForeignKey("AuthLevelRefId")]
        public ICollection<User>? Users { get; set; }
    }
}
