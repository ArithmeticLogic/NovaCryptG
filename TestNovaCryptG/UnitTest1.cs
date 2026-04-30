using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using moq;
using NovaCryptG.Services;

namespace UnitTest1.Tests
{
    // ------------------------------------------------------------------
    // Tests for CryptographyService (static methods)
    // ------------------------------------------------------------------
    public class CryptographyServiceTests
    {
        [Fact]
        public void Encrypt_Decrypt_RoundTrip()
        {
            // Arrange
            string original = "Hello, world!";
            string password = "testpass123"; // ≥8 chars
            byte[] originalBytes = Encoding.UTF8.GetBytes(original);

            // Act
            var encryptResult = CryptographyService.EncryptFileAsync(originalBytes, "temp", password).Result;
            Assert.True(encryptResult.Success);
            byte[] encrypted = encryptResult.Data;

            var decryptResult = CryptographyService.DecryptFileAsync(encrypted, "temp.encrypted", password).Result;
            Assert.True(decryptResult.Success);
            string decrypted = Encoding.UTF8.GetString(decryptResult.Data);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_ShortPassword_ShouldFail()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            string shortPassword = "short";

            var result = CryptographyService.EncryptFileAsync(data, "file.txt", shortPassword).Result;
            Assert.False(result.Success);
            Assert.Contains("at least 8 characters", result.Message);
        }

        [Fact]
        public void Decrypt_WrongPassword_ShouldProduceGarbage()
        {
            string original = "secret";
            string correctPassword = "correctPassword123";
            string wrongPassword = "wrongPassword123";
            byte[] originalBytes = Encoding.UTF8.GetBytes(original);

            var encryptResult = CryptographyService.EncryptFileAsync(originalBytes, "file.txt", correctPassword).Result;
            Assert.True(encryptResult.Success);
            byte[] encrypted = encryptResult.Data;

            var decryptResult = CryptographyService.DecryptFileAsync(encrypted, "file.txt.encrypted", wrongPassword).Result;
            Assert.True(decryptResult.Success); // decryption always succeeds
            string decrypted = Encoding.UTF8.GetString(decryptResult.Data);

            // With wrong password, the decrypted text should not equal the original
            Assert.NotEqual(original, decrypted);
        }

        [Fact]
        public void Decrypt_NonEncryptedFile_ShouldFail()
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes("This is not encrypted");
            string fileName = "plain.txt";

            var result = CryptographyService.DecryptFileAsync(plainBytes, fileName, "anyPassword").Result;
            Assert.False(result.Success);
            Assert.Equal("File is not encrypted", result.Message);
        }

        [Fact]
        public void Encrypt_EmptyData_ShouldSucceed()
        {
            byte[] empty = Array.Empty<byte>();
            string password = "password123";

            var result = CryptographyService.EncryptFileAsync(empty, "empty.txt", password).Result;
            Assert.True(result.Success);
            Assert.Empty(result.Data);
        }
    }

    // ------------------------------------------------------------------
    // Tests for FileStorageService (real file I/O in temp folder)
    // ------------------------------------------------------------------
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
        public async Task SaveAndLoad_ShouldPersist()
        {
            string fileName = "test.encrypted";
            string content = "Hello, world!";

            await _service.SaveFileAsync(fileName, content);
            string loaded = await _service.LoadFileAsync(fileName);

            Assert.Equal(content, loaded);
        }

        [Fact]
        public async Task Load_MissingFile_ReturnsEmpty()
        {
            string loaded = await _service.LoadFileAsync("missing.encrypted");
            Assert.Equal(string.Empty, loaded);
        }

        [Fact]
        public void GetFileList_ReturnsOnlyEncryptedFiles()
        {
            _service.SaveFileAsync("a.encrypted", "data").Wait();
            _service.SaveFileAsync("b.encrypted", "data").Wait();

            // Create a non-.encrypted file directly in the storage folder
            string storagePath = Path.Combine(_tempRoot, "AppData", "EncryptedFiles");
            Directory.CreateDirectory(storagePath);
            File.WriteAllText(Path.Combine(storagePath, "c.txt"), "ignored");

            var list = _service.GetFileList();
            Assert.Contains("a.encrypted", list);
            Assert.Contains("b.encrypted", list);
            Assert.DoesNotContain("c.txt", list);
        }

        [Fact]
        public void DeleteFile_RemovesExistingFile()
        {
            string fileName = "delete.encrypted";
            _service.SaveFileAsync(fileName, "data").Wait();
            string fullPath = Path.Combine(_tempRoot, "AppData", "EncryptedFiles", fileName);
            Assert.True(File.Exists(fullPath));

            _service.DeleteFile(fileName);
            Assert.False(File.Exists(fullPath));
        }

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

    // ------------------------------------------------------------------
    // Simple test for EncryptionTool component (optional)
    // This uses bUnit and mocks the service via an interface
    // If you haven't set up interfaces, skip this part.
    // ------------------------------------------------------------------
    // To keep it "most basic", we omit UI tests for now.
}