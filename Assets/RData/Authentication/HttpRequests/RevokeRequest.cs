using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData.Http;

namespace RData.Authentication.HttpRequests
{
    public class RevokeRequest : RDataHttpRequest<RevokeRequest.RevokeResponse>
    {
        public class RevokeResponse
        {
            public bool result;
        }

        private string _refreshToken;

        public override string Method
        {
            get { return kHttpVerbPOST; }
        }

        public override string Path
        {
            get { return "/auth/revoke"; }
        }

        public override Dictionary<string, string> Parameters
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "refreshToken", _refreshToken }
                };
            }
        }

        public RevokeRequest(string refreshToken)
        {
            _refreshToken = refreshToken;
        }
    }
}