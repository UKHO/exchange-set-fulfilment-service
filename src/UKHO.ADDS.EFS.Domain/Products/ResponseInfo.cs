using System.Net;

namespace UKHO.ADDS.EFS.Domain.Products
{
    public class ResponseInfo
    {
        public HttpStatusCode ResponseCode { get; set; }

        public DateTime? LastModified { get; set; }
    }
}
