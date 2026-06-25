namespace InnNou.Shared.Mapping
{
    public sealed class Mapper : IMapper
    {
        private readonly Dictionary<(Type, Type), Func<object, object>> _maps = new();

        public void Register<TSource, TDest>(Func<TSource, TDest> map) where TDest : notnull
            => _maps[(typeof(TSource), typeof(TDest))] = src => map((TSource)src)!;

        public TDest Map<TDest>(object source)
        {
            var sourceType = source.GetType();
            var destType = typeof(TDest);

            if (_maps.TryGetValue((sourceType, destType), out var fn))
                return (TDest)fn(source);

            var baseType = sourceType.BaseType;
            while (baseType is not null && baseType != typeof(object))
            {
                if (_maps.TryGetValue((baseType, destType), out fn))
                    return (TDest)fn(source);
                baseType = baseType.BaseType;
            }

            throw new InvalidOperationException(
                $"No mapping registered: {sourceType.Name} → {destType.Name}");
        }

        public List<TDest> MapList<TDest>(System.Collections.IEnumerable source)
            => source.Cast<object>().Select(Map<TDest>).ToList();
    }
}
