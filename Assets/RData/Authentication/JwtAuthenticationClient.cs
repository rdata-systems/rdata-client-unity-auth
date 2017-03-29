using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RData.Http;
using RData.Authentication.Exceptions;
using RData.Authentication.HttpRequests;
using JWT;
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
            public long refreshTokenExpiresAt; // In seconds since Unix Epoch, UTC
            public long accessTokenExpiresAt; // In seconds since Unix Epoch, UTC
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
            get { return Tools.Time.UnixTimeSecondsToDateTime(_authenticationInfo.refreshTokenExpiresAt); }
        }

        public DateTime AccessTokenExpiresAt
        {
            get { return Tools.Time.UnixTimeSecondsToDateTime(_authenticationInfo.accessTokenExpiresAt); }
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
            if (Authenticated)
            {
                LastError = new RDataAuthenticationException("Failed to Authenticate - already authenticated. Call Revoke() to Revoke the previous authentication");
                yield break;
            }
            // Send authentication request
            var localAuthRequest = new AuthenticateLocalRequest(username, password);
            yield return StartCoroutine(_httpClient.Send<AuthenticateLocalRequest, AuthenticateLocalRequest.AuthenticateLocalResponse>(localAuthRequest));

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

            var jwtRefreshToken = DecodeJwtToken<JwtRefreshToken>(_authenticationInfo.refreshToken);
            var jwtAccessToken = DecodeJwtToken<JwtAccessToken>(_authenticationInfo.accessToken);

            _authenticationInfo.user = jwtRefreshToken.user;
            _authenticationInfo.refreshTokenExpiresAt = jwtRefreshToken.exp;
            _authenticationInfo.accessTokenExpiresAt = jwtAccessToken.exp;
            
            // Save into player prefs
            SaveToPlayerPrefs();
        }

        public IEnumerator Register(string username, string email, string password)
        {
            // Send registration request
            var localRegistrationRequest = new RegisterLocalRequest(username, email, password);
            yield return StartCoroutine(_httpClient.Send<RegisterLocalRequest, RegisterLocalRequest.RegisterLocalResponse>(localRegistrationRequest));

            if (localRegistrationRequest.HasError)
            {
                LastError = new RDataAuthenticationException(localRegistrationRequest.Error);
                yield break;
            }

            if (!localRegistrationRequest.HasResponse)
            {
                LastError = new RDataAuthenticationException("Failed to register, http request failed but has no error");
                yield break;
            }
        }

        public IEnumerator Revoke()
        {
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

            JwtAccessToken jwtAccessToken;
            try
            {
                jwtAccessToken = DecodeJwtToken<JwtAccessToken>(_authenticationInfo.accessToken);
            }
            catch (RDataAuthenticationException e)
            {
                LastError = e;
                yield break;
            }

            _authenticationInfo.accessTokenExpiresAt = jwtAccessToken.exp;
            _authenticationInfo.user = jwtAccessToken.user; // Role or other user parameters might have updated
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

        private TJwtToken DecodeJwtToken<TJwtToken>(string token)
            where TJwtToken : JwtToken
        {
            string payLoad;
            TJwtToken decodedToken;

            try
            {
                payLoad = JsonWebToken.Decode(token, "", false);
            }
            catch (System.Exception e)
            {
                LastError = new RDataAuthenticationException("Failed to authenticate, refreshToken is invalid", e);
                throw LastError;
            }

            try
            {
                decodedToken = JsonMapper.ToObject<TJwtToken>(payLoad);
            }
            catch (RData.LitJson.JsonException e)
            {
                LastError = new RDataAuthenticationException("Failed to authenticate, refreshToken payload is not a valid json", e);
                throw LastError;
            }

            return decodedToken;
        }
    }
}