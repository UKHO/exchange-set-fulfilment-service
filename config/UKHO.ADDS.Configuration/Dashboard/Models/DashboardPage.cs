namespace UKHO.ADDS.Configuration.Dashboard.Models
{
    public class DashboardPage
    {
        public bool New { get; set; }
        public bool Updated { get; set; }
        public bool Pro { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Path { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Expanded { get; set; }
        public IEnumerable<DashboardPage> Children { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public IEnumerable<DashboardPageSection> Toc { get; set; }
    }
}
