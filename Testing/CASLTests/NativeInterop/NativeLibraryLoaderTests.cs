﻿// <copyright file="NativeLibraryLoaderTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable IDE0002 // Name can be simplified
namespace CASLTests
{
#pragma warning disable IDE0001 // Name can be simplified
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using CASL.Exceptions;
    using CASL.NativeInterop;
    using Moq;
    using Xunit;
    using Assert = CASLTests.Helpers.AssertExtensions;
#pragma warning restore IDE0001 // Name can be simplified

    /// <summary>
    /// Provides tests for the <see cref="NativeLibraryLoader"/> class.
    /// </summary>
    public class NativeLibraryLoaderTests
    {
        private const string WinDirPath = @"C:\Program Files\test-app";
        private const string LinuxDirPath = "/user/bin/test-app";
        private const string MacOSDirPath = "/Applications/test-app";
        private const string WinExtension = ".dll";
        private const string PosixExtenstion = ".so";
        private const string LibNameWithoutExt = "test-lib";
        private const string WinLibNameWithExt = LibNameWithoutExt + WinExtension;
        private const string PosixLibNameWithExt = LibNameWithoutExt + PosixExtenstion;
        private const char WinSeparatorChar = '\\';
        private const char PoxixSeparatorChar = '/';// MacOSX and Linux systems
        private readonly Mock<IDependencyManager> mockDependencyManager;
        private readonly Mock<IFilePathResolver> mockPathResolver;
        private readonly Mock<IPlatform> mockPlatform;
        private readonly Mock<IDirectory> mockDirectory;
        private readonly Mock<IFile> mockFile;
        private readonly Mock<IPath> mockPath;
        private readonly Mock<ILibrary> mockLibrary;
        private string? libPath;
        private ReadOnlyCollection<string>? libDirPaths;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="NativeLibraryLoaderTests"/> class.
        /// </summary>
        public NativeLibraryLoaderTests()
        {
            var testPath = @"/user/bin/my-lib.so.2";

            var nameNoExt = Path.GetFileNameWithoutExtension(testPath);
            var extension = Path.GetExtension(testPath);

            this.mockDependencyManager = new Mock<IDependencyManager>();
            this.mockPathResolver = new Mock<IFilePathResolver>();
            this.mockPlatform = new Mock<IPlatform>();
            this.mockDirectory = new Mock<IDirectory>();
            this.mockFile = new Mock<IFile>();
            this.mockPath = new Mock<IPath>();
            this.mockLibrary = new Mock<ILibrary>();
        }
        #endregion

