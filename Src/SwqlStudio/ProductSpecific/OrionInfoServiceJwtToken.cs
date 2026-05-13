using System.ServiceModel;
using SolarWinds.InformationService.Contract2;
using SwqlStudio.Properties;

namespace SwqlStudio
{
    internal class OrionInfoServiceJwtToken : InfoServiceBase
    {       

        public OrionInfoServiceJwtToken(string token)
        {
            
            //_endpoint = Settings.Default.OrionV3EndpointPathAD;
            //_endpointConfigName = "OrionWindowsTcpBinding";
            _binding = new NetTcpBinding("Windows");
            _credentials = new JwtTokenCredential(token);
        }

        public override string ServiceType => "Orion Jwt Token";
    }
}
