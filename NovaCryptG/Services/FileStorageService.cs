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

    // This saves to a servers file system
    public void SaveFile(string fileName, string content)
    {
        var fullPath = Path.Combine(_storagePath, fileName);
        File.WriteAllTextAsync(fullPath, content);
    }

    // Deletion of file
    public void DeleteFile(string fileName)
    {
        var fullPath = Path.Combine(_storagePath, fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    // Loading of a file
    public async Task<string> LoadFileAsync(string fileName)
    {
        var fullPath = Path.Combine(_storagePath, fileName);
        if (File.Exists(fullPath))
            return await File.ReadAllTextAsync(fullPath);
        return string.Empty;
    }

    // Getting file list
    public List<string?> GetFileList()
    {
        // Looks for files with .encrypted file extension
        return Directory.GetFiles(_storagePath, "*.encrypted")
            .Select(Path.GetFileName)
            .OrderByDescending(f => f)
            .ToList();
    }
}