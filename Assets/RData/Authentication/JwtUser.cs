using RData.LitJson;
using System;
using System.Collections.Generic;

namespace RData.Authentication
{
    public class JwtUser
    {
        public string id;
        public string email;
        public string username;
        public List<UserRole> roles;

        public bool Can(UserRole.Role role, string game=null, string group=null)
        {
            foreach(var r in roles)
            {
                var userRole = r.GetRole(); // Get "Role" enum value
                if ((userRole & role) == role && 
                    (r.group == null || group == r.group) &&
                    (r.game == null || game == r.game)) // Check
                    return true;
            }
            return false;
        }
    }
}