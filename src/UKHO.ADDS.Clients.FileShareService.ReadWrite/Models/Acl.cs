namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; } = Enumerable.Empty<string>();

        public IEnumerable<string> ReadGroups { get; set; } = Enumerable.Empty<string>();
    }
}