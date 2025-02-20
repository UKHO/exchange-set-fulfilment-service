using ADDSMock.Domain.Mappings;
using System.Net;

public void RegisterFragment(WireMockServer server, MockService mockService)
{
	this really should not build at all

    server
        .Given(
            Request.Create().WithPath("ess/health").UsingGet()
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("Healthy (override...)!"));
}
