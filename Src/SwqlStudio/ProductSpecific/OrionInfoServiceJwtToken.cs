using System.Net;
using System.ServiceModel;
using SolarWinds.InformationService.Contract2;
using SwqlStudio.Properties;

namespace SwqlStudio
{
    internal class OrionInfoServiceJwtToken : InfoServiceBase
    {
        public OrionInfoServiceJwtToken(string token)
        {
            _endpoint = Settings.Default.OrionV3EndpointPathToken;
            _endpointConfigName = "OrionHttpBinding_JwtToken";
            _protocolName = "https";
            _binding = new BasicHttpBinding("SWIS.Over.HTTP.JwtToken");
            _credentials = new JwtTokenCredential(token);

            // Bypass SSL validation for self-signed Orion certs
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        public override string ServiceType => "Orion (v3) JWT Token";

        protected override int Port
        {
            get { return Settings.Default.DefaultInfoServiceHttpsPort; }
        }
    }
}
