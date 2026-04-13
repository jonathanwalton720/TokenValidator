using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace JonathanWalton720.TokenValidator
{
    public interface ITokenUtility
    {
        string CreateNewToken(string userName, int userID);
        int GetTokenExpirationSeconds();
        TokenPayload GetTokenPayload(string tokenValue);
        ClaimsPrincipal ValidateToken(string token);
    }

    public class Utility : ITokenUtility
    {
        private IConfiguration configuration;
        private ILogger logger;
        private string secretKeyString;
        private byte[] tokenSecretKey;
        private string tokenClientId;
        private string tokenIssuer;
        private string tokenLifeTime;
        private bool validateExpiration;

        public Utility(IConfiguration configuration, ILogger logger)
        {

            this.configuration = configuration;
            this.logger = logger;
            //get token keys
            secretKeyString = configuration.GetSection("TokenSecurity")["SecretKey"];
            tokenSecretKey = Convert.FromBase64String(secretKeyString);
            tokenClientId = configuration.GetSection("TokenSecurity")["ClientId"];
            tokenIssuer = configuration.GetSection("TokenSecurity")["Issuer"];
            tokenLifeTime = configuration.GetSection("TokenSecurity")["TokenLifeTime"];
            validateExpiration = bool.Parse(configuration.GetSection("TokenSecurity")["ValidateExpiration"]);
        }

        public TokenPayload GetTokenPayload(string tokenValue)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();

                //check if we can read the token
                if (!jwtHandler.CanReadToken(tokenValue))
                {
                    return null;
                }


                var decodedToken = jwtHandler.ReadJwtToken(tokenValue);

                var tokenPayload = new TokenPayload();

                tokenPayload.UserName = decodedToken.Claims.FirstOrDefault(x => x.Type == "unique_name").Value;
                tokenPayload.UserID = int.Parse(decodedToken.Claims.FirstOrDefault(x => x.Type == "primarysid").Value);
                tokenPayload.Issuer = decodedToken.Claims.FirstOrDefault(x => x.Type == "iss").Value;
                tokenPayload.Audience = decodedToken.Claims.FirstOrDefault(x => x.Type == "aud").Value;
                var expirationValue = int.Parse(decodedToken.Claims.FirstOrDefault(x => x.Type == "exp").Value);
                tokenPayload.TokenExpirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationValue).DateTime;
                var issuedValue = int.Parse(decodedToken.Claims.FirstOrDefault(x => x.Type == "nbf").Value);
                tokenPayload.TokenIssuedTime = DateTimeOffset.FromUnixTimeSeconds(issuedValue).DateTime;
                tokenPayload.LogoutTime = decodedToken.Claims.FirstOrDefault(x => x.Type == Constants.ClaimTypeLastAccessOutTime)?.Value;

                return tokenPayload;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                throw;
            }
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = validateExpiration,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = tokenIssuer,
                    ValidAudience = tokenClientId,
                    IssuerSigningKey = new SymmetricSecurityKey(tokenSecretKey)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                return principal;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                throw;
            }

        }

        public string CreateNewToken(string userName, int userID)
        {
            var key = new SymmetricSecurityKey(tokenSecretKey);

            var jwt = new JwtSecurityToken(
                issuer: tokenIssuer,
                audience: tokenClientId,
                claims: new Claim[] {
                    new Claim("unique_name", userName),
                    new Claim("primarysid",userID.ToString()),
                    new Claim(ClaimTypes.Name, userName)
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(tokenLifeTime)),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public int GetTokenExpirationSeconds()
        {
            //tokenLifeTime from config is in minutes.
            int lifetime;
            if (Int32.TryParse(tokenLifeTime, out lifetime))
            {
                return lifetime * 60;
            }
            else
            {
                return 0;
            }
        }

    }
}
