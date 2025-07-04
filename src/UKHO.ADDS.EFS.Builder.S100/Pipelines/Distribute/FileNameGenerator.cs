using System.Dynamic;
using HandlebarsDotNet;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class FileNameGenerator
    {
        public const string JobId = "jobid";
        public const string Date = "date";

        private readonly ExchangeSetPipelineContext _context;

        public FileNameGenerator(ExchangeSetPipelineContext context)
        {
            _context = context;
        }

        public string GenerateFileName(string? jobId = null, DateTime? date = null)
        {
            // Template uses [] rather than {{ }} to avoid being swapped out by the configuration service
            var templateString = _context.ExchangeSetNameTemplate.Replace("[", "{{").Replace("]", "}}");

            var template = Handlebars.Compile(templateString);

            var model = new ExpandoObject();
            model.TryAdd(JobId, jobId ?? _context.JobId);

            date ??= DateTime.UtcNow;

            model.TryAdd(Date, date.Value.ToString("yyyyMMdd"));

            return template(model);
        }
    }
}
