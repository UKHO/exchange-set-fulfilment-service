using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

//using static System.Net.WebRequestMethods;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Services
{
    public interface ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className);


        public Task<byte[]> GetSamplePDF();
    }

    public class FakeSnippetService : ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className) => Task.FromResult("Source code view is disabled");

        public Task<byte[]> GetSamplePDF() => throw new NotImplementedException();
    }

    public class LocalSnippetService : ICodeSnippetService
    {
        public async Task<string> GetCodeSnippet(string className)
        {
            var basePath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.FullName;
            const string projectName = "UKHO.ADDS.EFS.Orchestrator.Dashboard";
            var classPath = projectName + className.Substring(projectName.Length).Replace(".", Path.DirectorySeparatorChar.ToString());
            var codePath = Path.Combine(basePath, $"{classPath}.razor");

            if (File.Exists(codePath))
            {
                return await Task.FromResult(File.ReadAllText(codePath));
            }

            return await Task.FromResult($"Unable to find code at {codePath}");
        }

        public async Task<byte[]> GetSamplePDF()
        {
            var path99 = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.FullName + "\\UKHO.ADDS.EFS.Orchestrator.Dashboard\\wwwroot\\pdf\\sample.pdf";

            return await File.ReadAllBytesAsync(path99);
        }
    }

    public class GitHubSnippetService : ICodeSnippetService
    {
        private const string baseUrl = "https://tabblazor.com/_content/razor_source";
        private readonly IHttpClientFactory httpClientFactory;
        private readonly NavigationManager navManager;
        private readonly Dictionary<string, string> cachedCode = new();

        public GitHubSnippetService(IHttpClientFactory httpClientFactory, NavigationManager navManager)
        {
            this.httpClientFactory = httpClientFactory;
            this.navManager = navManager;
        }

        public async Task<string> GetCodeSnippet(string className)
        {
            try
            {
                if (!cachedCode.ContainsKey(className))
                {
                    var baseName = "UKHO.ADDS.EFS.Orchestrator.Dashboard.";
                    var path = baseUrl + "/" + className.Replace(baseName, "").Replace(".", "/") + ".razor";

                    using var httpClient = httpClientFactory.CreateClient("GitHub");
                    using var stream = await httpClient.GetStreamAsync(path);
                    var reader = new StreamReader(stream);

                    var code = reader.ReadToEnd();

                    if (!cachedCode.ContainsKey(className))
                    {
                        cachedCode[className] = code;
                    }
                }

                return cachedCode[className];
            }
            catch (Exception ex)
            {
                return $"Unable to load code. Error: {ex.Message}";
            }
        }

        public async Task<byte[]> GetSamplePDF()
        {
            var url = navManager.BaseUri + "_content/UKHO.ADDS.EFS.Orchestrator.Dashboard/pdf/sample.pdf";
            using var httpClient = httpClientFactory.CreateClient("GitHub");
            var arr = await httpClient.GetByteArrayAsync(url);
            return arr;
        }
    }
}
