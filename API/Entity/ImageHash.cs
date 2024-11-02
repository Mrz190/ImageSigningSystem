using System.ComponentModel.DataAnnotations;

namespace API.Entity
{
    public class ImageHash
    {
        [Required]
        public int IdImage { get; set; }

        [Required]
        public string Hash { get; set; }
        
        [Required]
        public string HashedBy { get; set; }

        [Required]
        public DateTime HashTime { get; set; }
    }
}
