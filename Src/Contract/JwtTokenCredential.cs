using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace SolarWinds.InformationService.Contract2
{
    [DataContract(Name = "JwtToken", Namespace = "http://schema.solarwinds.com/2007/08/IS")]
    public class JwtTokenCredential : ServiceCredentials
    {
        public override CredentialType CredentialType
        {
            get { return CredentialType.JwtToken; }
        }

        [field: DataMember(Name = "Token", Order = 1, IsRequired = true)]
        public string Token { get; }

        public JwtTokenCredential(string token)
        {
            Token = token;
        }

        public override void ApplyTo(ChannelFactory channelFactory)
        {
            channelFactory.Endpoint.Address = new EndpointAddress(channelFactory.Endpoint.Address.Uri);

            channelFactory.Credentials.UserName.UserName = Token;
            channelFactory.Credentials.UserName.Password = string.Empty;

            channelFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            channelFactory.Credentials.ServiceCertificate.Authentication.CustomCertificateValidator = new AllTrustingCertificateValidator();
        }
    }
}
