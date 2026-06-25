namespace InnNou.Shared.Mapping
{
    public interface IMapper
    {
        TDestination Map<TDestination>(object source);
        List<TDestination> MapList<TDestination>(System.Collections.IEnumerable source);
    }
}
