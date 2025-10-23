using System.Net;
using UKHO.ADDS.EFS.Domain.External;

namespace UKHO.ADDS.EFS.Domain.ExternalErrors
{
    public class ExternalServiceError
    {
        public HttpStatusCode ErrorResponseCode { get; set; }

        public ExternalServiceName ServiceName { get; set; }
    }
}
