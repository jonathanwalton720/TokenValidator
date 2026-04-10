using System;

namespace JonathanWalton720.TokenValidator
{
    public interface ILogger
    {
        void LogError(string message);
        void LogError(string message, Exception ex);
    }
}