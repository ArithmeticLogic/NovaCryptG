namespace NovaCryptG.Services;

public class FileStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<FileStorageService> _logger;

    public record FileResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public string Content { get; init; } = "";
        public List<string?> FileList { get; init; } = new();
    }

    // Persistant file storage on the server
    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _storagePath = Path.Combine(env.ContentRootPath, "AppData", "EncryptedFiles");
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
        _logger = logger;
    }

    public FileResult GetFileList()
    {
        try
        {
            // Looks for files with .encrypted file extension
            var files = Directory.GetFiles(_storagePath, "*.encrypted")
                .Select(Path.GetFileName)
                .OrderByDescending(f => f)
                .ToList();
            return new FileResult { Success = true, FileList = files };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file list from {Path}", _storagePath);
            return new FileResult { Success = false, Message = "Unable to load file list." };
        }
    }

    public async Task<FileResult> SaveFileAsync(string fileName, string content)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, fileName);
            await File.WriteAllTextAsync(fullPath, content);
            _logger.LogInformation("Saved file {FileName}", fileName);
            return new FileResult { Success = true, Message = $"File saved as {fileName}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file {FileName}", fileName);
            return new FileResult { Success = false, Message = "Error saving file." };
        }
    }

    public async Task<FileResult> LoadFileAsync(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, fileName);
            if (File.Exists(fullPath))
            {
                var content = await File.ReadAllTextAsync(fullPath);
                _logger.LogInformation("Loaded file {FileName}", fileName);
                return new FileResult { Success = true, Content = content };
            }

            return new FileResult { Success = false, Message = "File not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load file {FileName}", fileName);
            return new FileResult { Success = false, Message = "Error loading file." };
        }
    }

    public FileResult DeleteFile(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file {FileName}", fileName);
                return new FileResult { Success = true, Message = $"Deleted {fileName}" };
            }

            return new FileResult { Success = false, Message = "File not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileName}", fileName);
            return new FileResult { Success = false, Message = "Error deleting file." };
        }
    }
}