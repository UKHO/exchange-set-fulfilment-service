namespace UKHO.ADDS.EFS.Jobs.S63
{
    public class S63ExchangeSetJob : ExchangeSetJob
    {
        public List<string>? Products { get; set; }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p));

        public override int GetProductCount() => (Products == null) ? 0 : Products?.Count ?? 0;
    }
}
