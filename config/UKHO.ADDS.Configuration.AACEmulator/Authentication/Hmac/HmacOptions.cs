using Microsoft.AspNetCore.Authentication;

namespace UKHO.ADDS.Configuration.AACEmulator.Authentication.Hmac
{
    public class HmacOptions : AuthenticationSchemeOptions
    {
        public string Credential { get; set; } = default!;

        public string Secret { get; set; } = default!;
    }
}
