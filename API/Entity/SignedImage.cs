using System.ComponentModel.DataAnnotations;

namespace API.Entity
{
    public class SignedImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public byte[] ImageData { get; set; }
        public byte[] StrippedData { get; set; }
        public string? Signature { get; set; }

        //public int UserId { get; set; }
        //[ForeignKey("UserId")]
        //public AppUser User { get; set; }
    }
}
