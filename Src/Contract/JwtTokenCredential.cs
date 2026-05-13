using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
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

            channelFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            channelFactory.Credentials.ServiceCertificate.Authentication.CustomCertificateValidator = new AllTrustingCertificateValidator();

            channelFactory.Endpoint.EndpointBehaviors.Add(new JwtTokenEndpointBehavior(Token));
        }
    }

    internal class JwtTokenEndpointBehavior : IEndpointBehavior
    {
        private readonly string _token;

        public JwtTokenEndpointBehavior(string token)
        {
            _token = token;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new JwtTokenMessageInspector(_token));
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }
    }

    internal class JwtTokenMessageInspector : IClientMessageInspector
    {
        private readonly string _token;

        public JwtTokenMessageInspector(string token)
        {
            _token = token;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            HttpRequestMessageProperty httpRequest;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object property))
            {
                httpRequest = (HttpRequestMessageProperty)property;
            }
            else
            {
                httpRequest = new HttpRequestMessageProperty();
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequest);
            }

            httpRequest.Headers["Authorization"] = "Bearer " + _token;
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
    }
}
