using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AspNetWebpack.AssetHelpers
{
    /// <summary>
    /// Service for including Webpack assets in UI projects.
    /// </summary>
    public class AssetService : IAssetService
    {
        private readonly Dictionary<string, string> _inlineStyles = new();
        private JsonDocument? _manifest;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetService"/> class.
        /// </summary>
        /// <param name="env">Web host environment.</param>
        /// <param name="options">Webpack options.</param>
        /// <param name="httpClientFactory">HttpClient factory.</param>
        /// <exception cref="ArgumentNullException">If Webpack options is null.</exception>
        public AssetService(IWebHostEnvironment env, IOptions<WebpackOptions> options, IHttpClientFactory httpClientFactory)
        {
            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            DevelopmentMode = env.IsDevelopment();

            if (DevelopmentMode)
            {
                HttpClient = httpClientFactory.CreateClient();
            }

            AssetBaseFilePath = env.IsDevelopment()
                ? options.Value.InternalDevServer + options.Value.AssetsPublicPath
                : env.WebRootPath + options.Value.AssetsPublicPath;

            ManifestPath = AssetBaseFilePath + options.Value.ManifestFile;

            AssetPath = DevelopmentMode
                ? options.Value.PublicDevServer + options.Value.AssetsPublicPath
                : options.Value.AssetsPublicPath;
        }

        /// <summary>
        /// Gets web path for UI assets.
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// Gets HttpClient for retrieving the manifest in development mode.
        /// </summary>
        protected HttpClient? HttpClient { get; }

        /// <summary>
        /// Gets a value indicating whether development mode is active.
        /// </summary>
        protected bool DevelopmentMode { get; }

        /// <summary>
        /// Gets full directory path for assets.
        /// </summary>
        protected string AssetBaseFilePath { get; }

        /// <summary>
        /// Gets full path for the manifest.
        /// </summary>
        protected string ManifestPath { get; }

        /// <summary>
        /// Gets the full file path.
        /// </summary>
        /// <param name="bundle">The bundle filename.</param>
        /// <returns>The full file path.</returns>
        public virtual async Task<string?> GetBundlePathAsync(string bundle)
        {
            if (string.IsNullOrEmpty(bundle))
            {
                return null;
            }

            var file = await GetFromManifestAsync(bundle).ConfigureAwait(false);

            return file != null
                ? $"{AssetPath}{file}"
                : null;
        }

        /// <summary>
        /// Gets a html script tag for the specified asset.
        /// </summary>
        /// <param name="bundle">The name of the Webpack bundle.</param>
        /// <param name="load">Enum for modifying script load behavior.</param>
        /// <returns>An HtmlString containing the html script tag.</returns>
        public virtual async Task<HtmlString> GetScriptTagAsync(string bundle, ScriptLoad load = ScriptLoad.Normal)
        {
            return await GetScriptTagAsync(bundle, null, load).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a html script tag for the specified asset.
        /// </summary>
        /// <param name="bundle">The name of the Webpack bundle.</param>
        /// <param name="fallbackBundle">The name of the bundle to fallback to if main bundle does not exist.</param>
        /// <param name="load">Enum for modifying script load behavior.</param>
        /// <returns>An HtmlString containing the html script tag.</returns>
        public virtual async Task<HtmlString> GetScriptTagAsync(string bundle, string? fallbackBundle, ScriptLoad load = ScriptLoad.Normal)
        {
            if (string.IsNullOrEmpty(bundle))
            {
                return HtmlString.Empty;
            }

            if (!Path.HasExtension(bundle))
            {
                bundle += ".js";
            }

            var file = await GetFromManifestAsync(bundle).ConfigureAwait(false);

            if (file == null && fallbackBundle != null)
            {
                file = await GetFromManifestAsync(fallbackBundle).ConfigureAwait(false);
            }

            return file != null
                ? new HtmlString(BuildScriptTag(file, load))
                : HtmlString.Empty;
        }

        /// <summary>
        /// Gets a html link tag for the specified asset.
        /// </summary>
        /// <param name="bundle">The name of the Webpack bundle.</param>
        /// <param name="fallbackBundle">The name of the bundle to fallback to if main bundle does not exist.</param>
        /// <returns>An HtmlString containing the html link tag.</returns>
        public virtual async Task<HtmlString> GetLinkTagAsync(string bundle, string? fallbackBundle = null)
        {
            if (string.IsNullOrEmpty(bundle))
            {
                return HtmlString.Empty;
            }

            if (!Path.HasExtension(bundle))
            {
                bundle += ".css";
            }

            var file = await GetFromManifestAsync(bundle).ConfigureAwait(false);

            if (file == null && fallbackBundle != null)
            {
                file = await GetFromManifestAsync(fallbackBundle).ConfigureAwait(false);
            }

            return file != null
                ? new HtmlString(BuildLinkTag(file))
                : HtmlString.Empty;
        }

        /// <summary>
        /// Gets a html style tag for the specified asset.
        /// </summary>
        /// <param name="bundle">The name of the Webpack bundle.</param>
        /// <param name="fallbackBundle">The name of the bundle to fallback to if main bundle does not exist.</param>
        /// <returns>An HtmlString containing the html style tag.</returns>
        public virtual async Task<HtmlString> GetStyleTagAsync(string bundle, string? fallbackBundle = null)
        {
            if (string.IsNullOrEmpty(bundle))
            {
                return HtmlString.Empty;
            }

            if (!Path.HasExtension(bundle))
            {
                bundle += ".css";
            }

            var file = await GetFromManifestAsync(bundle).ConfigureAwait(false);

            if (file == null && fallbackBundle != null)
            {
                file = await GetFromManifestAsync(fallbackBundle).ConfigureAwait(false);
            }

            return file != null
                ? new HtmlString(await BuildStyleTagAsync(file).ConfigureAwait(false))
                : HtmlString.Empty;
        }

        /// <summary>
        /// Gets the asset filename from the Webpack manifest.
        /// </summary>
        /// <param name="bundle">The name of the Webpack bundle.</param>
        /// <returns>The asset filename.</returns>
        protected virtual async Task<string?> GetFromManifestAsync(string bundle)
        {
            JsonDocument manifest;

            if (_manifest == null)
            {
                var json = DevelopmentMode
                    ? await FetchDevelopmentManifestAsync(HttpClient, ManifestPath).ConfigureAwait(false)
                    : await File.ReadAllTextAsync(ManifestPath).ConfigureAwait(false);

                manifest = JsonDocument.Parse(json);
                if (!DevelopmentMode)
                {
                    _manifest = manifest;
                }
            }
            else
            {
                manifest = _manifest;
            }

            try
            {
                return manifest.RootElement.GetProperty(bundle).GetString();
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Builds the script tag.
        /// </summary>
        /// <param name="file">The JS file to use in the tag.</param>
        /// <param name="load">Enum for modifying script load behavior.</param>
        /// <returns>A string containing the script tag.</returns>
        protected virtual string BuildScriptTag(string file, ScriptLoad load)
        {
            var crossOrigin = string.Empty;
            if (DevelopmentMode)
            {
                crossOrigin = "crossorigin=\"anonymous\"";
            }

            var loadType = DevelopmentMode ? " " : string.Empty;
            switch (load)
            {
                case ScriptLoad.Normal:
                    break;
                case ScriptLoad.Async:
                    loadType += "async";
                    break;
                case ScriptLoad.Defer:
                    loadType += "defer";
                    break;
                case ScriptLoad.AsyncDefer:
                    loadType += "async defer";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(load), load, null);
            }

            return $"<script src=\"{AssetPath}{file}\" {crossOrigin}{loadType}></script>";
        }

        /// <summary>
        /// Builds the link/style tag.
        /// </summary>
        /// <param name="file">The CSS file to use in the tag.</param>
        /// <returns>A string containing the link/style tag.</returns>
        protected virtual string BuildLinkTag(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return $"<link href=\"{AssetPath}{file}\" rel=\"stylesheet\" />";
        }

        /// <summary>
        /// Builds the link/style tag.
        /// </summary>
        /// <param name="file">The CSS file to use in the tag.</param>
        /// <returns>A string containing the link/style tag.</returns>
        protected virtual async Task<string> BuildStyleTagAsync(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!DevelopmentMode && _inlineStyles.ContainsKey(file))
            {
                return _inlineStyles[file];
            }

            var filename = file;
            var queryIndex = filename.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex != -1)
            {
                filename = filename.Substring(0, queryIndex);
            }

            var fullPath = $"{AssetBaseFilePath}{filename}";

            var style = DevelopmentMode
                ? await FetchDevelopmentStyleAsync(HttpClient, fullPath).ConfigureAwait(false)
                : await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);

            var result = $"<style>{style}</style>";

            if (!DevelopmentMode)
            {
                _inlineStyles.Add(file, result);
            }

            return result;
        }

        /// <summary>
        /// Fetch the manifest from dev server.
        /// </summary>
        /// <param name="httpClient">The HttpClient.</param>
        /// <param name="manifestPath">Path for the manifest file.</param>
        /// <returns>The manifest content.</returns>
        /// <exception cref="ArgumentNullException">If HttpClient is null.</exception>
        /// <exception cref="Exception">If dev server cannot be reached.</exception>
        // ReSharper disable once MemberCanBeMadeStatic.Global
        protected virtual async Task<string> FetchDevelopmentManifestAsync(HttpClient? httpClient, string manifestPath)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            try
            {
                return await httpClient.GetStringAsync(new Uri(manifestPath)).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("Webpack Dev Server not started!");
            }
        }

        /// <summary>
        /// Fetch the CSS file content from dev server.
        /// </summary>
        /// <param name="httpClient">The HttpClient.</param>
        /// <param name="fullPath">Path to CSS file.</param>
        /// <returns>The CSS file content.</returns>
        /// <exception cref="ArgumentNullException">If HttpClient is null.</exception>
        // ReSharper disable once MemberCanBeMadeStatic.Global
        protected virtual async Task<string> FetchDevelopmentStyleAsync(HttpClient? httpClient, string fullPath)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return await httpClient.GetStringAsync(new Uri(fullPath)).ConfigureAwait(false);
        }
    }
}
