using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JonathanWalton720.TokenValidator;

namespace JonathanWalton720.TokenValidatorTests
{
    public class FakeTokenLogger: ILogger
    {
        public void LogError(string message)
        {
            // do nothing
        }

        public void LogError(string message, Exception ex)
        {
            // do nothing
        }
    }
}
