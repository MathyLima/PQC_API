using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.COMMUNICATION.Responses
{
    public class LoginResponseJson
    {
        public string Token { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
