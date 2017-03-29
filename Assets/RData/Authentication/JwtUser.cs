using RData.LitJson;

namespace RData.Authentication
{
    public class JwtUser
    {
        public string id;
        public string email;
        public string username;
        public string role;

        [JsonIgnore] public const string kRoleUser = "user";
        [JsonIgnore] public const string kRoleInstructor = "instructor";
    }
}