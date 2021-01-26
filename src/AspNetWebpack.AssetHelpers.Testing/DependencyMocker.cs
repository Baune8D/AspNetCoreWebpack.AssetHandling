// <copyright file="DependencyMocker.cs" company="Morten Larsen">
// Copyright (c) Morten Larsen. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace AspNetWebpack.AssetHelpers.Testing
{
    /// <summary>
    /// Static functions for mocking common dependencies.
    /// </summary>
    public static class DependencyMocker
    {
        /// <summary>
        /// Mock IWebHostEnvironment.
        /// </summary>
        /// <param name="environmentName">The environment name.</param>
        /// <returns>The WebHostEnvironment object.</returns>
        public static Mock<IWebHostEnvironment> GetWebHostEnvironment(string environmentName)
        {
            var webHostEnvironmentMock = new Mock<IWebHostEnvironment>();

            webHostEnvironmentMock
                .SetupGet(x => x.EnvironmentName)
                .Returns(environmentName);

            webHostEnvironmentMock
                .SetupGet(x => x.WebRootPath)
                .Returns(TestValues.WebRootPath);

            return webHostEnvironmentMock;
        }

        /// <summary>
        /// Mock IFileSystem.
        /// </summary>
        /// <returns>The FileSystem object.</returns>
        public static Mock<IFileSystem> GetFileSystem()
        {
            var fileSystemMock = new Mock<IFileSystem>();

            fileSystemMock
                .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync($"{{\"{TestValues.JsonBundle}\": \"{TestValues.JsonResultBundle}\"}}");

            return fileSystemMock;
        }

        /// <summary>
        /// Mock IHttpClientFactory.
        /// </summary>
        /// <param name="httpMessageHandler">The HTTP message handler to use with HttpClient.</param>
        /// <returns>The HttpClientFactory object.</returns>
        public static Mock<IHttpClientFactory> GetHttpClientFactory(HttpMessageHandler httpMessageHandler)
        {
            var httpClientFactory = new Mock<IHttpClientFactory>();

            httpClientFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns<string>(_ => new HttpClient(httpMessageHandler));

            return httpClientFactory;
        }

        /// <summary>
        /// Mock ISharedSettings.
        /// </summary>
        /// <param name="environmentName">The environment name.</param>
        /// <returns>The SharedSettings object.</returns>
        public static Mock<ISharedSettings> GetSharedSettings(string environmentName)
        {
            var isDevelopment = environmentName == TestValues.Development;

            var sharedSettings = new Mock<ISharedSettings>();

            sharedSettings
                .SetupGet(x => x.DevelopmentMode)
                .Returns(isDevelopment);

            sharedSettings
                .SetupGet(x => x.AssetsDirectoryPath)
                .Returns(isDevelopment
                    ? "https://domain/assets/"
                    : "/Some/Path/To/Assets/");

            sharedSettings
                .SetupGet(x => x.AssetsWebPath)
                .Returns(TestValues.AssetsWebPath);

            sharedSettings
                .SetupGet(x => x.ManifestPath)
                .Returns(isDevelopment
                    ? "https://domain/manifest.json"
                    : "/Some/Path/To/Manifest.json");

            return sharedSettings;
        }
    }
}
