using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetHotelsQueryResponse
    {
        public List<Hotel> Hotels { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public int? NextPageNumber { get; set; }
        public int? PreviousPageNumber { get; set; }
    }
}
