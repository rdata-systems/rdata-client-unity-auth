using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData;
using RData.JsonRpc;
using RData.Requests;
using RData.Responses;
using RData.LitJson;

namespace RData.Authentication.JsonRpcRequests
{
    public class JwtAuthorizationRequest : JsonRpcRequest<JwtAuthorizationRequest.Parameters, BooleanResponse>
    {
        [JsonAlias("method")]
        public override string Method
        {
            get { return "authorize"; }
        }

        [JsonIgnore]
        public override bool IsBulked
        {
            get { return false; }
        }

        public class Parameters
        {
            [RData.LitJson.JsonAlias("accessToken")]
            public string AccessToken { get; set; }

            [RData.LitJson.JsonAlias("gameVersion")]
            public int GameVersion { get; set; }

            [RData.LitJson.JsonAlias("selectedGroups")]
            public string[] SelectedGroups { get; set; }
        }

        public JwtAuthorizationRequest() : base() { }

        public JwtAuthorizationRequest(string accessToken, int gameVersion, string[] selectedGroups=null) : base()
        {
            Params = new Parameters()
            {
                AccessToken = accessToken,
                GameVersion = gameVersion,
                SelectedGroups = selectedGroups
            };
        }
    }
}