// <copyright file="ManifestServiceTests.cs" company="Morten Larsen">
// Copyright (c) Morten Larsen. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using AspNetWebpack.AssetHelpers.Testing;
using FluentAssertions;
using Moq;
using Xunit;

namespace AspNetWebpack.AssetHelpers.Tests
{
    public sealed class ManifestServiceTests : IDisposable
    {
        private readonly Mock<IFileSystem> _fileSystemMock;

        private ManifestService? _manifestService;

        public ManifestServiceTests()
        {
            _fileSystemMock = DependencyMocker.GetFileSystem();
        }

        public void Dispose()
        {
            _manifestService?.Dispose();
        }

        [Fact]
        public async Task GetFromManifest_DevelopmentNoHttpClient_ShouldThrowArgumentNullException()
        {
            // Arrange
            var sharedSettingsMock = DependencyMocker.GetSharedSettings(TestValues.Development);
            _manifestService = new ManifestService(sharedSettingsMock.Object, _fileSystemMock.Object);

            // Act
            Func<Task> act = () => _manifestService.GetFromManifestAsync("InvalidBundle");

            // Assert
            await act.Should().ThrowExactlyAsync<ArgumentNullException>();
            _fileSystemMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetFromManifest_DevelopmentRequestFail_ShouldThrowHttpRequestException()
        {
            // Arrange
            var sharedSettingsMock = DependencyMocker.GetSharedSettings(TestValues.Development);
            var httpClientFactoryMock = DependencyMocker.GetHttpClientFactory(new HttpMessageHandlerFailStub());
            _manifestService = new ManifestService(sharedSettingsMock.Object, _fileSystemMock.Object, httpClientFactoryMock.Object);

            // Act
            Func<Task> act = () => _manifestService.GetFromManifestAsync("InvalidBundle");

            // Assert
            await act.Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage("Webpack Dev Server not started!");
            _fileSystemMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetFromManifest_DevelopmentInvalidBundle_ShouldReturnNull()
        {
            // Arrange
            var sharedSettingsMock = DependencyMocker.GetSharedSettings(TestValues.Development);
            var httpClientFactoryMock = DependencyMocker.GetHttpClientFactory(new HttpMessageHandlerManifestStub());
            _manifestService = new ManifestService(sharedSettingsMock.Object, _fileSystemMock.Object, httpClientFactoryMock.Object);

            // Act
            var result = await _manifestService.GetFromManifestAsync("InvalidBundle");
            var result2 = await _manifestService.GetFromManifestAsync("InvalidBundle");

            // Assert
            result.Should().BeNull();
            result2.Should().BeNull();
            _fileSystemMock.VerifyNoOtherCalls();
            httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
            httpClientFactoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetFromManifest_DevelopmentValidBundle_ShouldReturnResultBundle()
        {
            // Arrange
            var sharedSettingsMock = DependencyMocker.GetSharedSettings(TestValues.Development);
            var httpClientFactoryMock = DependencyMocker.GetHttpClientFactory(new HttpMessageHandlerManifestStub());
            _manifestService = new ManifestService(sharedSettingsMock.Object, _fileSystemMock.Object, httpClientFactoryMock.Object);

            // Act
            var result = await _manifestService.GetFromManifestAsync(TestValues.JsonBundle);
            var result2 = await _manifestService.GetFromManifestAsync(TestValues.JsonBundle);

            // Assert
            result.Should().Be(TestValues.JsonResultBundle);
            result2.Should().Be(TestValues.JsonResultBundle);
            _fileSystemMock.VerifyNoOtherCalls();
            httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
            httpClientFactoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetFromManifest_ProductionInvalidBundle_ShouldReturnNull()
        {
            // Arrange
            var sharedSettingsMock = DependencyMocker.GetSharedSettings(TestValues.Production);
            var httpClientFactoryMock = DependencyMocker.GetHttpClientFactory(new HttpMessageHandlerManifestStub());
            _manifestService = new ManifestService(sharedSettingsMock.Object, _fileSystemMock.Object, httpClientFactoryMock.Object);

            // Act
            var result = await _manifestService.GetFromManifestAsync("InvalidBundle");
            var result2 = await _manifestService.GetFromManifestAsync("InvalidBundle");

            // Assert
            result.Should().BeNull();
            result2.Should().BeNull();
            _fileSystemMock.Verify(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default), Times.Once);
            _fileSystemMock.VerifyNoOtherCalls();
            httpClientFactoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetFromManifest_ProductionValidBundle_ShouldReturnResultBundle()
        {
            // Arrange
            var sharedSettingsMock = DependencyMocker.GetSharedSettings(TestValues.Production);
            var httpClientFactoryMock = DependencyMocker.GetHttpClientFactory(new HttpMessageHandlerManifestStub());
            _manifestService = new ManifestService(sharedSettingsMock.Object, _fileSystemMock.Object, httpClientFactoryMock.Object);

            // Act
            var result = await _manifestService.GetFromManifestAsync(TestValues.JsonBundle);
            var result2 = await _manifestService.GetFromManifestAsync(TestValues.JsonBundle);

            // Assert
            result.Should().Be(TestValues.JsonResultBundle);
            result2.Should().Be(TestValues.JsonResultBundle);
            _fileSystemMock.Verify(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default), Times.Once);
            _fileSystemMock.VerifyNoOtherCalls();
            httpClientFactoryMock.VerifyNoOtherCalls();
        }
    }
}
