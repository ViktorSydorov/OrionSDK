using System.ServiceModel;
using SolarWinds.InformationService.Contract2;
using SwqlStudio.Properties;

namespace SwqlStudio
{
    internal class OrionInfoServiceJwtToken : InfoServiceBase
    {
        public OrionInfoServiceJwtToken(string token)
        {
            _endpoint = Settings.Default.OrionV3EndpointPath;
            _endpointConfigName = "OrionTcpBinding_InformationServicev3";
            _binding = new NetTcpBinding("TransportMessage");
            _credentials = new JwtTokenCredential(token);
        }

        public override string ServiceType => "Orion (v3) JWT Token";
    }
}
