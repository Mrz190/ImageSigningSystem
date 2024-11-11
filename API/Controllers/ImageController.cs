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
                    await file.CopyToAsync(memoryStream);
                    var originalImageData = memoryStream.ToArray();

                    // Удаляем Exif перед подписанием
                    byte[] strippedImageData = StripExif(originalImageData);

                    var signedImage = new SignedImage
                    {
                        ImageData = originalImageData,
                        StrippedData = strippedImageData, // Оригинальные данные без Exif для подписи и проверки
                        Signature = null
                    };

                    _context.SignedImages.Add(signedImage);
                    await _context.SaveChangesAsync();

                    return Ok("Изображение успешно загружено.");
                }
            }

            return BadRequest("Изображение не предоставлено.");
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

            return File(image.ImageData, "image/png");
        }

        // Метод для скачивания оригинала изображения
        [HttpGet("download-without-exif/{id}")]
        public async Task<IActionResult> DownloadImageWithoutExif(int id)
        {
            var image = await _context.SignedImages.FindAsync(id);
            
            if (image == null) return NotFound("Изображение не найдено.");

            return File(image.StrippedData, "image/png");
        }

        // Метод для создания подписи и добавления её в Exif
        [HttpPost("sign/{imageId}")]
        public async Task<IActionResult> SignImage(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            // Подпись на данных без Exif
            var signature = SignImageData(image.StrippedData);

            // Подпись в Exif оригинальных данных
            byte[] signedImageData = AddSignatureToImageMetadata(image.ImageData, signature);

            image.ImageData = signedImageData;
            image.Signature = signature;

            await _context.SaveChangesAsync();

            return Ok("Изображение подписано и метаданные обновлены.");
        }

        // Метод для удаления Exif перед хешированием
        private byte[] StripExif(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                image.Metadata.ExifProfile = null;  // Убираем Exif
                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms); 
                    return ms.ToArray();
                }
            }
        }

        // Метод для проверки подписи
        [HttpPost("verify-signature/{imageId}")]
        public async Task<IActionResult> VerifyImageSignature(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            // Извлекаем подпись из метаданных
            var signature = ExtractSignatureFromImageMetadata(image.ImageData);
            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest("Подпись не найдена в метаданных.");
            }

            // Проверяем подпись на оригинальных данных без Exif
            bool isValid = VerifySignature(image.StrippedData, signature);
            return isValid ? Ok("Подпись верна.") : BadRequest("Подпись неверна.");
        }

        // Метод для проверки подписи
        private bool VerifySignature(byte[] imageData, string signature)
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

        // Метод для добавления подписи в Exif метаданные
        private byte[] AddSignatureToImageMetadata(byte[] imageData, string signature)
        {
            using (var image = Image.Load(imageData))
            {
                var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();
                image.Metadata.ExifProfile = exifProfile;

                // Добавляем подпись в тег UserComment
                exifProfile.SetValue(ExifTag.UserComment, signature);

                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);  // Или используйте нужный формат
                    return ms.ToArray();
                }
            }
        }

        // Метод для извлечения подписи из Exif метаданных
        private string ExtractSignatureFromImageMetadata(byte[] imageData)
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
    }
}
