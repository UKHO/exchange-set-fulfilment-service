namespace UKHO.ADDS.EFS.Orchestrator.Api.Metadata
{
    internal static class MetadataExtensions
    {
        public static RouteHandlerBuilder WithRequiredHeader(this RouteHandlerBuilder builder, string name, string description, string defaultValue)
        {
            builder.Add(x => { x.Metadata.Add(new OpenApiHeaderParameter { Name = name, Description = description, Required = true, ExpectedValue = defaultValue }); });

            return builder;
        }
    }
}
