﻿// <copyright file="NativeDependencyManagerTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

#pragma warning disable IDE0002 // Name can be simplified
namespace CASLTests.NativeInterop
{
#pragma warning disable IDE0001 // Name can be simplified
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using CASL;
    using CASL.NativeInterop;
    using Moq;
    using Xunit;
    using Assert = CASLTests.Helpers.AssertExtensions;
#pragma warning restore IDE0001 // Name can be simplified

    /// <summary>
    /// Tests the <see cref="NativeDependencyManager"/> class.
    /// </summary>
    public class NativeDependencyManagerTests
    {
        private readonly Mock<IPlatform> mockPlatform;
        private readonly Mock<IFile> mockFile;
        private readonly Mock<IPath> mockPath;
        private readonly Mock<IApplication> mockApp;
        private readonly Mock<IFilePathResolver> mockPathResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeDependencyManagerTests"/> class.
        /// </summary>
        public NativeDependencyManagerTests()
        {
            this.mockPlatform = new Mock<IPlatform>();
            this.mockFile = new Mock<IFile>();
            this.mockPath = new Mock<IPath>();
            this.mockApp = new Mock<IApplication>();
            this.mockPathResolver = new Mock<IFilePathResolver>();
        }

        #region Constructor Tests
        [Fact]
        public void Ctor_WhenInvokedWithNullPlatform_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                _ = new OpenALDependencyManager(null, null, null, null, null);
            }, "The parameter must not be null. (Parameter 'platform')");
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullFile_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                _ = new OpenALDependencyManager(
                    new Mock<IPlatform>().Object,
                    null,
                    this.mockPath.Object,
                    this.mockApp.Object,
                    this.mockPathResolver.Object);
            }, "The parameter must not be null. (Parameter 'file')");
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullPath_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                _ = new OpenALDependencyManager(
                    new Mock<IPlatform>().Object,
                    this.mockFile.Object,
                    null,
                    this.mockApp.Object,
                    this.mockPathResolver.Object);
            }, "The parameter must not be null. (Parameter 'path')");
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullApplication_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                _ = new OpenALDependencyManager(
                    new Mock<IPlatform>().Object,
                    this.mockFile.Object,
                    this.mockPath.Object,
                    null,
                    this.mockPathResolver.Object);
            }, "The parameter must not be null. (Parameter 'application')");
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullPathResolver_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                _ = new OpenALDependencyManager(
                    new Mock<IPlatform>().Object,
                    this.mockFile.Object,
                    this.mockPath.Object,
                    this.mockApp.Object,
                    null);
            }, "The parameter must not be null. (Parameter 'nativeLibPathResolver')");
        }
        #endregion

        #region Prop Tests
        [Fact]
        public void NativeLibraries_WhenGettingNullValue_ReturnsCorrecResult()
        {
            // Arrange
            this.mockPlatform.Setup(m => m.Is64BitProcess()).Returns(true);

            var manager = CreateManager();

            // Act
            manager.NativeLibraries = null;
            var actual = manager.NativeLibraries;

            // Assert
            Assert.Empty(actual);
        }

        [Fact]
        public void NativeLibraries_WhenSettingValue_ReturnsCorrecResult()
        {
            // Arrange
            var libName = "test-native-lib.dll";
            this.mockPlatform.Setup(m => m.Is64BitProcess()).Returns(true);
            this.mockPlatform.Setup(m => m.IsWinPlatform()).Returns(true);
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension(libName)).Returns(libName.Split('.')[0]);

            var manager = CreateManager();

            // Act
            manager.NativeLibraries = new ReadOnlyCollection<string>(new List<string> { libName });
            var actual = manager.NativeLibraries;

            // Assert
            Assert.Single(actual);
            Assert.Equal("test-native-lib", actual[0]);
        }
        #endregion

        #region Method Tests
        [Fact]
        public void VerifyDependencies_WhenLibrarySrcDoesNotExist_ThrowsException()
        {
            // Arrange
            var assemblyDirPath = @"C:\test-dir\";
            var srcDirPath = $@"{assemblyDirPath}runtimes\win-x64\native\";

            this.mockFile.Setup(m => m.Exists($"{srcDirPath}lib.dll")).Returns(false);
            this.mockPathResolver.Setup(m => m.GetDirPath()).Returns(srcDirPath);

            this.mockPath.Setup(m => m.GetExtension("lib.dll")).Returns(".dll");
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension("lib.dll")).Returns("lib");

            var manager = CreateManager();
            manager.NativeLibraries = new ReadOnlyCollection<string>(new[] { "lib.dll" }.ToList());

            // Act & Assert
            Assert.ThrowsWithMessage<FileNotFoundException>(() =>
            {
                manager.VerifyDependencies();
            }, $"The native dependency library '{srcDirPath}lib.dll' does not exist.");
        }

        [Fact]
        public void VerifyDependencies_WhenNativeLibExists_DoesNotThrowException()
        {
            // Arrange
            var assemblyDirPath = @"C:\test-dir\";
            var srcDirPath = $@"{assemblyDirPath}runtimes\win-x64\native\";

            this.mockFile.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);
            this.mockPathResolver.Setup(m => m.GetDirPath()).Returns(srcDirPath);

            this.mockPath.Setup(m => m.GetExtension("lib.dll")).Returns(".dll");
            this.mockPath.Setup(m => m.GetFileNameWithoutExtension("lib.dll")).Returns("lib");

            var manager = CreateManager();
            manager.NativeLibraries = new ReadOnlyCollection<string>(new[] { "lib.dll" }.ToList());

            // Act & Assert
            Assert.DoesNotThrow<FileNotFoundException>(() =>
            {
                manager.VerifyDependencies();
            });
        }
        #endregion

        /// <summary>
        /// Creates a new instance of <see cref="OpenALDependencyManager"/> for the purpose of testing.
        /// </summary>
        /// <returns>The instance to test.</returns>
        private OpenALDependencyManager CreateManager()
            => new OpenALDependencyManager(
                this.mockPlatform.Object,
                this.mockFile.Object,
                this.mockPath.Object,
                this.mockApp.Object,
                this.mockPathResolver.Object);
    }
}
