using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NovaCryptG.Services;

// TODO: Look into whether I should add some tests from the pages such as password validation.
// TODO: Add tests for the other services (Current services with tests should be fine and not require any major updated)
namespace TestNovaCryptG
{
    // Tests for CryptographyService (static methods)
    public class EncryptionServiceTests
    {
        [Fact]
        public async Task Encrypt_Decrypt_RoundTrip()
        {
            // Arrange
            string original = "Hello, world!";
            string password = "Testing1! "; // Password must be at least 8 characters long and have at least 1 upper case character, digit, and special character
            byte[] originalBytes = Encoding.UTF8.GetBytes(original);

            // Act
            var encryptResult = await EncryptionService.EncryptFileAsync(originalBytes, "temp", password);
            Assert.True(encryptResult.Success);
            byte[] encrypted = encryptResult.Data;

            var decryptResult = await EncryptionService.DecryptFileAsync(encrypted, "temp.encrypted", password);
            Assert.True(decryptResult.Success);
            string decrypted = Encoding.UTF8.GetString(decryptResult.Data);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public async Task Encrypt_ShortPassword_ShouldFail()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            string shortPassword = "Test1!"; // Less than 8 characters

            var result = await EncryptionService.EncryptFileAsync(data, "file.txt", shortPassword);
            Assert.False(result.Success);
            Assert.Contains("Password must be at least 8 characters", result.Message);
        }

        [Fact]
        public async Task Encrypt_NoUpperCaseCharacterInPassword_ShouldFail()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            string noUpperCaseCharacterPassword = "testing1!"; // No upper case character

            var result = await EncryptionService.EncryptFileAsync(data, "file.txt", noUpperCaseCharacterPassword);
            Assert.False(result.Success);
            Assert.Contains("Password must be at least 8 characters", result.Message);
        }

        [Fact]
        public async Task Encrypt_NoDigitInPassword_ShouldFail()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            string noDigitPassword = "Testing!"; // No digit

            var result = await EncryptionService.EncryptFileAsync(data, "file.txt", noDigitPassword);
            Assert.False(result.Success);
            Assert.Contains("Password must be at least 8 characters", result.Message);
        }

        [Fact]
        public async Task Encrypt_NoSpecialCharacterInPassword_ShouldFail()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            string noSpecialCharacterPassword = "Testing1"; // No special character

            var result = await EncryptionService.EncryptFileAsync(data, "file.txt", noSpecialCharacterPassword);
            Assert.False(result.Success);
            Assert.Contains("Password must be at least 8 characters", result.Message);
        }

        [Fact]
        public async Task Decrypt_WrongPassword_ShouldProduceGarbage()
        {
            string original = "secret";
            string correctPassword = "correctPassword1!";
            string wrongPassword = "wrongPassword1!";
            byte[] originalBytes = Encoding.UTF8.GetBytes(original);

            var encryptResult = await EncryptionService.EncryptFileAsync(originalBytes, "file.txt", correctPassword);
            Assert.True(encryptResult.Success);
            byte[] encrypted = encryptResult.Data;

            var decryptResult = await EncryptionService.DecryptFileAsync(encrypted, "file.txt.encrypted", wrongPassword);
            Assert.True(decryptResult.Success); // decryption always succeeds
            string decrypted = Encoding.UTF8.GetString(decryptResult.Data);

            // With wrong password, the decrypted text should not equal the original
            Assert.NotEqual(original, decrypted);
        }

        [Fact]
        public async Task Decrypt_NonEncryptedFile_ShouldFail()
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes("This is not encrypted");
            const string fileName = "plain.txt";

            var result = await EncryptionService.DecryptFileAsync(plainBytes, fileName, "testing1!");
            Assert.False(result.Success);
            Assert.Equal("File is not encrypted", result.Message);
        }
    }

    // Tests for FileStorageService (real file I/O in temp folder)
    public class FileStorageServiceTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly FileStorageService _service;

        public FileStorageServiceTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempRoot);
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(_tempRoot);
            _service = new FileStorageService(mockEnv.Object);
        }

        [Fact]
        public async Task SaveAndLoad_ShouldPersist() // Both functions individually, as 1 test
        {
            const string fileName = "test.encrypted";
            string content = "Hello, world!";

            await _service.SaveFileAsync(fileName, content);
            string loaded = await _service.LoadFileAsync(fileName);

            Assert.Equal(content, loaded);
        }

        // If a file is not present/does not exist
        [Fact]
        public async Task Load_MissingFile_ReturnsEmpty()
        {
            var loaded = await _service.LoadFileAsync("missing.encrypted");
            Assert.Equal(string.Empty, loaded);
        }

        [Fact]
        public async Task GetFileList_ReturnsOnlyEncryptedFiles()
        {
            await _service.SaveFileAsync("a.encrypted", "data");
            await _service.SaveFileAsync("b.encrypted", "data");

            // Create a non-.encrypted file directly in the storage folder
            var storagePath = Path.Combine(_tempRoot, "AppData", "EncryptedFiles");
            Directory.CreateDirectory(storagePath);
            await File.WriteAllTextAsync(Path.Combine(storagePath, "c.txt"), "ignored");

            var list = _service.GetFileList();
            Assert.Contains("a.encrypted", list);
            Assert.Contains("b.encrypted", list);
            Assert.DoesNotContain("c.txt", list);
        }

        [Fact]
        public async Task DeleteFile_RemovesExistingFile()
        {
            string fileName = "delete.encrypted";
            await _service.SaveFileAsync(fileName, "data");
            var fullPath = Path.Combine(_tempRoot, "AppData", "EncryptedFiles", fileName);
            Assert.True(File.Exists(fullPath));

            _service.DeleteFile(fileName);
            Assert.False(File.Exists(fullPath));
        }

        // If a file is not present/does not exist
        [Fact]
        public void DeleteFile_Missing_DoesNothing()
        {
            // Should not throw
            _service.DeleteFile("nonexistent.encrypted");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
    }
}