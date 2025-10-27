using System.Net;
using UKHO.ADDS.EFS.Domain.External;

namespace UKHO.ADDS.EFS.Domain.ExternalErrors
{
    public class ExternalServiceError
    {
        public HttpStatusCode ErrorResponseCode { get; private set; }
        public ExternalServiceName ServiceName { get; private set; }

        public ExternalServiceError(HttpStatusCode errorResponseCode, ExternalServiceName serviceName)
        {
            ErrorResponseCode = errorResponseCode;
            ServiceName = serviceName;
        }
    }
}
