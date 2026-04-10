using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonathanWalton720.TokenValidator
{
    public class TokenPayload
    {
        public string UserName;
        public int UserID;
        public DateTimeOffset TokenExpirationTime;
        public string Issuer;
        public string Audience;

        public bool HasExpired
        {
            get
            {
                //less than zero means that the current time is less than the expiration time
                return DateTimeOffset.Compare(TokenExpirationTime, DateTime.Now) < 0;
            }
        }

        public DateTime TokenIssuedTime { get; set; }
        public string LastAccessOutTime { get; set; }
    }
}
