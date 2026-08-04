using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SupplierPriceSubscriptionService(
    IDbConnectionFactory connectionFactory,
    ISupplierService supplierService,
    IMapper mapper) : ISupplierPriceSubscriptionService
{
    // Defensive upper bound on a single "set my subscriptions" call — this is a one-off personal
    // settings save (a multi-select on /preferences), not a hot path, so the per-token
    // GetSupplierByTokenAsync visibility check below is an intentional, bounded exception to the
    // usual "batch, never loop one query per item" rule (CLAUDE.md) rather than an oversight:
    // reimplementing GetSupplierByTokenAsync's global/private hierarchy branches as a second,
    // batched SP would duplicate authorization logic that must stay in exact sync with the
    // original. This cap keeps the worst case bounded regardless.
    private const int MaxSubscriptionSuppliers = 200;

    private async Task<int> ResolveUserIdAsync(IDbConnection connection, IRequestContext context)
    {
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);

        if (user is null)
            throw new ApiException(ErrorCodes.UserNotFound, "Current user could not be resolved.", 404);

        return user.UserId;
    }

    public async Task<List<SupplierPriceChangeSubscriptionDto>> SetSubscriptionsAsync(List<Guid> supplierTokens, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var distinctTokens = supplierTokens.Distinct().ToList();
        if (distinctTokens.Count > MaxSubscriptionSuppliers)
            throw new ApiException(ErrorCodes.SupplierPriceSubscriptionTooManySuppliers, $"Cannot subscribe to more than {MaxSubscriptionSuppliers} suppliers at once.", 400);

        await using var connection = connectionFactory.CreateConnection();
        var userId = await ResolveUserIdAsync(connection, context);

        // Only tokens the caller's own organization can actually see survive — a stale or
        // out-of-scope token is silently dropped rather than erroring, same "don't reveal what
        // you can't see" posture as GetSupplierByTokenAsync itself returning null instead of 403.
        var visibleTokens = new List<Guid>();
        foreach (var token in distinctTokens)
        {
            var supplier = await supplierService.GetSupplierByTokenAsync(token, context, cancellationToken);
            if (supplier is not null)
                visibleTokens.Add(token);
        }

        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@SupplierTokens", visibleTokens.Count > 0 ? string.Join(',', visibleTokens) : null);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        await connection.ExecuteAsync("sp_SupplierPriceChangeSubscription_Set", p, commandType: CommandType.StoredProcedure);

        var rows = (await connection.QueryAsync<SupplierPriceChangeSubscription>(
            "sp_SupplierPriceChangeSubscription_GetForUser", new { UserId = userId }, commandType: CommandType.StoredProcedure)).ToList();

        return mapper.MapList<SupplierPriceChangeSubscriptionDto>(rows);
    }

    public async Task<List<SupplierPriceChangeSubscriptionDto>> GetMySubscriptionsAsync(IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var userId = await ResolveUserIdAsync(connection, context);

        var rows = (await connection.QueryAsync<SupplierPriceChangeSubscription>(
            "sp_SupplierPriceChangeSubscription_GetForUser", new { UserId = userId }, commandType: CommandType.StoredProcedure)).ToList();

        return mapper.MapList<SupplierPriceChangeSubscriptionDto>(rows);
    }
}
