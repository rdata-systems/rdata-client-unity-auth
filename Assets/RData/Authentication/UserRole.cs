using RData.LitJson;
using System;

namespace RData.Authentication
{
    public class UserRole
    {
        public string role;
        public string group;
        public string game;
        
        [Flags]
        public enum Role
        {
            Read = 1,
            Write = 2,
            ReadWrite = 1 + 2,
            ReadData = 4,
            WriteData = 8,
            ReadWriteData = 4 + 8
        }

        public Role GetRole()
        {
            var rCamelCased = Char.ToUpperInvariant(role[0]) + role.Substring(1); // readWriteData -> ReadWriteData
            return (Role)Enum.Parse(typeof(UserRole.Role), rCamelCased); // Parse to the "Role" enum
        }
    }
}