using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Authentication.Exceptions
{
    public class RDataAuthenticationException : RData.Exceptions.RDataException
    {

        public RDataAuthenticationException()
        {
        }

        public RDataAuthenticationException(string message)
        : base(message)
        {
        }

        public RDataAuthenticationException(string message, System.Exception inner)
        : base(message, inner)
        {
        }
        
        public RDataAuthenticationException(RData.Http.Exceptions.RDataHttpException inner)
        : base((inner.HasApiError ? inner.ApiError.message : inner.Message), inner)
        {
        }
    }
}