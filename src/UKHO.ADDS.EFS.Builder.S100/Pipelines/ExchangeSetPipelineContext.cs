using System.Diagnostics.CodeAnalysis;
using HandlebarsDotNet;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    [ExcludeFromCodeCoverage]
    internal class ExchangeSetPipelineContext
    {
        private readonly IConfiguration _configuration;
        private readonly INodeStatusWriter _nodeStatusWriter;
        private readonly IToolClient _toolClient;
        private readonly ILoggerFactory _loggerFactory;

        // Templates for exchange set filenames
        private const string LowerEnvironmentTemplate = "V01X01_{{JobId}}.zip";
        private const string HigherEnvironmentTemplate = "V01X01.zip";

        // Environment names that are considered lower environments
        private static readonly string[] LowerEnvironments = { "Development", "Dev" };

        // Compiled Handlebars templates for better performance
        private static readonly HandlebarsTemplate<object, object> _lowerEnvTemplate;
        private static readonly HandlebarsTemplate<object, object> _higherEnvTemplate;

        // Static constructor to compile the templates
        static ExchangeSetPipelineContext()
        {
            // Register and compile templates
            _lowerEnvTemplate = Handlebars.Compile(LowerEnvironmentTemplate);
            _higherEnvTemplate = Handlebars.Compile(HigherEnvironmentTemplate);
        }

        public ExchangeSetPipelineContext(IConfiguration configuration, INodeStatusWriter nodeStatusWriter, IToolClient toolClient, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _nodeStatusWriter = nodeStatusWriter;
            _toolClient = toolClient;
            _loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration => _configuration;

        public IToolClient ToolClient => _toolClient;

        public INodeStatusWriter NodeStatusWriter => _nodeStatusWriter;

        public ILoggerFactory LoggerFactory => _loggerFactory;

        public string JobId { get; set; }
        public bool IsDebugSession { get; set; }
        public string FileShareEndpoint { get; set; }
        public string BuildServiceEndpoint { get; set; }
        public string WorkspaceAuthenticationKey { get; set; }
        public ExchangeSetJob Job { get; set; }
        public IEnumerable<BatchDetails> BatchDetails { get; set; }
        public string BatchId { get; set; }
        public string WorkSpaceRootPath { get; set; } = "/usr/local/tomcat/ROOT";
        public string WorkSpaceSpoolPath { get; } = "spool";
        public string WorkSpaceSpoolDataSetFilesPath { get; } = "dataSet_files";
        public string WorkSpaceSpoolSupportFilesPath { get; } = "support_files";
        public string ExchangeSetFileName => GetExchangeSetFileName();
        public string ExchangeSetFilePath { get; set; } = "/usr/local/tomcat/ROOT/xchg";
        public string ExchangeSetArchiveFolderName { get; set; } = "ExchangeSetArchive";

        /// <summary>
        /// Gets the exchange set file name based on the current environment.
        /// </summary>
        /// <returns>The appropriate file name for the current environment using Handlebars templates.</returns>
        private string GetExchangeSetFileName()
        {
            // Get the current environment name
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Create template data object
            var templateData = new { JobId };

            // Determine if we're in a lower environment and have a valid JobId
            var isLowerEnvironment = LowerEnvironments.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
            var hasValidJobId = !string.IsNullOrEmpty(JobId);

            // Select the appropriate template based on environment and JobId
            return (isLowerEnvironment && hasValidJobId) ? _lowerEnvTemplate(templateData) : _higherEnvTemplate(templateData);
        }
    }
}
