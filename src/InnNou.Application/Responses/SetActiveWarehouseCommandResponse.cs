namespace InnNou.Application.Responses
{
    public class SetActiveWarehouseCommandResponse
    {
        public Guid WarehouseToken { get; set; }
        public bool IsActive { get; set; }
    }
}
