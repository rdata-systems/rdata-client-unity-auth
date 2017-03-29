using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Http.Exceptions
{
    public class ForbiddenException : RDataHttpException
    {

        public ForbiddenException()
        {
        }

        public ForbiddenException(string message)
        : base(message)
        {
        }

        public ForbiddenException(string message, RDataHttpException inner)
        : base(message, inner)
        {
        }

        public ForbiddenException(string message, RDataApiError error)
        : base(message, error)
        {
        }
    }
}