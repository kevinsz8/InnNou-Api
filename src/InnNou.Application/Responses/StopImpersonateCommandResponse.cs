using System;
using System.Collections.Generic;
using System.Text;

namespace InnNou.Application.Responses
{
    public class StopImpersonateCommandResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UserId { get; set; }
        public Guid UserToken { get; set; }
    }
}
