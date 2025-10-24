using Microsoft.AspNetCore.Hosting;

namespace MyWebApp.Api.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }
        private readonly string _folderPath = Path.Combine(Directory.GetCurrentDirectory(), "MediaStorage");
        public async Task<string> UploadFile(IFormFile file)
        {
            string fileName = Path.GetFileName(file.FileName);
            var keyName = Guid.NewGuid().ToString() + "_" + fileName;
            string filePath = Path.Combine(_folderPath, keyName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return keyName;
        }
        public async Task<string> SaveFileAsync(IFormFile file, string applicationId, string documentType)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "applications", applicationId);
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{documentType}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return Path.Combine("uploads", "applications", applicationId, uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName} for application {ApplicationId}",
                    file.FileName, applicationId);
                throw;
            }
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_folderPath, filePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }
                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file {FilePath}", filePath);
                throw;
            }
        }

        public async Task<string> SaveFileAsync(string fileName, byte[] fileBytes)
        {
            try
            {
                // Sanitize filename by replacing invalid characters
                var sanitizedFileName = SanitizeFileName(fileName);
                var keyName = $"{Guid.NewGuid()}_{sanitizedFileName}";
                var filePath = Path.Combine(_folderPath, keyName);

                // Ensure the directory exists (including any subdirectories)
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                await File.WriteAllBytesAsync(filePath, fileBytes);

                return keyName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName}", fileName);
                throw;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file";

            // Get invalid characters for file names
            var invalidChars = Path.GetInvalidFileNameChars();

            // Replace invalid characters with underscores
            foreach (var invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            // Also replace forward slashes specifically (they might not be in GetInvalidFileNameChars on all systems)
            fileName = fileName.Replace('/', '_').Replace('\\', '_');

            return fileName;
        }


    }
}
