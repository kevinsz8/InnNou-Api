using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IHotelContactService
    {
        Task<PagedResult<HotelContactDto>> GetPagedByHotelTokenAsync(Guid hotelToken, int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<HotelContactDto?> GetByTokenAsync(Guid hotelContactToken, IRequestContext context, CancellationToken cancellationToken);
        Task<HotelContactDto?> CreateAsync(HotelContactDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<HotelContactDto?> EditAsync(HotelContactDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid hotelContactToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
