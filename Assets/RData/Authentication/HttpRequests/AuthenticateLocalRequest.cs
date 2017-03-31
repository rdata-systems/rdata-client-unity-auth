using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData.Http;
using System;

namespace RData.Authentication.HttpRequests
{
    public class AuthenticateLocalRequest : RDataHttpRequest<AuthenticateLocalRequest.AuthenticateLocalResponse>
    {
        public class AuthenticateLocalResponse : RDataHttpResponse
        {
            public string refreshToken;
            public string accessToken;
        }

        private string _username;
        private string _password;

        public override string Method
        {
            get { return kHttpVerbPOST; }
        }

        public override string Path
        {
            get { return "/auth/local/authenticate"; }
        }

        public override Dictionary<string, string> Parameters
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "username", _username },
                    { "password", _password }
                };
            }
        }

        public AuthenticateLocalRequest(string username, string password)
        {
            _username = username;
            _password = password;
        }
    }
}