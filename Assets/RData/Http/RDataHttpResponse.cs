using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData.LitJson;

namespace RData.Http
{
    public abstract class RDataHttpResponse
    {
        [JsonAlias("error")]
        public RDataApiError Error { get; set; }

        public bool HasError
        {
            get { return Error != null; }
        }
    }
}