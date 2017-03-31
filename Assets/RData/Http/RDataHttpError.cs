using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData.LitJson;

namespace RData.Http
{
    public class RDataApiError
    {
        [JsonAlias("message")]
        public string Message { get; set; }

        [JsonAlias("name")]
        public string Name { get; set; }
    }
}