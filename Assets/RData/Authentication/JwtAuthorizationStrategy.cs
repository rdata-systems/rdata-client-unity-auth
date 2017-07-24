using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData;
using RData.Responses;
using RData.JsonRpc;
using RData.Exceptions;
using RData.Authorization;
using RData.Authentication.Exceptions;

namespace RData.Authentication
{
    public class JwtAuthorizationStrategy : IAuthorizationStrategy
    {
        private RDataClient _rDataClient;
        private JwtAuthenticationClient _jwtAuthClient;
        
        public delegate void JwtRefreshTokenExpired();
        public event JwtRefreshTokenExpired OnJwtRefreshTokenExpired;
        
        private const float kAuthenticationRetryTime = 5.0f; // Seconds

        public bool Authorized { get; private set; }

        public string UserId
        {
            get { return _jwtAuthClient.UserId; }
            set { throw new System.NotImplementedException(string.Format("Manually setting the UserId while using {0} is not supported. Please use {1}.Authenticate()", typeof(JwtAuthorizationStrategy).Name, typeof(JwtAuthenticationClient).Name)); }
        }

        public RDataException LastError { get; private set; }

        public bool HasError { get { return LastError != null; } }

        public JwtAuthorizationStrategy(RDataClient rRataClient, JwtAuthenticationClient jwtAuthClient)
        {
            _rDataClient = rRataClient;
            _jwtAuthClient = jwtAuthClient;
        }

        public IEnumerator Authorize()
        {
            if (_rDataClient.Authorized)
            {
                LastError = new RDataAuthorizationException("Already authorized");
                yield break;
            }
            if (!_jwtAuthClient.Authenticated)
            {
                LastError = new RDataAuthorizationException(string.Format("You must call {0}.Authenticate before calling {1}.Authorize", typeof(JwtAuthenticationClient).Name, typeof(JwtAuthorizationStrategy).Name));
                yield break;
            }

            yield return CoroutineManager.StartCoroutine(SendAuthorizationRequest(_jwtAuthClient.AccessToken, _rDataClient.GameVersion, _jwtAuthClient.SelectedGroups));

            if (_rDataClient.Authorized)
                _rDataClient.OnAuthorized();
        }

        public IEnumerator RestoreAuthorization()
        {
            if(_jwtAuthClient.RefreshTokenExpired)
            {
                if (OnJwtRefreshTokenExpired != null)
                    OnJwtRefreshTokenExpired();
                yield break;
            }

            while (_jwtAuthClient.AccessTokenExpired)
            { 
                yield return CoroutineManager.StartCoroutine(_jwtAuthClient.Refresh());
                if (_jwtAuthClient.HasError)
                {
                    Debug.LogError(string.Format("JwtAuthorizationStrategy failed to re-authenticate, retrying in {0} seconds", kAuthenticationRetryTime));
                    yield return new WaitForSeconds(kAuthenticationRetryTime);
                }
            }

            yield return CoroutineManager.StartCoroutine(SendAuthorizationRequest(_jwtAuthClient.AccessToken, _rDataClient.GameVersion, _jwtAuthClient.SelectedGroups));
        }

        public void ResetAuthorization()
        {
            Authorized = false;
        }

        private IEnumerator SendAuthorizationRequest(string accessToken, int gameVersion, string[] selectedGroups = null)
        {
            if (!_rDataClient.IsAvailable)
            {
                // If we made it this far, there is probably no reason for data collection not to authorize us when the connection is available again. 
                // Pretend that we will be to start collecting data that will be sent once we reconnect to the server
                Authorized = true;
                LastError = new RDataServerNotAvailableException("Data collection server is not available");
                yield break;
            }

            var request = new RData.Authentication.JsonRpcRequests.JwtAuthorizationRequest(accessToken, gameVersion, selectedGroups);
            yield return CoroutineManager.StartCoroutine(_rDataClient.Send<RData.Authentication.JsonRpcRequests.JwtAuthorizationRequest, BooleanResponse>(request));
            if (request.Response.HasError)
            {
                LastError = new RDataAuthorizationException(request.Response.Error);
                yield break;
            }
            else
            {
                Authorized = request.Response.Result;
            }
        }
    }
}