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
        public string m_hostName = "http://localhost:80/";

        private string HostName
        {
            get
            {
                return RemoveTrailingSlash(m_hostName);
            }
        }

        public IEnumerator Send<TRequest, TResponse>(TRequest request)
            where TRequest : RDataHttpRequest<TResponse>
        {
            UnityWebRequest unityWebRequest;

            if (request.Method == UnityWebRequest.kHttpVerbGET)
            {
                unityWebRequest = UnityWebRequest.Get(HostName + request.Path);
            }
            else if (request.Method == UnityWebRequest.kHttpVerbPOST)
            {
                unityWebRequest = UnityWebRequest.Post(HostName + request.Path, request.Parameters);
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

            request.ResponseCode = unityWebRequest.responseCode;

            try
            {
                request.Response = RData.LitJson.JsonMapper.ToObject<TResponse>(unityWebRequest.downloadHandler.text);
            }
            catch (RData.LitJson.JsonException e)
            {
                request.Error = new RDataHttpException(string.Format("Http request {0} failed to parse json response: {1}", request.Path, unityWebRequest.downloadHandler.text), e);
                yield break;
            }

            // Check if status code indicates about error
            if(unityWebRequest.responseCode == 400)
            {
                request.Error = new BadRequestException(string.Format("Http request {0} failed with 400 Bad Request", request.Path));
                yield break;
            }

            if (unityWebRequest.responseCode == 401)
            {
                request.Error = new UnauthorizedException(string.Format("Http request {0} failed with 401 Unauthorized", request.Path));
                yield break;
            }

            if (unityWebRequest.responseCode == 403)
            {
                request.Error = new ForbiddenException(string.Format("Http request {0} failed with 403 Forbidden", request.Path));
                yield break;
            }

            if (unityWebRequest.responseCode == 404)
            {
                request.Error = new NotFoundException(string.Format("Http request {0} failed with 404 Not Found", request.Path));
                yield break;
            }

            if (unityWebRequest.responseCode != 200)
            {
                request.Error = new RDataHttpException(string.Format("Http request {0} failed with unknown status code: {1}", request.Path, unityWebRequest.responseCode));
                yield break;
            }

            Debug.Log(string.Format("RDataHttpClient Recv, Method: {0}, Path: {1}, Response: {2}", request.Method, request.Path, unityWebRequest.downloadHandler.text));

            // Everything is fine at this point
        }

        private string RemoveTrailingSlash(string str)
        {
            int lastSlash = str.LastIndexOf('/');
            str = (lastSlash > -1) ? str.Substring(0, lastSlash) : str;
            return str;
        }
    }
}