namespace InnNou.Application.Responses.Common
{
    public class ParLevelConfiguration
    {
        public ParLevel? Base { get; set; }
        public List<ParLevelOverride> Overrides { get; set; } = [];
        public ParLevelEffective? EffectiveToday { get; set; }
    }
}
