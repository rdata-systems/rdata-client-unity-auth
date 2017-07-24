using RData.JsonRpc;

namespace RData.Authentication.Exceptions
{
    /// <summary>
    /// Used when authorization on the data collection server fails - data collection server is not available
    /// </summary>
    public class RDataServerNotAvailableException : RData.Authentication.Exceptions.RDataAuthorizationException
    {

        public RDataServerNotAvailableException()
        {
        }

        public RDataServerNotAvailableException(string message)
        : base(message)
        {
        }

        public RDataServerNotAvailableException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        public RDataServerNotAvailableException(JsonRpcError<string> error)
        : base(error)
        {
        }
    }
}