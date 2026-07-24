namespace InnNou.Domain.Dtos
{
    public class ParLevelConfigurationDto
    {
        public ParLevelDto? Base { get; set; }
        public List<ParLevelOverrideDto> Overrides { get; set; } = [];
        public ParLevelEffectiveDto? EffectiveToday { get; set; }
    }
}
