using RData.JsonRpc;

namespace RData.Authentication.Exceptions
{
    public class RDataAuthorizationException : RData.Exceptions.RDataException
    {

        public RDataAuthorizationException()
        {
        }

        public RDataAuthorizationException(string message)
        : base(message)
        {
        }

        public RDataAuthorizationException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        public RDataAuthorizationException(JsonRpcError<string> error)
        : base(error)
        {
        }
    }
}