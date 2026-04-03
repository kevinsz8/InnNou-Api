using InnNou.Application.Responses.Common;
using System.Collections.Generic;

namespace InnNou.Application.Responses
{
    public class GetUsersQueryResponse
    {
        public List<User> Users { get; set; } = new();
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
