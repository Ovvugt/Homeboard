using System.Data;
using Dapper;

namespace Homeboard.Core.Data;

public static class DapperConfig
{
    private static int _initialized;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1) return;
        SqlMapper.AddTypeHandler(new GuidStringTypeHandler());
        SqlMapper.AddTypeHandler(new NullableGuidStringTypeHandler());
    }

    private sealed class GuidStringTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);
        public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
    }

    private sealed class NullableGuidStringTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object value) => value is null or DBNull ? null : Guid.Parse((string)value);
        public override void SetValue(IDbDataParameter parameter, Guid? value) =>
            parameter.Value = value?.ToString() ?? (object)DBNull.Value;
    }
}
