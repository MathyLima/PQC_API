using System.Reflection;
using System.Runtime.Serialization;

namespace PQC.SHARED.Communication.DTOs.Enums
{

    public static class EnumExtensions
    {
        public static string ToEnumString(this Enum value)
        {
            var type = value.GetType();
            var member = type.GetMember(value.ToString());

            if (member.Length > 0)
            {
                var attr = member[0]
                    .GetCustomAttribute<EnumMemberAttribute>();

                if (attr != null)
                    return attr.Value!;
            }

            return value.ToString();
        }
    }

}
