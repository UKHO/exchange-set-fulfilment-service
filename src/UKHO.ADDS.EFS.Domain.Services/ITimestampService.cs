using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Services
{
    public interface ITimestampService
    {
        /// <summary>
        ///     Gets the timestamp for a given job.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        Task<DateTime> GetTimestampForJobAsync(Job job);

        Task SetTimestampForJobAsync(Job job);
    }
}
