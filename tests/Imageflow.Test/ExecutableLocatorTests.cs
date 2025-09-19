using Imageflow.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Imageflow.Test
{
    public class ExecutableLocatorTests : IDisposable
    {
        private readonly string _tempDir;

        public ExecutableLocatorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ImageflowTest_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Fact]
        public void FindsExecutable_InCustomPath()
        {
            // Arrange
            var fakeExeName = "myfake-imageflow";
            var exeExtension = RuntimeFileLocator.ExecutableExtension.Value;
            var fullExeName = exeExtension.Length > 0 ? $"{fakeExeName}.{exeExtension}" : fakeExeName;
            var fakeExePath = Path.Combine(_tempDir, fullExeName);
            File.WriteAllText(fakeExePath, "I am not a real executable");

            var searchDirs = new List<string> { _tempDir };

            // Act
            var result = ExecutableLocator.FindExecutable(fakeExeName, searchDirs);

            // Assert
            Assert.Equal(fakeExePath, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Throws_WhenExecutableIsMissing()
        {
            // Arrange
            var fakeExeName = "a-missing-executable";
            var searchDirs = new List<string> { _tempDir };

            // Act & Assert
            var ex = Assert.Throws<DllNotFoundException>(() => ExecutableLocator.FindExecutable(fakeExeName, searchDirs));
            Assert.Contains(fakeExeName, ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindsExecutable_AndCachesResult()
        {
            // Arrange
            var fakeExeName = "another-fake-imageflow";
            var exeExtension = RuntimeFileLocator.ExecutableExtension.Value;
            var fullExeName = exeExtension.Length > 0 ? $"{fakeExeName}.{exeExtension}" : fakeExeName;
            var fakeExePath = Path.Combine(_tempDir, fullExeName);
            File.WriteAllText(fakeExePath, "I am also not a real executable");

            var searchDirs = new List<string> { _tempDir };

            // Act
            var result1 = ExecutableLocator.FindExecutable(fakeExeName, searchDirs);

            // To test caching, we delete the file. The second call should still succeed.
            File.Delete(fakeExePath);
            var result2 = ExecutableLocator.FindExecutable(fakeExeName, searchDirs);

            // Assert
            Assert.Equal(fakeExePath, result1, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(result1, result2);
        }
    }
}
