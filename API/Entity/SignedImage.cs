using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entity
{
    public enum ImageStatus
    {
        AwaitingSignature,
        PendingAdminSignature,
        Signed,
        Rejected
    }

    public class SignedImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImageName { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        [Required]
        public byte[] StrippedData { get; set; }

        public string? Signature { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public string Status { get; set; } = ImageStatus.AwaitingSignature.ToString();

        public string UploadedBy { get; set; }
    }
}
