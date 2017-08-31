using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using RData.Http.Exceptions;
using System.Linq;

namespace RData.Http
{
    public class RDataHttpClient : MonoBehaviour
    {
        public enum Stage { development, testing, production };
        
        public Stage m_stage = Stage.development;
        public string m_hostNameDevelopment = "http://localhost:9000/api/v1";
        public string m_hostNameTesting = "";
        public string m_hostNameProduction = "";

        private string HostName
        {
            get
            {
                switch (m_stage)
                {
                    default:
                    case Stage.development: return RemoveTrailingSlash(m_hostNameDevelopment);
                    case Stage.testing: return RemoveTrailingSlash(m_hostNameTesting);
                    case Stage.production: return RemoveTrailingSlash(m_hostNameProduction);
                }
            }
        }

        public IEnumerator Send<TRequest, TResponse>(TRequest request)
            where TRequest : RDataHttpRequest<TResponse>
            where TResponse : RDataHttpResponse
        {
            UnityWebRequest unityWebRequest;

            if (request.Method == UnityWebRequest.kHttpVerbGET)
            {
                unityWebRequest = UnityWebRequest.Get(HostName + request.Path);
            }
            else if (request.Method == UnityWebRequest.kHttpVerbPOST)
            {
                //original implementation 
                //unityWebRequest = UnityWebRequest.Post(HostName + request.Path, request.Parameters);

                //We must create a request manually to prevent the default form serialization that won't take strings longer than 36k chars
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(SerializeSimpleForm(request.Parameters));
                unityWebRequest = new UnityWebRequest(HostName + request.Path, UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw)
                    {
                        contentType = "application/x-www-form-urlencoded"
                    },
                    downloadHandler = (DownloadHandler)new DownloadHandlerBuffer()
                };
            }
            else if (request.Method == UnityWebRequest.kHttpVerbPUT)
            {
                unityWebRequest = UnityWebRequest.Put(HostName + request.Path, request.BodyData);
            }
            else if (request.Method == UnityWebRequest.kHttpVerbDELETE)
            {
                unityWebRequest = UnityWebRequest.Delete(HostName + request.Path);
            }
            else
            {
                request.Error = new RDataHttpException(string.Format("HTTP Method {0} is currently not supported by RDataHttpClient", request.Method));
                yield break;
            }

            if (request.Headers != null)
            {
                foreach (var headerKvp in request.Headers)
                {
                    unityWebRequest.SetRequestHeader(headerKvp.Key, headerKvp.Value);
                }
            }

            string parametersDebug = request.Parameters == null ? "none" : string.Join("&", request.Parameters.Select(x => x.Key + "=" + x.Value).ToArray());
            string headersDebug = request.Headers == null ? "none" : string.Join("; ", request.Headers.Select(x => x.Key + "=" + x.Value).ToArray());
            Debug.Log(string.Format("RDataHttpClient Send, Method: {0}, Path: {1}, Parameters: [{2}], Headers: [{3}], BodyData: {4}", request.Method, request.Path, parametersDebug, headersDebug, request.BodyData));

            yield return unityWebRequest.Send();

            if (unityWebRequest.isError)
            {
                request.Error = new RDataHttpException(string.Format("Http request {0} failed with error: {1}", request.Path, unityWebRequest.error));
                yield break;
            }

			string downloadHandlerResponseText = "";

			if (unityWebRequest.downloadHandler != null)
			{
				downloadHandlerResponseText = unityWebRequest.downloadHandler.text;
            }
           
            Debug.Log(string.Format("RDataHttpClient Recv, Method: {0}, Path: {1}, Response: {2}", request.Method, request.Path, downloadHandlerResponseText));

            request.ResponseCode = unityWebRequest.responseCode;

            try
            {
                request.Response = RData.LitJson.JsonMapper.ToObject<TResponse>(downloadHandlerResponseText);
            }
            catch (RData.LitJson.JsonException e)
            {
                request.Error = new RDataHttpException(string.Format("Http request {0} failed to parse json response: {1}", request.Path, downloadHandlerResponseText), e);
                yield break;
            }

            // Check if status code indicates about error
            if(unityWebRequest.responseCode == 400)
            {
                request.Error = new BadRequestException(string.Format("Http request {0} failed with 400 Bad Request", request.Path), request.Response.Error);
                yield break;
            }

            if (unityWebRequest.responseCode == 401)
            {
                request.Error = new UnauthorizedException(string.Format("Http request {0} failed with 401 Unauthorized", request.Path), request.Response.Error);
                yield break;
            }

            if (unityWebRequest.responseCode == 403)
            {
                request.Error = new ForbiddenException(string.Format("Http request {0} failed with 403 Forbidden", request.Path), request.Response.Error);
                yield break;
            }

            if (unityWebRequest.responseCode == 404)
            {
                request.Error = new NotFoundException(string.Format("Http request {0} failed with 404 Not Found", request.Path), request.Response.Error);
                yield break;
            }

            if (unityWebRequest.responseCode != 200)
            {
                request.Error = new RDataHttpException(string.Format("Http request {0} failed with unknown status code: {1}", request.Path, unityWebRequest.responseCode), request.Response.Error);
                yield break;
            }

            // Everything is fine at this point
        }

        private string RemoveTrailingSlash(string str)
        {
            int lastSlash = str.LastIndexOf('/');
            str = (lastSlash == str.Length - 1) ? str.Substring(0, lastSlash) : str;
            return str;
        }

        /// <summary>
        /// Serializes the simple form.
        /// Mono mishandles this method traced through UnityWebRequest.Post > SerializeSimpleForm method, using 
        /// Uri.EscapeDataString which checks length (for GET requests) but should not check for post form data.
        ///
        /// </summary>
        /// <returns>The simple form.</returns>
        /// <param name="formFields">Form fields.</param>
        private string SerializeSimpleForm(Dictionary<string, string> formFields)
        {
            string text = "";
            foreach (KeyValuePair<string, string> current in formFields)
            {
                if (text.Length > 0)
                {
                    text += "&";
                }
                text = text + (current.Key) + "=" + (current.Value);
            }
            return text;
        }   
    }
}