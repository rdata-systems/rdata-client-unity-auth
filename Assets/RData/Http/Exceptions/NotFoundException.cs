using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Http.Exceptions
{
    public class NotFoundException : RDataHttpException
    {

        public NotFoundException()
        {
        }

        public NotFoundException(string message)
        : base(message)
        {
        }

        public NotFoundException(string message, RDataHttpException inner)
        : base(message, inner)
        {
        }

        public NotFoundException(string message, RDataApiError error)
        : base(message, error)
        {
        }
    }
}