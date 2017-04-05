using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RData.Http;
using RData.Authentication.Exceptions;
using RData.Authentication.HttpRequests;
using RData.LitJson;

namespace RData.Authentication
{
    [RequireComponent(typeof(RDataHttpClient))]
    public class JwtAuthenticationClient : MonoBehaviour
    {
        /// <summary>
        /// If set to true, authentication information will be saved into playerPrefs 
        /// and can be re-used after client restarts
        /// </summary>
        public bool SerializeIntoPlayerPrefs = true;

        private const string kPlayerPrefsKey = "RData.Authentication";

        private RDataHttpClient _httpClient;

        protected class AuthenticationInfo
        {
            public int version = 1;
            public JwtUser user;
            public string refreshToken;
            public string accessToken;
            public long refreshTokenExpiresAt; // In milliseconds since Unix Epoch, UTC
            public long accessTokenExpiresAt; // In milliseconds since Unix Epoch, UTC
        }

        protected AuthenticationInfo _authenticationInfo;

        public RDataAuthenticationException LastError { get; set; }

        public bool HasError
        {
            get { return LastError != null; }
        }

        public string UserId
        {
            get { return _authenticationInfo.user.id; }
        }

        public JwtUser User
        {
            get { return _authenticationInfo.user; }
        }

        public string RefreshToken
        {
            get { return _authenticationInfo.refreshToken; }
        }

        public string AccessToken
        {
            get { return _authenticationInfo.accessToken; }
        }

        public DateTime RefreshTokenExpiresAt
        {
            get { return Tools.Time.UnixTimeMillisecondsToDateTime(_authenticationInfo.refreshTokenExpiresAt); }
        }

        public DateTime AccessTokenExpiresAt
        {
            get { return Tools.Time.UnixTimeMillisecondsToDateTime(_authenticationInfo.accessTokenExpiresAt); }
        }
        
        public bool RefreshTokenExpired
        {
            get { return DateTime.UtcNow > RefreshTokenExpiresAt; }
        }

        public bool AccessTokenExpired
        {
            get { return DateTime.UtcNow > AccessTokenExpiresAt; }
        }

        public bool Authenticated
        {
            get
            {
                return _authenticationInfo != null && 
                    _authenticationInfo.user != null && 
                    !string.IsNullOrEmpty(_authenticationInfo.user.id) && 
                    !string.IsNullOrEmpty(_authenticationInfo.refreshToken);
            }
        }

        protected void Awake()
        {
            _httpClient = GetComponent<RDataHttpClient>();
            LoadFromPlayerPrefs();
        }

        public IEnumerator Authenticate(string username, string password)
        {
            LastError = null;

            if (Authenticated)
            {
                LastError = new RDataAuthenticationException("Failed to Authenticate - already authenticated. Call Revoke() to Revoke the previous authentication");
                yield break;
            }
            // Send authentication request
            var localAuthRequest = new AuthenticateLocalRequest(username, password);
            yield return StartCoroutine(_httpClient.Send<AuthenticateLocalRequest, AuthenticateLocalRequest.AuthenticateLocalResponse>(localAuthRequest));

            if(localAuthRequest.HasError && localAuthRequest.Error is RData.Http.Exceptions.UnauthorizedException)
            {
                LastError = new InvalidCredentialsException(localAuthRequest.Error);
                yield break;
            }

            if (localAuthRequest.HasError)
            {
                LastError = new RDataAuthenticationException(localAuthRequest.Error);
                yield break;
            }

            if(!localAuthRequest.HasResponse)
            {
                LastError = new RDataAuthenticationException("Failed to authenticate, http request failed but has no error");
                yield break;
            }

            // Process the authentication response - build new AuthenticationInfo object
            _authenticationInfo = new AuthenticationInfo();

            _authenticationInfo.accessToken = localAuthRequest.Response.accessToken;
            _authenticationInfo.refreshToken = localAuthRequest.Response.refreshToken;
            _authenticationInfo.user = localAuthRequest.Response.user;
            _authenticationInfo.refreshTokenExpiresAt = localAuthRequest.Response.refreshTokenExpiresAt;
            _authenticationInfo.accessTokenExpiresAt = localAuthRequest.Response.accessTokenExpiresAt;
            
            // Save into player prefs
            SaveToPlayerPrefs();
        }

