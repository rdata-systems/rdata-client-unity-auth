using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData.Http;
using System;

namespace RData.Authentication.HttpRequests
{
    public class RegisterLocalRequest : RDataHttpRequest<RegisterLocalRequest.RegisterLocalResponse>
    {
        public class RegisterLocalResponse
        {
            public JwtUser user;
        }

        private string _email;
        private string _username;
        private string _password;

        public override string Method
        {
            get { return kHttpVerbPOST; }
        }

        public override string Path
        {
            get { return "/auth/local/register"; }
        }

        public override Dictionary<string, string> Parameters
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "username", _username },
                    { "email", _email },
                    { "password", _password }
                };
            }
        }

        public RegisterLocalRequest(string username, string email, string password)
        {
            _username = username;
            _email = email;
            _password = password;
        }
    }
}