using System.Dynamic;
using HandlebarsDotNet;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;

namespace UKHO.ADDS.EFS.Infrastructure.Services
{
    internal class TemplateFileNameGeneratorService : IFileNameGeneratorService
    {
        public const string JobId = "jobid";
        public const string Date = "date";

        public string GenerateFileName(string template, JobId jobId, DateTime? date = null)
        {
            // Template uses [] rather than {{ }} to avoid being swapped out by the configuration service
            var templateString = template.Replace("[", "{{").Replace("]", "}}");

            var compiledTemplate = Handlebars.Compile(templateString);

            var model = new ExpandoObject();
            model.TryAdd(JobId, jobId);

            date ??= DateTime.UtcNow;

            model.TryAdd(Date, date.Value.ToString("yyyyMMdd"));

            return compiledTemplate(model);
        }
    }
}
