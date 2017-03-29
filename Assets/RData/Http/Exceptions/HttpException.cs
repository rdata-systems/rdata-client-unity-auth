using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RData.Http.Exceptions
{
    public class RDataHttpException : RData.Exceptions.RDataException
    {
        public RDataApiError ApiError;
        
        public bool HasApiError
        {
            get { return ApiError != null; }
        }

        public RDataHttpException()
        {
        }

        public RDataHttpException(string message)
        : base(message)
        {
        }

        public RDataHttpException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        public RDataHttpException(string message, RDataApiError error)
        : base(message)
        {
            this.ApiError = error;
        }
    }
}