        public IEnumerator Register(string username, string email, string password)
        {
            LastError = null;

            // Send registration request
            var localRegistrationRequest = new RegisterLocalRequest(username, email, password);
            yield return StartCoroutine(_httpClient.Send<RegisterLocalRequest, RegisterLocalRequest.RegisterLocalResponse>(localRegistrationRequest));

            if (localRegistrationRequest.HasError)
            {
                if (localRegistrationRequest.Error is RData.Http.Exceptions.BadRequestException && 
                    localRegistrationRequest.Error.HasApiError && 
                    localRegistrationRequest.Error.ApiError.Name == "UsernameTakenError")
                {
                    LastError = new UsernameTakenException(localRegistrationRequest.Error);
                    yield break;
                }
                else if (localRegistrationRequest.Error is RData.Http.Exceptions.BadRequestException && 
                    localRegistrationRequest.Error.HasApiError &&
                    localRegistrationRequest.Error.ApiError.Name == "EmailTakenError")
                {
                    LastError = new EmailTakenException(localRegistrationRequest.Error);
                    yield break;
                }
                else
                {
                    // Unknown error
                    LastError = new RDataAuthenticationException(localRegistrationRequest.Error);
                    yield break;
                }
            }

            if (!localRegistrationRequest.HasResponse)
            {
                LastError = new RDataAuthenticationException("Failed to register, http request failed but has no error");
                yield break;
            }
        }

        public IEnumerator Revoke()
        {
            LastError = null;

            if (!Authenticated)
            {
                LastError = new RDataAuthenticationException("You need to be authenticated to Revoke the authentication");
                yield break;
            }
            // If refresh token is not expired, revoke it by sending the revoke request
            if (!RefreshTokenExpired)
            {
                var revokeRequest = new RevokeRequest(RefreshToken);
                yield return StartCoroutine(_httpClient.Send<RevokeRequest, RevokeRequest.RevokeResponse>(revokeRequest));

                // If we have an error and this error is not an UnauthorizedException or ForbiddenException, we want to throw it
                // Otherwise, consume the error and revoke the token on the client (the token is already expired/revoked on the server)
                if (revokeRequest.HasError && 
                    !(revokeRequest.Error is Http.Exceptions.UnauthorizedException) && 
                    !(revokeRequest.Error is Http.Exceptions.ForbiddenException))
                {
                    LastError = new RDataAuthenticationException(revokeRequest.Error);
                    yield break;
                }

                if (!revokeRequest.HasResponse)
                {
                    LastError = new RDataAuthenticationException("Failed to Revoke(), http request failed but has no error");
                    yield break;
                }
            }

            ResetValues();
            SaveToPlayerPrefs();
        }

        public IEnumerator Refresh()
        {
            LastError = null;

            if (!Authenticated)
            {
                LastError = new RDataAuthenticationException("You need to be authenticated to Revoke the authentication");
                yield break;
            }
            if (RefreshTokenExpired)
            {
                LastError = new RDataAuthenticationException("Failed to refresh the access token: refresh token expired.");
                yield break;
            }

            var refreshRequest = new RefreshRequest(RefreshToken);
            yield return StartCoroutine(_httpClient.Send<RefreshRequest, RefreshRequest.RefreshResponse>(refreshRequest));

            if (refreshRequest.HasError)
            {
                LastError = new RDataAuthenticationException(refreshRequest.Error);
                yield break;

            }

            if (!refreshRequest.HasResponse)
            {
                LastError = new RDataAuthenticationException("Failed to Refresh(), http request failed but has no error");
                yield break;
            }
            
            _authenticationInfo.accessToken = refreshRequest.Response.accessToken;
            _authenticationInfo.accessTokenExpiresAt = refreshRequest.Response.accessTokenExpiresAt;
            _authenticationInfo.user = refreshRequest.Response.user; // Role or other user parameters might have updated
            SaveToPlayerPrefs();
        }
        
        private void ResetValues()
        {
            _authenticationInfo = new AuthenticationInfo();
            SaveToPlayerPrefs();
        }

        private void LoadFromPlayerPrefs()
        {
            string authInfoJson = PlayerPrefs.GetString(kPlayerPrefsKey);
            if (string.IsNullOrEmpty(authInfoJson))
            {
                ResetValues(); // No auth info previosuly saved. Create a new one.
                return;
            }

            try
            {
                _authenticationInfo = RData.LitJson.JsonMapper.ToObject<AuthenticationInfo>(authInfoJson);

            } catch(RData.LitJson.JsonException e)
            {
                Debug.LogError(string.Format("Failed to load authentication info from the player prefs. Creating new authentication info. Json Deserialization failed: {0}", e));
                ResetValues();
            }
        }

        private void SaveToPlayerPrefs()
        {
            string authInfoJson = RData.LitJson.JsonMapper.ToJson(_authenticationInfo);
            PlayerPrefs.SetString(kPlayerPrefsKey, authInfoJson);
        }
    }
}