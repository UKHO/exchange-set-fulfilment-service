using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TabBlazor.Components.QuickTables;
using UKHO.ADDS.EFS.Orchestrator.Dashboard.Data;

public interface IDataService
{
    IQueryable<Country> Countries { get; }
    Task<GridItemsProviderResult<Country>> GetCountriesAsync(int startIndex, int? count, string sortBy, bool sortAscending, CancellationToken cancellationToken);
}
