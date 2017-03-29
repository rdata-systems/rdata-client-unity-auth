using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Http.Exceptions
{
    public class BadRequestException : RDataHttpException
    {

        public BadRequestException()
        {
        }

        public BadRequestException(string message)
        : base(message)
        {
        }

        public BadRequestException(string message, RDataHttpException inner)
        : base(message, inner)
        {
        }

        public BadRequestException(string message, RDataApiError error)
        : base(message, error)
        {
        }
    }
}