using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Jobs
{
    [ValueObject<Uri>(Conversions.SystemTextJson, typeof(ValidationException))]
    public partial struct CallbackUri
    {
        public static CallbackUri None => From(new Uri("none://"));
    }
}
