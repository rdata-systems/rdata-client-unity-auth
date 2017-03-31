using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Authentication.Exceptions
{
    public class EmailTakenException : RDataAuthenticationException
    {

        public EmailTakenException()
        {
        }

        public EmailTakenException(string message)
        : base(message)
        {
        }

        public EmailTakenException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        public EmailTakenException(RData.Http.Exceptions.RDataHttpException inner)
        : base(inner)
        {
        }
    }
}