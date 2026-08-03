using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetSubFamilyNameTranslationsCommandRequest : IRequest<ApiResponse<SetSubFamilyNameTranslationsCommandResponse>>
    {
        public Guid SubFamilyToken { get; set; }
        public Dictionary<string, string> NameTranslations { get; set; } = [];
    }
}
