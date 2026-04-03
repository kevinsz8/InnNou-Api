using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;
using System;

namespace InnNou.Application.Requests
{
    public class DeleteUserCommandRequest : IRequest<ApiResponse<DeleteUserCommandResponse>>
    {
        public int UserId { get; set; }
    }
}
