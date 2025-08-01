using System.Dynamic;
using HandlebarsDotNet;

namespace UKHO.ADDS.EFS.Utilities
{
    public class FileNameGenerator
    {
        private readonly string _template;

        public const string JobId = "jobid";
        public const string Date = "date";

        public FileNameGenerator(string template)
        {
            _template = template;
        }

        public string GenerateFileName(string jobId, DateTime? date = null)
        {
            // Template uses [] rather than {{ }} to avoid being swapped out by the configuration service
            var templateString = _template.Replace("[", "{{").Replace("]", "}}");

            var template = Handlebars.Compile(templateString);

            var model = new ExpandoObject();
            model.TryAdd(JobId, jobId);

            date ??= DateTime.UtcNow;

            model.TryAdd(Date, date.Value.ToString("yyyyMMdd"));

            return template(model);
        }
    }
}
