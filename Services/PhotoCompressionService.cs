using SkiaSharp;

namespace Ticket.Services
{
    public class PhotoCompressionService : IPhotoCompressionService
    {
        private readonly string _cacheDir;

        public PhotoCompressionService()
        {
            _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "photo_cache");
            if (!Directory.Exists(_cacheDir))
                Directory.CreateDirectory(_cacheDir);
        }

        public async Task<string?> CompressPhotoAsync(string sourceFilePath, string destinationFileName, int maxWidth = 1024, int maxHeight = 1024, int quality = 80)
        {
            if (!File.Exists(sourceFilePath))
                return null;

            try
            {
                return await Task.Run(() =>
                {
                    using var inputStream = File.OpenRead(sourceFilePath);
                    using var bitmap = SKBitmap.Decode(inputStream);

                    if (bitmap == null)
                        return null;

                    // Center-crop to square before resize
                    int cropSize = Math.Min(bitmap.Width, bitmap.Height);
                    int cropX = (bitmap.Width - cropSize) / 2;
                    int cropY = (bitmap.Height - cropSize) / 2;
                    var cropRect = new SKRectI(cropX, cropY, cropX + cropSize, cropY + cropSize);
                    using var croppedBitmap = new SKBitmap(cropSize, cropSize);
                    bitmap.ExtractSubset(croppedBitmap, cropRect);

                    var (newWidth, newHeight) = CalculateDimensions(croppedBitmap.Width, croppedBitmap.Height, maxWidth, maxHeight);

                    var newInfo = new SKImageInfo(newWidth, newHeight);
                    var sampling = new SKSamplingOptions(SKFilterMode.Linear);
                    using var scaledBitmap = croppedBitmap.Resize(newInfo, sampling);
                    if (scaledBitmap == null)
                        return null;

                    // Encode to file
                    var outputPath = Path.Combine(_cacheDir, destinationFileName);
                    using var image = SKImage.FromBitmap(scaledBitmap);
                    using var fileStream = File.Create(outputPath);
                    image.Encode(SKEncodedImageFormat.Jpeg, quality).SaveTo(fileStream);

                    return outputPath;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Photo compression failed: {ex.Message}");
                return null;
            }
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return 0;

            return await Task.Run(() =>
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            });
        }

        private (int width, int height) CalculateDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            var aspectRatio = (double)originalWidth / originalHeight;

            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
                return (originalWidth, originalHeight);

            int newWidth, newHeight;

            if (aspectRatio > 1) // Landscape
            {
                newWidth = maxWidth;
                newHeight = (int)(maxWidth / aspectRatio);
            }
            else // Portrait or square
            {
                newHeight = maxHeight;
                newWidth = (int)(maxHeight * aspectRatio);
            }

            return (newWidth, newHeight);
        }
    }
}
