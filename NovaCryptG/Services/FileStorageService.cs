namespace NovaCryptG.Services;

public class FileStorageService
{
    private readonly string _storagePath;

    // Persistant file storage on the server
    public FileStorageService(IWebHostEnvironment env)
    {
        _storagePath = Path.Combine(env.ContentRootPath, "AppData", "EncryptedFiles");
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public List<string?> GetFileList()
    {
        // Looks for files with .encrypted file extension
        return Directory.GetFiles(_storagePath, "*.encrypted")
            .Select(Path.GetFileName)
            .OrderByDescending(f => f)
            .ToList();
    }

    public async Task SaveFileAsync(string fileName, string content)
    {
        var fullPath = Path.Combine(_storagePath, fileName);
        await File.WriteAllTextAsync(fullPath, content);
    }

    public async Task<string> LoadFileAsync(string fileName)
    {
        var fullPath = Path.Combine(_storagePath, fileName);
        if (File.Exists(fullPath))
            return await File.ReadAllTextAsync(fullPath);
        return string.Empty;
    }

    public void DeleteFile(string fileName)
    {
        var fullPath = Path.Combine(_storagePath, fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}