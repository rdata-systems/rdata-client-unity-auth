using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RData;
using RData.Authentication;
using RData.LitJson;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    public RData.Authentication.JwtAuthenticationClient _authenticationClient;

    public Text statusInfo;
    public Text errorInfo;
    public InputField usernameInputField;
    public InputField passwordInputField;
    public Button loginButton;
    public Button revokeButton;
    public Button refreshButton;
    public Text refreshTokenInfo;
    public Text accessTokenInfo;
    public Text userInfo;

    private const string kStatusSendingAuthRequest = "Sending authentication request...";
    private const string kStatusSendingRevokeRequest = "Sending revoke request...";
    private const string kStatusSendingRefreshRequest = "Sending refresh request...";
    private const string kStatusAuthFailed = "Authentication failed";
    private const string kStatusNonAuthenticated = "Not authenticated";
    private const string kStatusRevokeFailed = "Revoke failed";
    private const string kStatusRefreshFailed = "Refresh failed";
    private const string kStatusRefreshTokenExpired = "Refresh token expired";
    private const string kStatusAuthSuccessful = "Authenticated";
    private const string kStatusRevokeSuccessful = "Revoked";
    private const string kStatusRefreshSuccessful = "Refreshed";

    public void OnLoginButtonClicked()
    {
        StartCoroutine(OnLoginButtonClickedCoro());
    }

    private IEnumerator OnLoginButtonClickedCoro()
    {
        SetFormInteractable(false);
        SetStatus(kStatusSendingAuthRequest);
        yield return StartCoroutine(_authenticationClient.Authenticate(usernameInputField.text, passwordInputField.text));
        
        // Check if any errors happened during authentication
        if (_authenticationClient.HasError)
        {
            SetError(_authenticationClient.LastError);
            SetStatus(kStatusAuthFailed);
            SetFormInteractable(true);
            yield break;
        }

        SetStatus(kStatusAuthSuccessful);
        SetFormInteractable(true);

        yield return StartCoroutine(AuthorizeDataCollection());
    }

    public void OnRevokeButtonClicked()
    {
        if (!_authenticationClient.Authenticated)
        {
            SetStatus(kStatusNonAuthenticated);
            return;
        }

        StartCoroutine(OnRevokeButtonClickedCoro());
    }

    private IEnumerator OnRevokeButtonClickedCoro()
    {
        SetFormInteractable(false);
        SetStatus(kStatusSendingRevokeRequest);

        yield return StartCoroutine(_authenticationClient.Revoke());

        // Check if any errors happened during revoke
        if (_authenticationClient.HasError)
        {
            SetError(_authenticationClient.LastError);
            SetStatus(kStatusRevokeFailed);
            SetFormInteractable(true);
            yield break;
        }

        SetStatus(kStatusRevokeSuccessful);
        SetFormInteractable(true);
    }

    public void OnRefreshButtonClicked()
    {
        if (!_authenticationClient.Authenticated)
        {
            SetStatus(kStatusNonAuthenticated);
            return;
        }

        if (_authenticationClient.RefreshTokenExpired)
        {
            SetStatus(kStatusRefreshTokenExpired);
            return;
        }

        StartCoroutine(OnRefreshButtonClickedCoro());
    }

    private IEnumerator OnRefreshButtonClickedCoro()
    {
        SetFormInteractable(false);
        SetStatus(kStatusSendingRefreshRequest);
        yield return StartCoroutine(_authenticationClient.Refresh());

        // Check if any errors happened during refresh
        if (_authenticationClient.HasError)
        {
            SetError(_authenticationClient.LastError);
            SetStatus(kStatusRefreshFailed);
            SetFormInteractable(true);
            yield break;
        }

        SetStatus(kStatusRefreshSuccessful);
        SetFormInteractable(true);
    }

    private void Update()
    {
        UpdateAuthenticationInfo();
    }

    private IEnumerator Start()
    {
        // If already authenticated...
        if (_authenticationClient.Authenticated)
        {
            // If refresh token is expired, revoke the authentication
            if (_authenticationClient.RefreshTokenExpired)
            {
                yield return StartCoroutine(_authenticationClient.Revoke());

                // Check if revoking caused any errors
                if (_authenticationClient.HasError)
                    SetError(_authenticationClient.LastError);
                
                yield break;
            }

            // If access token is expired, refresh the access token
            if (_authenticationClient.AccessTokenExpired)
            {
                yield return StartCoroutine(_authenticationClient.Refresh());

                // Check if refreshing caused any errors
                if (_authenticationClient.HasError)
                {
                    SetError(_authenticationClient.LastError);
                    yield break;
                }
            }

            // We should be good to go, authorize data collection
            yield return StartCoroutine(AuthorizeDataCollection());
        }
    }

    private IEnumerator AuthorizeDataCollection()
    {
        Debug.Log("Authorizing data collection.");

        // Wait for data collection server to become available
        while (!RDataSingleton.Client.IsAvailable)
            yield return null;

        Debug.Log("Data collection available!");

        RDataSingleton.Client.AuthorizationStrategy = new JwtAuthorizationStrategy(RDataSingleton.Client, _authenticationClient);
        yield return StartCoroutine(RDataSingleton.Client.Authorize());

        Debug.Log("Data collection authorized!");
    }

    private void SetStatus(string status)
    {
        statusInfo.text = status;
    }

    private void SetError(RData.Authentication.Exceptions.RDataAuthenticationException exception)
    {
        if(exception.InnerException is RData.Http.Exceptions.RDataHttpException && ((RData.Http.Exceptions.RDataHttpException)exception.InnerException).HasApiError)
        {
            var err = ((RData.Http.Exceptions.RDataHttpException)exception.InnerException).ApiError;
            errorInfo.text = string.Format("Exception: {0}, HTTP Error.name: {1}, HTTP Error.message: {2}", exception, err.name, err.message);
        }
        else
        {
            errorInfo.text = string.Format("Exception: {0}", exception);
        }

        Debug.LogException(exception);
    }

    private void SetFormInteractable(bool isInteractable)
    {
        usernameInputField.interactable = isInteractable;
        passwordInputField.interactable = isInteractable;
        loginButton.interactable = isInteractable;
        revokeButton.interactable = isInteractable;
        refreshButton.interactable = isInteractable;
    }

    private void UpdateAuthenticationInfo()
    {
        if (_authenticationClient.Authenticated && _authenticationClient.AccessToken != null && _authenticationClient.RefreshToken != null)
        {
            if (_authenticationClient.AccessTokenExpired)
            {
                accessTokenInfo.text = "Access token expired";
            } else
            {
                accessTokenInfo.text = string.Format("expires at: {0}\nexpires in {1}", _authenticationClient.AccessTokenExpiresAt, _authenticationClient.AccessTokenExpiresAt - DateTime.UtcNow);
            }

            if (_authenticationClient.RefreshTokenExpired)
            {
                refreshTokenInfo.text = "Refresh token expired";
            }
            else
            {
                refreshTokenInfo.text = string.Format("expires at: {0}\nexpires in {1}", _authenticationClient.RefreshTokenExpiresAt, _authenticationClient.RefreshTokenExpiresAt - DateTime.UtcNow);
            }

            var user = _authenticationClient.User;
            userInfo.text = string.Format(" user.id: {0} \n user.username: {1} \n user.email: {2} \n user.role: {3} ", user.id, user.username, user.email, user.role);
        }
        else
        {
            refreshTokenInfo.text = "No refresh token";
            accessTokenInfo.text = "No access token";
            userInfo.text = "No user";
        }
    }
}
