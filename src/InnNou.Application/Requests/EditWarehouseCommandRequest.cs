using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditWarehouseCommandRequest : IRequest<ApiResponse<EditWarehouseCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public string Name { get; set; } = default!;
        public string? Code { get; set; }
        public string? Description { get; set; }

        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public Guid? ZoneToken { get; set; }

        public bool IsInventoriable { get; set; }
        public bool CanReceivePurchases { get; set; }
        public bool CanReceiveTransfers { get; set; }
        public bool CanTransferOut { get; set; }
        public bool CanConsumeInventory { get; set; }
        public bool CanProduceItems { get; set; }
        public bool CanSellItems { get; set; }
        public bool CanAdjustInventory { get; set; }
        public bool CanReceiveReturns { get; set; }
        public bool TrackLotNumbers { get; set; }
        public bool TrackExpirationDates { get; set; }
        public bool TrackSerialNumbers { get; set; }
        public bool RequireApproval { get; set; }
        public bool IsDefaultReceivingWarehouse { get; set; }
        public bool IsDefaultConsumptionWarehouse { get; set; }
        public bool IsMainWarehouse { get; set; }
    }
}
