using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RData.Http
{
    public abstract class RDataHttpRequest<TResponse>
        where TResponse : RDataHttpResponse
    {
        public const string kHttpVerbCREATE = UnityWebRequest.kHttpVerbCREATE;
        public const string kHttpVerbDELETE = UnityWebRequest.kHttpVerbDELETE;
        public const string kHttpVerbGET = UnityWebRequest.kHttpVerbGET;
        public const string kHttpVerbHEAD = UnityWebRequest.kHttpVerbHEAD;
        public const string kHttpVerbPOST = UnityWebRequest.kHttpVerbPOST;
        public const string kHttpVerbPUT = UnityWebRequest.kHttpVerbPUT;

        public abstract string Method { get; }
        public abstract string Path { get; }
        public virtual Dictionary<string, string> Parameters { get; set; }
        public virtual Dictionary<string, string> Headers { get; set; }
        public virtual string BodyData { get; set; }

        public long ResponseCode { get; set; }
        public TResponse Response { get; set; }

        public RData.Http.Exceptions.RDataHttpException Error { get; set; }

        public bool HasResponse
        {
            get { return Response != null; }
        }

        public bool HasError
        {
            get { return Error != null; }
        }
    }
}