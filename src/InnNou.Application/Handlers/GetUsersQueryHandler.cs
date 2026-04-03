using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Domain.Persistence;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InnNou.Application.Handlers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQueryRequest, ApiResponse<GetUsersQueryResponse>>
    {
        private readonly IUserService _userService;
        private readonly AutoMapper.IMapper _mapper;
        public GetUsersQueryHandler(IUserService userService, AutoMapper.IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }
        public async Task<ApiResponse<GetUsersQueryResponse>> Handle(GetUsersQueryRequest request, CancellationToken cancellationToken)
        {
            var resultUsers = await _userService.GetUsersAsync(request.PageNumber, request.PageSize, request.SearchField, request.SearchText, cancellationToken);
            var users = _mapper.Map<List<User>>(resultUsers.Items);
            var totalPages = resultUsers.TotalPages;
            var response = new GetUsersQueryResponse
            {
                Users = users,
                TotalCount = resultUsers.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetUsersQueryResponse>.SuccessResponse(response);
        }
    }
}
