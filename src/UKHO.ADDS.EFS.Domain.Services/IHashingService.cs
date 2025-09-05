namespace UKHO.ADDS.EFS.Domain.Services
{
    public interface IHashingService
    {
        string CalculateHash(string value);
    }
}
