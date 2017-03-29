namespace RData.Authentication
{
    public class JwtRefreshToken : JwtToken
    {
        public JwtUser user;
        public JwtSession session;
    }
}
