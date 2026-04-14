using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InnNou.Application.Requests
{
    public class StopImpersonateCommandRequest : IRequest<ApiResponse<StopImpersonateCommandResponse>>
    {
    }
}
