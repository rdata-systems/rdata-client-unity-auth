using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Authentication.Exceptions
{
    public class InvalidCredentialsException : RDataAuthenticationException
    {

        public InvalidCredentialsException()
        {
        }

        public InvalidCredentialsException(string message)
        : base(message)
        {
        }

        public InvalidCredentialsException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        public InvalidCredentialsException(RData.Http.Exceptions.RDataHttpException inner)
        : base(inner)
        {
        }
    }
}