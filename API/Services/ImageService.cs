using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp;
using System.Security.Cryptography;
using API.Entity;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Intefaces;
using API.Dto;
using AutoMapper;

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
            var signature = SignImageData(image.StrippedData);

            // Signature in Exif of original data
            byte[] signedImageData = AddSignatureToImageMetadata(image.ImageData, signature);

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

        public async Task<bool> DeleteImage(int id)
        {
            var image = await GetImageById(id);

            _context.SignedImages.Remove(image);

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

            if (!changes.Any())
                return false;

            await _unitOfWork.CompleteAsync();

            return true;
        }
    }
}