        #region Constructor Tests
        [Fact]
        public void Ctor_WithNullDependencyManager_ThrowsException()
        {
            //Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                var loader = new NativeLibraryLoader(
                    null,
                    this.mockPlatform.Object,
                    this.mockDirectory.Object,
                    this.mockFile.Object,
                    this.mockPath.Object,
                    this.mockLibrary.Object);
            }, "The parameter must not be null. (Parameter 'dependencyManager')");
        }

        [Fact]
        public void Ctor_WithNullPlatform_ThrowsException()
        {
            //Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                var loader = new NativeLibraryLoader(
                    this.mockDependencyManager.Object,
                    null,
                    this.mockDirectory.Object,
                    this.mockFile.Object,
                    this.mockPath.Object,
                    this.mockLibrary.Object);
            }, "The parameter must not be null. (Parameter 'platform')");
        }

        [Fact]
        public void Ctor_WithNullDirectoryObject_ThrowsException()
        {
            //Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                var loader = new NativeLibraryLoader(
                    this.mockDependencyManager.Object,
                    this.mockPlatform.Object,
                    null,
                    this.mockFile.Object,
                    this.mockPath.Object,
                    this.mockLibrary.Object);
            }, "The parameter must not be null. (Parameter 'directory')");
        }

        [Fact]
        public void Ctor_WithNullFileObject_ThrowsException()
        {
            //Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                var loader = new NativeLibraryLoader(
                    this.mockDependencyManager.Object,
                    this.mockPlatform.Object,
                    this.mockDirectory.Object,
                    null,
                    this.mockPath.Object,
                    this.mockLibrary.Object);
            }, "The parameter must not be null. (Parameter 'file')");
        }

        [Fact]
        public void Ctor_WithNullPath_ThrowsException()
        {
            //Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                var loader = new NativeLibraryLoader(
                    this.mockDependencyManager.Object,
                    this.mockPlatform.Object,
                    this.mockDirectory.Object,
                    this.mockFile.Object,
                    null,
                    this.mockLibrary.Object);
            }, "The parameter must not be null. (Parameter 'path')");
        }

        [Fact]
        public void Ctor_WithNullLibrary_ThrowsException()
        {
            //Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                var loader = new NativeLibraryLoader(
                    this.mockDependencyManager.Object,
                    this.mockPlatform.Object,
                    this.mockDirectory.Object,
                    this.mockFile.Object,
                    this.mockPath.Object,
                    null);
            }, "The parameter must not be null. (Parameter 'library')");
        }

        [Fact]
        public void Ctor_WhenLibraryDoesNotExist_ThrowsException()
        {
            // Arrange
            MockPlatformAsWindows();
            this.mockLibrary.SetupGet(p => p.LibraryName).Returns(string.Empty);

            // Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                _ = CreateLoader();
            }, "The parameter must not be null or empty. (Parameter 'libraryName')");
        }

        [Theory]
        [InlineData(LibNameWithoutExt + ".txt", WinExtension)]
        public void Ctor_WhenUsingLibraryNameWithIncorrectLibraryExtension_FixesExtension(
            string libName,
            string extension)
        {
            //Arrange
            this.mockLibrary.SetupGet(p => p.LibraryName).Returns(libName);
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension(libName)).Returns($"{libName.Split('.')[0]}");
            this.mockPath.Setup(m => m.HasExtension(It.IsAny<string>()))
                .Returns<string>(path =>
                {
                    return path.Contains('.');
                });
            this.mockPlatform.Setup(m => m.GetPlatformLibFileExtension()).Returns(extension);

            //Act
            var loader = CreateLoader();

            //Assert
            Assert.Equal(WinLibNameWithExt, loader.LibraryName);
        }
        #endregion

        #region Method Tests
        [Theory]
        [InlineData(WinDirPath, WinSeparatorChar, WinLibNameWithExt, WinExtension)]
        [InlineData(LinuxDirPath, PoxixSeparatorChar, PosixLibNameWithExt, PosixExtenstion)]
        public void LoadLibrary_WhenLibraryDoesNotLoad_ThrowsException(
            string dirPath,
            char dirSeparatorChar,
            string libName,
            string extension)
        {
            //Arrange
            var systemError = "Could not load the library";

            this.mockFile.Setup(m => m.Exists($"{dirPath}{dirSeparatorChar}{libName}")).Returns(true);

            this.mockDependencyManager.SetupGet(p => p.NativeLibDirPath).Returns($"{dirPath}{dirSeparatorChar}");

            this.mockLibrary.SetupGet(p => p.LibraryName).Returns(libName);

            this.mockPath.Setup(m => m.GetFileNameWithoutExtension(libName)).Returns(libName.Split('.')[0]);
            this.mockPath.SetupGet(p => p.DirectorySeparatorChar).Returns(dirSeparatorChar);
            this.mockPath.Setup(m => m.HasExtension(It.IsAny<string>()))
                .Returns<string>(path =>
                {
                    return path.Contains('.');
                });

            this.mockPlatform.Setup(m => m.GetPlatformLibFileExtension()).Returns(extension);
            this.mockPlatform.Setup(m => m.GetLastSystemError()).Returns(systemError);

            var loader = CreateLoader();

            //Act & Assert
            Assert.ThrowsWithMessage<LoadLibraryException>(() =>
            {
                loader.LoadLibrary();
            }, $"{systemError}\n\nLibrary Path: '{dirPath}{dirSeparatorChar}{libName}'");
        }

        [Fact]
        public void LoadLibrary_WhenInvoked_ReturnsLibraryPointer()
        {
            // Arrange
            nint expected = 1234;
            var libFilePath = $"{WinDirPath}{WinSeparatorChar}{WinLibNameWithExt}";
            this.mockFile.Setup(m => m.Exists(libFilePath)).Returns(true);
            this.mockDependencyManager.SetupGet(p => p.NativeLibDirPath).Returns(WinDirPath);
            this.mockLibrary.SetupGet(p => p.LibraryName).Returns(WinLibNameWithExt);

            this.mockPath.SetupGet(p => p.DirectorySeparatorChar).Returns(WinSeparatorChar);
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension(WinLibNameWithExt)).Returns(LibNameWithoutExt);
            this.mockPath.Setup(m => m.HasExtension(It.IsAny<string>()))
                .Returns<string>(path =>
                {
                    return path.Contains('.');
                });

            this.mockPlatform.Setup(m => m.GetPlatformLibFileExtension()).Returns(WinExtension);
            this.mockPlatform.Setup(m => m.LoadLibrary(libFilePath)).Returns(expected);

            var loader = CreateLoader();

            // Act
            var actual = loader.LoadLibrary();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LoadLibrary_WhenLibraryFileDoesNotExist_ThrowsException()
        {
            nint expected = 1234;
            var libFilePath = $"{WinDirPath}{WinSeparatorChar}{WinLibNameWithExt}";
            this.mockFile.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);
            this.mockDependencyManager.SetupGet(p => p.NativeLibDirPath).Returns(WinDirPath);
            this.mockLibrary.SetupGet(p => p.LibraryName).Returns(WinLibNameWithExt);

            this.mockPath.SetupGet(p => p.DirectorySeparatorChar).Returns(WinSeparatorChar);
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension(WinLibNameWithExt)).Returns(LibNameWithoutExt);
            this.mockPath.Setup(m => m.HasExtension(It.IsAny<string>()))
               .Returns<string>(path =>
               {
                   return path.Contains('.');
               });

            this.mockPlatform.Setup(m => m.GetPlatformLibFileExtension()).Returns(WinExtension);
            this.mockPlatform.Setup(m => m.LoadLibrary(libFilePath)).Returns(expected);

            var loader = CreateLoader();

            // Act & Assert
            Assert.ThrowsWithMessage<FileNotFoundException>(() =>
            {
                _ = loader.LoadLibrary();
            }, $"Could not find the library '{WinLibNameWithExt}' in directory path '{WinDirPath}{WinSeparatorChar}'");
        }
        #endregion

        /// <summary>
        /// Mocks a windows platform.
        /// </summary>
        private void MockPlatformAsWindows()
        {
            this.libDirPaths = new ReadOnlyCollection<string>(new[] { $@"C:\test-dir\" });
            this.libPath = $"{this.libDirPaths[0]}{WinLibNameWithExt}";

            this.mockPlatform.Setup(m => m.IsWinPlatform()).Returns(true);
            this.mockPlatform.Setup(m => m.IsPosixPlatform()).Returns(false);
            this.mockPlatform.Setup(m => m.GetPlatformLibFileExtension()).Returns(".dll");
            this.mockPlatform.Setup(m => m.Is32BitProcess()).Returns(false);
            this.mockPlatform.Setup(m => m.Is64BitProcess()).Returns(true);
            this.mockPlatform.Setup(m => m.LoadLibrary(this.libPath)).Returns(new IntPtr(1234));
            this.mockPlatform.Setup(m => m.GetLastSystemError()).Returns("Could not load module.");

            this.mockDirectory.Setup(m => m.Exists(this.libDirPaths[0])).Returns(true);

            this.mockFile.Setup(m => m.Exists(this.libPath)).Returns(true);

            this.mockPath.SetupGet(p => p.DirectorySeparatorChar).Returns('\\');
            this.mockPath.Setup(m => m.HasExtension(WinLibNameWithExt)).Returns(true);
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns(WinLibNameWithExt.Replace(".dll", ""));

            this.mockLibrary.SetupGet(p => p.LibraryName).Returns(WinLibNameWithExt);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NativeLibraryLoader"/> class for the purpose of testing.
        /// </summary>
        /// <returns>The instance to test.</returns>
        private NativeLibraryLoader CreateLoader()
            => new NativeLibraryLoader(
                this.mockDependencyManager.Object,
                this.mockPlatform.Object,
                this.mockDirectory.Object,
                this.mockFile.Object,
                this.mockPath.Object,
                this.mockLibrary.Object);
    }
}
