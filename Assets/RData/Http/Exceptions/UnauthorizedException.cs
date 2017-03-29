using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Http.Exceptions
{
    public class UnauthorizedException : RDataHttpException
    {

        public UnauthorizedException()
        {
        }

        public UnauthorizedException(string message)
        : base(message)
        {
        }

        public UnauthorizedException(string message, RDataHttpException inner)
        : base(message, inner)
        {
        }
        
        public UnauthorizedException(string message, RDataApiError error)
        : base(message, error)
        {
        }
    }
}