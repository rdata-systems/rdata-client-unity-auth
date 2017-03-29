using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData.Http;

namespace RData.Authentication.HttpRequests
{
    public class RefreshRequest : RDataHttpRequest<RefreshRequest.RefreshResponse>
    {
        public class RefreshResponse
        {
            public string accessToken;
        }

        private string _refreshToken;

        public override string Method
        {
            get { return kHttpVerbPOST; }
        }

        public override string Path
        {
            get { return "/auth/refresh"; }
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

        public RefreshRequest(string refreshToken)
        {
            _refreshToken = refreshToken;
        }
    }
}