using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp;
using System.Security.Cryptography;
using API.Entity;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Intefaces;
using API.Dto;
using AutoMapper;
using SixLabors.ImageSharp.Formats.Jpeg;

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
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                imageDtos = images.Select(img => new ImageForAdminDto
                {
                    Id = img.Id,
                    ImageName = img.ImageName,
                    Status = img.Status.ToString(),
                    UserName = img.UploadedBy
                }).ToList();
                return imageDtos;
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
               .ToListAsync();

            if (images != null && images.Count != 0)
            {
                imageDtos = images.Select(img => new ImageForSupportDto
                {
                    Id = img.Id,
                    ImageName = img.ImageName,
                    Status = img.Status.ToString(),
                    UserName = img.UploadedBy
                }).ToList();
                return imageDtos;
            }
            return null;
        }

        public async Task<bool> SignatureOperation(SignedImage image)
        {
            Console.WriteLine($"Before adding signature: {image.ImageData.Length} bytes");
            var signature = SignImageData(image.StrippedData);

            // Signature in Exif of original data
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

        public string ExtractSignatureFromJpgMetadata(byte[] imageData)
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

        public string ExtractSignatureFromPngMetadata(byte[] imageData)
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

        public async Task<byte[]> ConvertToByteArrayAsync(IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
                return null;

            using (var memoryStream = new MemoryStream())
            {
                await formFile.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
        
        public string GetFileFormat(byte[] fileBytes)
        {
            if (fileBytes.Take(2).SequenceEqual(new byte[] { 0xFF, 0xD8 }))
                return "JPG";
            if (fileBytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return "PNG";

            return "Unknown";
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
                    image.SaveAsJpeg(ms, new JpegEncoder { Quality = 100 });
                    return ms.ToArray();
                }
            }
        }

        // Removing Exif before hashing merthod
        public byte[] StripExif(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                image.Metadata.ExifProfile = null;

                using (var ms = new MemoryStream())
                {
                    image.SaveAsJpeg(ms, new JpegEncoder { Quality = 100 });
                    return ms.ToArray();
                }
            }
        }

        public byte[] RemoveUserComment(byte[] imageData)
        {
            using (var image = Image.Load(imageData))
            {
                var exifProfile = image.Metadata.ExifProfile;

                if (exifProfile != null)
                {
                    exifProfile.RemoveValue(ExifTag.UserComment);
                }

                using (var ms = new MemoryStream())
                {
                    image.SaveAsJpeg(ms, new JpegEncoder { Quality = 100 });
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

            if(images.Count != 0)
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