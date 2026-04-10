using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace JonathanWalton720.TokenValidator
{
    /// <summary>
    /// The TokenValidator class.
    /// </summary>
    public class TokenValidator
    {
        private static MemoryCache _cache = new MemoryCache("TokenValidator");
        private static ConcurrentDictionary<string, Task> tasks = new ConcurrentDictionary<string, Task>();

        /// <summary>
        /// Cache for handling many validation requests for the same authentication header value.
        /// </summary>
        public static MemoryCache Cache
        {
            get
            {
                return _cache;
            }
            set
            {
                _cache = value;
            }
        }

        /// <summary>
        /// Cache expiration for validation requests in seconds.
        /// </summary>
        public const int CacheExpirationSeconds = 5;

        private HttpClient httpClient;
        private Uri authorizationServerEndpoint;
        private ILogger logger;

        /// <summary>
        /// Initializes a new instance of the TokenValidator class.
        /// </summary>
        /// <param name="authorizationServerDomain">The authorization server domain for validation requests.</param>
        /// <param name="logger">The ILogger to log validation errors.</param>
        /// <param name="httpMessageHandler">The HttpMessageHandler for sending requests.</param>
        public TokenValidator(string authorizationServerDomain, ILogger logger = null, HttpMessageHandler httpMessageHandler = null)
        {
            if (string.IsNullOrWhiteSpace(authorizationServerDomain))
            {
                throw new ArgumentException("authorizationServerDomain is required");
            }
            var baseUri = new Uri(authorizationServerDomain);
            this.authorizationServerEndpoint = new Uri(baseUri, "api/Validate");
            this.httpClient = new HttpClient(httpMessageHandler ?? new HttpClientHandler());
            this.logger = logger;
        }

        /// <summary>
        /// Validates the bearer authorization header from client requests.
        /// </summary>
        /// <param name="authenticationHeaderValue">The authentication header value to validate.</param>
        public void Validate(AuthenticationHeaderValue authenticationHeaderValue)
        {
            if (authenticationHeaderValue == null)
            {
                throw new ArgumentNullException(nameof(authenticationHeaderValue));
            }
            if (!string.Equals(authenticationHeaderValue.Scheme, "bearer", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("must use bearer authentication");
            }
            if (string.IsNullOrWhiteSpace(authenticationHeaderValue.Parameter))
            {
                throw new ArgumentException("bearer authentication is missing");
            }
            bool? isValid = GetValueFromCache(authenticationHeaderValue);

            if (!isValid.HasValue)
            {
                Task task;
                tasks.TryGetValue(authenticationHeaderValue.Parameter, out task);
                if (task == null)
                {
                    task = Task.Run(() => RequestValidationAsync(authenticationHeaderValue));
                    tasks.TryAdd(authenticationHeaderValue.Parameter, task);
                }
                task.Wait();
                isValid = GetValueFromCache(authenticationHeaderValue);
            }
            if (!isValid.HasValue)
            {
                var message = "an error occurred, could not retrieve validation from the cache";
                TryLogError(message);
                throw new TokenException(message);
            }
            if (isValid.HasValue && !isValid.Value)
            {
                throw new TokenException("you are not authorized");
            }
        }

        private bool? GetValueFromCache(AuthenticationHeaderValue authenticationHeaderValue)
        {
            var value = Cache.Get(authenticationHeaderValue.Parameter);
            bool? isValid = null;
            if (value is bool)
            {
                isValid = (bool)value;
            }

            return isValid;
        }

        private void RequestValidationAsync(AuthenticationHeaderValue authenticationHeaderValue)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, authorizationServerEndpoint);
                request.Headers.Authorization = authenticationHeaderValue;
                var response = httpClient.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    Cache.Add(authenticationHeaderValue.Parameter, true, DateTime.Now.AddSeconds(CacheExpirationSeconds));
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Cache.Add(authenticationHeaderValue.Parameter, false, DateTime.Now.AddSeconds(CacheExpirationSeconds));
                }
                else
                {
                    TryLogError("Could not connect to authorization endpoint. " + authorizationServerEndpoint);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                TryLogError(ex.Message, ex);
            }
            finally
            {
                Task t;
                tasks.TryRemove(authenticationHeaderValue.Parameter, out t);
            }
        }

        private void TryLogError(string message, Exception ex = null)
        {
            if (logger != null)
            {
                logger.LogError(message, ex);
            }
        }
    }
}
