using SixLabors.ImageSharp;
using System.Security.Cryptography;
using API.Entity;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Dto;
using AutoMapper;
using API.Interfaces;
using SixLabors.ImageSharp.Formats.Png;

namespace API.Services
{
    public class ImageService
    {
        private readonly string _privateKey;
        private readonly string _publicKey;
        private readonly DataContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ImageService(DataContext context, IUnitOfWork unitOfWork, IMapper mapper)
        {
            string privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "private.key");
            _privateKey = System.IO.File.ReadAllText(privateKeyPath);

            string publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "public.key");
            _publicKey = System.IO.File.ReadAllText(publicKeyPath);

            _context = context;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<ImageForAdminDto?>> GetAdminImages()
        {
            List<ImageForAdminDto> imageDtos;
            var images = await _context.SignedImages
               .Where(img => img.Status == ImageStatus.PendingAdminSignature.ToString())
               .Select(img => new ImageForAdminDto
               {
                   Id = img.Id,
                   ImageName = img.ImageName,
                   Status = img.Status.ToString(),
                   UserName = img.UploadedBy
               })
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                return images;
            }

            return null;
        }

        public async Task<bool> RejectSigningImage(SignedImage image)
        {
            image.Status = ImageStatus.Rejected.ToString();

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

            if (!changes.Any())
                return false;

            await _unitOfWork.CompleteAsync();

            return true;
        }

        public async Task<List<ImageForSupportDto?>> GetSupportImages()
        {
            List<ImageForSupportDto> imageDtos;

            var images = await _context.SignedImages
               .Where(img => img.Status == ImageStatus.AwaitingSignature.ToString())
               .Select(img => new ImageForSupportDto
               {
                   Id = img.Id,
                   ImageName = img.ImageName,
                   Status = img.Status.ToString(),
                   UserName = img.UploadedBy
               })
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                return images;
            }
            return null;
        }

        public async Task<bool> SignatureOperation(SignedImage image)
        {
            Console.WriteLine($"Before adding signature: {image.ImageData.Length} bytes");

            var signature = SignImageData(image.StrippedData);

            byte[] signedImageData = AddSignatureToImageMetadata(image.ImageData, signature);

            Console.WriteLine($"After adding signature: {signedImageData.Length} bytes");

            image.ImageData = signedImageData;
            image.Signature = signature;
            image.Status = ImageStatus.Signed.ToString();

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            if (!changes.Any())
                return false;

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<List<ImageDto?>> GetUserImages(int userId)
        {
            List<ImageDto> imageDtos;

            var images = await _context.SignedImages
               .Where(img => img.UserId == userId)
               .Select(img => new ImageDto
               {
                   Id = img.Id,
                   ImageName = img.ImageName,
                   Status = img.Status.ToString()
               })
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                return images;
            }
            return null;
        }

        public byte[] RemoveMetadata(byte[] fileBytes)
        {
            using var image = Image.Load(fileBytes);

            var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);
            if (pngMetadata != null)
            {
                pngMetadata.TextData.Clear(); // Remove text data
            }

            // Save image without metadata
            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, new PngEncoder());
            return memoryStream.ToArray();
        }

        public async Task<List<ImageDto?>> GetSignedImagesForUser(int userId)
        {
            List<ImageDto> imageDtos;

            var images = await _context.SignedImages
               .Where(img => img.UserId == userId && img.Status == ImageStatus.Signed.ToString())
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                imageDtos = images.Select(img => new ImageDto
                {
                    Id = img.Id,
                    ImageName = img.ImageName,
                    Status = img.Status.ToString()
                }).ToList();
                return imageDtos;
            }
            return null;
        }

        public async Task<List<ImageDto?>> GetRejectedImagesForUser(int userId)
        {
            List<ImageDto> imageDtos;

            var images = await _context.SignedImages
               .Where(img => img.UserId == userId && img.Status == ImageStatus.Rejected.ToString())
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                imageDtos = images.Select(img => new ImageDto
                {
                    Id = img.Id,
                    ImageName = img.ImageName,
                    Status = img.Status.ToString()
                }).ToList();
                return imageDtos;
            }
            return null;
        }

        public async Task<bool> UploadSendImageForSigningToSupport(SignedImage image)
        {
            _context.SignedImages.Add(image);

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

            if (!changes.Any())
                return false;

            await _unitOfWork.CompleteAsync();

            return true;
        }

        public async Task<SignedImage> GetImageById(int id)
        {
            var image = await _context.SignedImages.FindAsync(id);

            return image;
        }

        public async Task<object> GetImagesDataBy(int id)
        {
            var image = await _context.SignedImages.Select(img => new { img.Id, img.ImageName, img.Status, img.UploadedBy }).FirstOrDefaultAsync();

            return image;
        }

        // Extracting signature method
        public string ExtractSignatureFromPngMetadata(byte[] fileBytes)
        {
            using var image = Image.Load(fileBytes);
            
            var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);

            if (pngMetadata != null)
            {
                // Find field "Signature"
                return pngMetadata.TextData
                    .FirstOrDefault(t => t.Keyword.Equals("Signature", StringComparison.OrdinalIgnoreCase))
                    .Value;
            }

            return string.Empty;
        }

        public async Task<byte[]> ConvertToByteArrayAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public string GetFileFormat(byte[] fileBytes)
        {
            using var image = Image.Load(fileBytes, out var format);
            return format.Name.ToUpper();
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
                var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);

                if (pngMetadata != null)
                {
                    var existing = pngMetadata.TextData
                        .FirstOrDefault(t => t.Keyword.Equals("Signature", StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                        pngMetadata.TextData.Remove(existing);

                    pngMetadata.TextData.Add(new PngTextData("Signature", signature, null, null));
                }

                using (var ms = new MemoryStream())
                {
                    image.Save(ms, new PngEncoder());
                    return ms.ToArray();
                }
            }
        }

        // Removing Exif before hashing merthod
        public byte[] StripExif(byte[] fileBytes)
        {
            using var image = Image.Load(fileBytes);

            image.Metadata.ExifProfile = null;

            var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);
            pngMetadata?.TextData.Clear();

            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, new PngEncoder());
            return memoryStream.ToArray();
        }

        public byte[] RemoveUserComment(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);

                if (pngMetadata != null)
                {
                    var existing = pngMetadata.TextData
                        .FirstOrDefault(t => t.Keyword.Equals("Signature", StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                        pngMetadata.TextData.Remove(existing);
                }

                using (var ms = new MemoryStream())
                {
                    image.Save(ms, new PngEncoder());
                    return ms.ToArray();
                }
            }
        }

        public async Task<bool> DeleteImage(SignedImage image)
        {
            _context.SignedImages.Remove(image);

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

            if (!changes.Any())
                return false;

            await _unitOfWork.CompleteAsync();

            return true;
        }

        public async Task<bool> DeleteImage(int imageId)
        {
            var image = await GetImageById(imageId);

            _context.SignedImages.Remove(image);

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

            if (!changes.Any())
                return false;

            await _unitOfWork.CompleteAsync();

            return true;
        }

        public async Task<bool> DeleteAllUserImages(int userId)
        {
            var images = await GetAllUserImages(userId);

            if (images.Count != 0)
            {
                foreach (var image in images)
                {
                    _context.Remove(image);
                }
                var changes = _unitOfWork.Context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    .ToList();

                if (!changes.Any()) return false;
                await _unitOfWork.CompleteAsync();
            }

            return true;
        }

        public async Task<List<SignedImage>> GetAllUserImages(int userId)
        {
            return _context.SignedImages.AsNoTracking()
                                        .Where(p => p.UserId == userId)
                                        .ToList();
        }
    }
}