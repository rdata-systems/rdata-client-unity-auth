using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Authentication.Exceptions
{
    public class UsernameTakenException : RDataAuthenticationException
    {

        public UsernameTakenException()
        {
        }

        public UsernameTakenException(string message)
        : base(message)
        {
        }

        public UsernameTakenException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        public UsernameTakenException(RData.Http.Exceptions.RDataHttpException inner)
        : base(inner)
        {
        }
    }
}