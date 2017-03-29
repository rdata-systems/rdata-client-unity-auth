using RData.LitJson;

namespace RData.Authentication
{
    public class JwtAccessToken : JwtToken
    {
        public JwtUser user;
    }
}