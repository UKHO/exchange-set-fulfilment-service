namespace UKHO.ADDS.Configuration.Aspire
{
    public class EndpointTemplateBuilder
    {
        private readonly List<EndpointTemplate> _templates;

        public EndpointTemplateBuilder()
        {
            _templates = [];
        }

        internal IEnumerable<EndpointTemplate> Templates => _templates;

        public void AddEndpoint(string name, IResourceBuilder<ProjectResource> resource, bool useHttps, string? hostname = null, string? path = null)
        {
            if (_templates.Any(x => x.Name.Equals(name)))
            {
                throw new ArgumentException($"A template named {name} already exists");
            }

            _templates.Add(new EndpointTemplate()
            {
                Name = name,
                Resource = resource,
                UseHttps = useHttps,
                Hostname = hostname,
                Path = path
            });
        }
    }
}
