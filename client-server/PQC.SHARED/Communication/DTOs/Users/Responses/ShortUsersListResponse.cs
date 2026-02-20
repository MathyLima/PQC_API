using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.SHARED.Communication.DTOs.Users.Responses
{
    public class ShortUsersListResponse
    {
        public List<ShortUserResponseJson> Users { get; set; } = new List<ShortUserResponseJson>();
    }
}
