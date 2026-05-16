using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NovaCryptG.Data;
using NovaCryptG.Models;
using NovaCryptG.Services;

namespace TestNovaCryptG
{
    public abstract class DatabaseTestBase
    {
        private readonly SqliteConnection _keepAliveConnection;

        protected DatabaseTestBase()
        {
            _keepAliveConnection = new SqliteConnection("DataSource=:memory:");
            _keepAliveConnection.Open();
        }

        protected IDbContextFactory<AppDbContext> CreateDbContextFactory()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_keepAliveConnection)
                .Options;

            var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
            mockFactory
                .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var context = new AppDbContext(options);
                    context.Database.EnsureCreated();
                    return context;
                });

            return mockFactory.Object;
        }

        public void Dispose()
        {
            _keepAliveConnection.Close();
            _keepAliveConnection.Dispose();
        }
    }

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
            var mockLogger = new Mock<ILogger<FileStorageService>>();
            _service = new FileStorageService(mockEnv.Object, mockLogger.Object);
        }

        [Fact]
        public async Task SaveAndLoad_ShouldPersist() // Both functions individually, as 1 test
        {
            const string fileName = "test.encrypted";
            string content = "Hello, world!";

            var saveResult = await _service.SaveFileAsync(fileName, content);
            Assert.True(saveResult.Success);

            var loadResult = await _service.LoadFileAsync(fileName);
            Assert.True(loadResult.Success);
            Assert.Equal(content, loadResult.Content);
        }

        [Fact]
        public async Task Load_MissingFile_ShouldFail()
        {
            var loadResult = await _service.LoadFileAsync("missing.encrypted");
            Assert.False(loadResult.Success);
            Assert.Equal("File not found.", loadResult.Message);
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

            var listResult = _service.GetFileList();
            Assert.True(listResult.Success);
            Assert.Contains("a.encrypted", listResult.FileList);
            Assert.Contains("b.encrypted", listResult.FileList);
            Assert.DoesNotContain("c.txt", listResult.FileList);
        }

        [Fact]
        public async Task DeleteFile_RemovesExistingFile()
        {
            string fileName = "delete.encrypted";
            await _service.SaveFileAsync(fileName, "data");
            var fullPath = Path.Combine(_tempRoot, "AppData", "EncryptedFiles", fileName);
            Assert.True(File.Exists(fullPath));

            var deleteResult = _service.DeleteFile(fileName);
            Assert.True(deleteResult.Success);
            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void DeleteFile_Missing_ShouldFail()
        {
            var result = _service.DeleteFile("nonexistent.encrypted");
            Assert.False(result.Success);
            Assert.Equal("File not found.", result.Message);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
    }

    public class AuthenticationServiceTests : DatabaseTestBase, IDisposable
    {
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;

        public AuthenticationServiceTests()
        {
            _mockLogger = new Mock<ILogger<AuthenticationService>>();
        }

        private AuthenticationService CreateService(IDbContextFactory<AppDbContext> factory)
        {
            return new AuthenticationService(factory, _mockLogger.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUser()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string username = "testuser";
            string password = "StrongPass1!";
            string hashed = BCrypt.Net.BCrypt.HashPassword(password);

            await using (var ctx = await factory.CreateDbContextAsync())
            {
                ctx.LoginCredentials.Add(new LoginCredential
                {
                    UserName = username,
                    UserPassword = hashed,
                    IsAdmin = false
                });
                await ctx.SaveChangesAsync();
            }

            var result = await service.LoginAsync(username, password);
            Assert.NotNull(result);
            Assert.Equal(username, result.UserName);
            Assert.False(result.IsAdmin);
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ReturnsNull()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            var result = await service.LoginAsync("nonexistent", "AnyPass1!");
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string username = "testuser";
            string correctPassword = "CorrectPass1!";
            string wrongPassword = "WrongPass1!";
            string hashed = BCrypt.Net.BCrypt.HashPassword(correctPassword);

            await using (var ctx = await factory.CreateDbContextAsync())
            {
                ctx.LoginCredentials.Add(new LoginCredential
                {
                    UserName = username,
                    UserPassword = hashed
                });
                await ctx.SaveChangesAsync();
            }

            var result = await service.LoginAsync(username, wrongPassword);
            Assert.Null(result);
        }

        [Fact]
        public async Task IsUserAdminAsync_AdminUser_ReturnsTrue()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string username = "admin";

            await using (var ctx = await factory.CreateDbContextAsync())
            {
                ctx.LoginCredentials.Add(new LoginCredential
                {
                    UserName = username,
                    UserPassword = "hash",
                    IsAdmin = true
                });
                await ctx.SaveChangesAsync();
            }

            var isAdmin = await service.IsUserAdminAsync(username);
            Assert.True(isAdmin);
        }

        [Fact]
        public async Task IsUserAdminAsync_NonAdminUser_ReturnsFalse()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string username = "user";

            await using (var ctx = await factory.CreateDbContextAsync())
            {
                ctx.LoginCredentials.Add(new LoginCredential
                {
                    UserName = username,
                    UserPassword = "hash",
                    IsAdmin = false
                });
                await ctx.SaveChangesAsync();
            }

            var isAdmin = await service.IsUserAdminAsync(username);
            Assert.False(isAdmin);
        }

        [Fact]
        public async Task IsUserAdminAsync_UserNotFound_ReturnsFalse()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            var isAdmin = await service.IsUserAdminAsync("ghost");
            Assert.False(isAdmin);
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }

    public class RegistrationServiceTests : DatabaseTestBase, IDisposable
    {
        private readonly Mock<ILogger<RegistrationService>> _mockLogger;

        public RegistrationServiceTests()
        {
            _mockLogger = new Mock<ILogger<RegistrationService>>();
        }

        private RegistrationService CreateService(IDbContextFactory<AppDbContext> factory)
        {
            return new RegistrationService(factory, _mockLogger.Object);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_Succeeds()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string username = "newuser";
            string password = "ValidPass1!";

            var (success, message) = await service.RegisterAsync(username, password);
            Assert.True(success);
            Assert.Contains("success", message, StringComparison.OrdinalIgnoreCase);

            await using var ctx = await factory.CreateDbContextAsync();
            var user = await ctx.LoginCredentials.FirstOrDefaultAsync(u => u.UserName == username);
            Assert.NotNull(user);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, user.UserPassword));
            Assert.False(user.IsAdmin);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ShouldFail()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string username = "existing";
            string password = "Pass1234!";

            await using (var ctx = await factory.CreateDbContextAsync())
            {
                ctx.LoginCredentials.Add(new LoginCredential
                {
                    UserName = username,
                    UserPassword = "whatever"
                });
                await ctx.SaveChangesAsync();
            }

            var (success, message) = await service.RegisterAsync(username, password);
            Assert.False(success);
            Assert.Equal("Username already taken.", message);
        }

        [Fact]
        public async Task RegisterAsync_CaseInsensitiveDuplicate_ShouldFail()
        {
            var factory = CreateDbContextFactory();
            var service = CreateService(factory);
            string original = "UserOne";
            string duplicate = "userone";

            await using (var ctx = await factory.CreateDbContextAsync())
            {
                ctx.LoginCredentials.Add(new LoginCredential
                {
                    UserName = original,
                    UserPassword = "hash"
                });
                await ctx.SaveChangesAsync();
            }

            var (success, _) = await service.RegisterAsync(duplicate, "Pass1234!");
            Assert.False(success);
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }

    public class UserSessionServiceTests
    {
        [Fact]
        public void Initially_NotLoggedIn()
        {
            var session = new UserSessionService();
            Assert.False(session.IsLoggedIn);
            Assert.Null(session.CurrentUserName);
        }

        [Fact]
        public void LogIn_SetsUserNameAndIsLoggedIn()
        {
            var session = new UserSessionService();
            session.LogIn("Alice");
            Assert.True(session.IsLoggedIn);
            Assert.Equal("Alice", session.CurrentUserName);
        }

        [Fact]
        public void LogIn_RaisesOnChangedEvent()
        {
            var session = new UserSessionService();
            bool eventFired = false;
            session.OnChanged += () => eventFired = true;
            session.LogIn("Bob");
            Assert.True(eventFired);
        }
    }
}