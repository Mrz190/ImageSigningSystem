using API.Data;
using API.Entity;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Security.Cryptography;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly DataContext _context;

        private readonly string _privateKey;
        private readonly string _publicKey;

        public ImageController(DataContext context, IConfiguration configuration)
        {
            _context = context;

            string privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "private.key");
            _privateKey = System.IO.File.ReadAllText(privateKeyPath);

            string publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "public.key");
            _publicKey = System.IO.File.ReadAllText(publicKeyPath);
        }



        // Метод для загрузки изображения
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Копируем файл в память
                    await file.CopyToAsync(memoryStream);
                    var imageData = memoryStream.ToArray();

                    var user = await _context.Users.FindAsync(User.Identity.Name);
                    var signedImage = new SignedImage
                    {
                        ImageData = imageData
                    };

                    _context.SignedImages.Add(signedImage);
                    await _context.SaveChangesAsync();

                    return Ok("Изображение успешно загружено.");
                }
            }

            return BadRequest("Изображение не предоставлено.");
        }

        // Метод для подписания изображения
        [HttpPost("sign/{imageId}")]
        public async Task<IActionResult> SignImage(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            var signedImage = SignImageData(image.ImageData);

            byte[] signedImageData = AddSignatureToImageMetadata(image.ImageData, signedImage);

            image.ImageData = signedImageData;
            image.Signature = signedImage;
            await _context.SaveChangesAsync();

            return Ok("Изображение подписано и метаданные обновлены.");
        }

        // Метод для добавления подписи в метаданные изображения (Exif)
        private byte[] AddSignatureToImageMetadata(byte[] imageData, string signature)
        {
            using (var image = Image.Load(imageData))
            {
                var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();
                image.Metadata.ExifProfile = exifProfile;

                var encodedSignature = new EncodedString(signature);
                exifProfile.SetValue(ExifTag.UserComment, encodedSignature);

                using (var memoryStream = new MemoryStream())
                {
                    image.SaveAsJpeg(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        // Метод для подписи изображения с использованием приватного ключа
        private string SignImageData(byte[] imageData)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(_privateKey.ToCharArray());

                byte[] imageHash = SHA256.Create().ComputeHash(imageData);

                // Подпись хеша изображения
                byte[] signature = rsa.SignData(imageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Конверт в Base64
                return Convert.ToBase64String(signature);
            }
        }

        // Метод для скачивания изображения
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            var image = await _context.SignedImages.FindAsync(id);

            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            return File(image.ImageData, "image/jpeg");
        }

        // Метод для проверки подписи изображения
        [HttpPost("verify-signature/{imageId}")]
        public async Task<IActionResult> VerifyImageSignature(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);

            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            bool isValid = VerifyImageSignature(image.ImageData, image.Signature);

            if (isValid)
            {
                return Ok("Подпись верна.");
            }
            else
            {
                return BadRequest("Подпись неверна.");
            }
        }
            private bool VerifyImageSignature(byte[] imageData, string signature)
            {
                try
                {
                    using (var rsa = RSA.Create())
                    {
                        rsa.ImportFromPem(_publicKey.ToCharArray());

                        byte[] imageHash = SHA256.Create().ComputeHash(imageData);
                        Console.WriteLine($"Computed Image Hash: {BitConverter.ToString(imageHash)}");

                        byte[] signatureBytes = Convert.FromBase64String(signature);
                        Console.WriteLine($"Signature to verify: {BitConverter.ToString(signatureBytes)}");

                        bool isValid = rsa.VerifyData(imageHash, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        Console.WriteLine($"Signature valid: {isValid}");

                        return isValid;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return false;
                }
            }



        // Метод для извлечения подписи из метаданных изображения
        [HttpGet("get-signature/{imageId}")]
        public async Task<IActionResult> GetSignatureFromImageMetadata(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            var signature = ExtractSignatureFromImageMetadata(image.ImageData);

            if (string.IsNullOrEmpty(signature))
            {
                return NotFound("Подпись не найдена в метаданных.");
            }

            return Ok(signature);
        }

        // Метод для извлечения подписи из метаданных изображения
        private string ExtractSignatureFromImageMetadata(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                var exifProfile = image.Metadata.ExifProfile;

                if (exifProfile != null)
                {
                    var userComment = exifProfile.GetValue(ExifTag.UserComment);
                    if (userComment != null)
                    {
                        return userComment.Value.ToString();
                    }
                }
            }

            return null;
        }
    }
}
