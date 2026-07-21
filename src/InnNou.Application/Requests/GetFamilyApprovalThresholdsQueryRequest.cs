using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetFamilyApprovalThresholdsQueryRequest : IRequest<ApiResponse<GetFamilyApprovalThresholdsQueryResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? FamilyToken { get; set; }
        public bool IncludeInactive { get; set; } = false;
    }
}
