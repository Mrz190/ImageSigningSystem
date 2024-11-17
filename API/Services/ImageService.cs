using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp;
using System.Security.Cryptography;

namespace API.Services
{
    public class ImageService
    {
        private readonly string _privateKey;
        private readonly string _publicKey;

        public ImageService()
        {
            string privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "private.key");
            _privateKey = System.IO.File.ReadAllText(privateKeyPath);

            string publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "public.key");
            _publicKey = System.IO.File.ReadAllText(publicKeyPath);
        }

        // Extracting signature method
        public string ExtractSignatureFromImageMetadata(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                var exifProfile = image.Metadata.ExifProfile;
                if (exifProfile != null)
                {
                    var userComment = exifProfile.GetValue(ExifTag.UserComment);
                    return userComment?.Value.ToString();
                }
            }
            return null;
        }

        // Checking signature method
        public bool VerifySignature(byte[] imageData, string signature)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportFromPem(_publicKey.ToCharArray());

                    byte[] imageHash = SHA256.Create().ComputeHash(imageData);
                    byte[] signatureBytes = Convert.FromBase64String(signature);

                    return rsa.VerifyData(imageHash, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        // Signing image with primary key method
        public string SignImageData(byte[] imageData)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(_privateKey.ToCharArray());

                byte[] imageHash = SHA256.Create().ComputeHash(imageData);

                // Hash signature
                byte[] signature = rsa.SignData(imageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Convert into Base64
                return Convert.ToBase64String(signature);
            }
        }

        // Adding signature into Exif of metadata method
        public byte[] AddSignatureToImageMetadata(byte[] imageData, string signature)
        {
            using (var image = Image.Load(imageData))
            {
                var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();
                image.Metadata.ExifProfile = exifProfile;

                // Add signature to tag UserComment
                exifProfile.SetValue(ExifTag.UserComment, signature);

                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);
                    return ms.ToArray();
                }
            }
        }

        // Removing Exif before hashing merthod
        public byte[] StripExif(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                image.Metadata.ExifProfile = null;  // Removing Exif
                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
