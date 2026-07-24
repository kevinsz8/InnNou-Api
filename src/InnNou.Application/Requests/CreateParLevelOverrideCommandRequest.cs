using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateParLevelOverrideCommandRequest : IRequest<ApiResponse<CreateParLevelOverrideCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public Guid ArticleToken { get; set; }

        // "SEASONAL" / "EVENT".
        public string Type { get; set; } = string.Empty;
        public string? Label { get; set; }

        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }

        // SEASONAL only.
        public int? StartMonth { get; set; }
        public int? StartDay { get; set; }
        public int? EndMonth { get; set; }
        public int? EndDay { get; set; }

        // EVENT only.
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
