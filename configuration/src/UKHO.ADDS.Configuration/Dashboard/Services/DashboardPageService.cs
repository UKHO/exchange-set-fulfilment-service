using UKHO.ADDS.Configuration.Dashboard.Models;

namespace UKHO.ADDS.Configuration.Dashboard.Services
{
    public class DashboardPageService
    {
        private readonly DashboardPage[] _allPages =
        {
            new DashboardPage { Name = "Configuration", Path = "/", Icon = "\ue88a" }
        };

        public IEnumerable<DashboardPage> Pages => _allPages;

        public DashboardPage FindCurrent(Uri uri)
        {
            IEnumerable<DashboardPage> Flatten(IEnumerable<DashboardPage> e)
            {
                return e.SelectMany(c => c.Children != null ? Flatten(c.Children) : new[] { c });
            }

            return Flatten(Pages)
                .FirstOrDefault(example => example.Path == uri.AbsolutePath || $"/{example.Path}" == uri.AbsolutePath);
        }

        public string TitleFor(DashboardPage example)
        {
            if (example != null && example.Name != "Overview")
            {
                return example.Title ?? "";
            }

            return "";
        }

        public string DescriptionFor(DashboardPage example) => example?.Description ?? "";
    }
}
