using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IHotelService
    {
        Task<PagedResult<HotelDto>> GetHotelsAsync(int pageNumber, int pageSize, string? searchField, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<HotelDto?> GetHotelByTokenAsync(Guid hotelToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> HotelExistsByNameAsync(string name, CancellationToken cancellationToken);
        Task<HotelDto?> CreateHotelAsync(HotelDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<HotelDto?> EditHotelAsync(HotelDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteHotelAsync(Guid hotelToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
