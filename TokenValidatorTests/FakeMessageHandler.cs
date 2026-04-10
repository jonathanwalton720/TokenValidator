using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JonathanWalton720.TokenValidatorTests
{
    public class FakeMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode httpStatusCode;

        public FakeMessageHandler(HttpStatusCode httpStatusCode)
        {
            this.httpStatusCode = httpStatusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.StatusCode = httpStatusCode;
            return Task.Run(() => httpResponseMessage);
        }
    }
}
