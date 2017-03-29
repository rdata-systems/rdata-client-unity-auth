using System;

namespace RData.Authentication {
    public class JwtToken
    {
        public long iat; // In seconds since Unix Epoch, UTC
        public long exp; // In seconds since Unix Epoch, UTC

        public DateTime issuedAt
        {
            get { return RData.Tools.Time.UnixTimeSecondsToDateTime(iat); }
        }

        public DateTime expiresAt
        {
            get { return RData.Tools.Time.UnixTimeSecondsToDateTime(exp); }
        }
    }
}