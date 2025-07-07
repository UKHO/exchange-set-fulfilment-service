using System.Dynamic;
using HandlebarsDotNet;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builder.Common.Pipelines.Distribute
{
    public class FileNameGenerator<T> where T : ExchangeSetJob
    {
        public const string JobId = "jobid";
        public const string Date = "date";

        private readonly ExchangeSetPipelineContext<T> _context;

        public FileNameGenerator(ExchangeSetPipelineContext<T>context)
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
