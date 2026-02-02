using Core.Enum;
using Core.Interface.Service.Entity;

namespace Core.Extensions;

/// <summary>
/// Extension methods for cleaner lookup resolution syntax.
/// Provides fluent async API for resolving lookup value codes to IDs.
/// </summary>
public static class LookupResolverExtensions
{
    /// <summary>
    /// Gets the lookup value_id for an enum value_code asynchronously.
    /// </summary>
    /// <param name="valueCodeEnum">The enum representing the value code</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the lookup value is not found</exception>
    /// <example>
    /// var statusId = await OrderStatusCode.PENDING.IdAsync(_lookupResolver, typeId: 10);
    /// </example>
    public static async Task<uint> IdAsync(
        this System.Enum valueCodeEnum,
     ILookupResolver resolver,
        ushort typeId,
        CancellationToken ct = default)
    {
        if (resolver == null)
        {
       throw new ArgumentNullException(nameof(resolver));
        }

        return await resolver.GetIdAsync(typeId, valueCodeEnum, ct);
    }

    /// <summary>
    /// Attempts to get the lookup value_id for an enum value_code asynchronously.
    /// </summary>
    /// <param name="valueCodeEnum">The enum representing the value code</param>
 /// <param name="resolver">The lookup resolver instance</param>
  /// <param name="typeId">The lookup type ID</param>
 /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id if found; otherwise null</returns>
    /// <example>
    /// var statusId = await OrderStatusCode.PENDING.TryIdAsync(_lookupResolver, typeId: 10);
    /// if (statusId == null) { /* handle missing value */ }
    /// </example>
    public static async Task<uint?> TryIdAsync(
        this System.Enum valueCodeEnum,
        ILookupResolver resolver,
 ushort typeId,
        CancellationToken ct = default)
    {
        if (resolver == null)
        {
       throw new ArgumentNullException(nameof(resolver));
     }

    return await resolver.TryGetIdAsync(typeId, valueCodeEnum, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for a string value_code asynchronously.
    /// </summary>
    /// <param name="valueCode">The value code (case-insensitive)</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the lookup value is not found</exception>
    /// <example>
    /// var statusId = await "PENDING".IdAsync(_lookupResolver, typeId: 10);
    /// </example>
    public static async Task<uint> IdAsync(
        this string valueCode,
        ILookupResolver resolver,
        ushort typeId,
      CancellationToken ct = default)
    {
      if (resolver == null)
 {
   throw new ArgumentNullException(nameof(resolver));
 }

        return await resolver.GetIdAsync(typeId, valueCode, ct);
    }

    /// <summary>
    /// Attempts to get the lookup value_id for a string value_code asynchronously.
    /// </summary>
    /// <param name="valueCode">The value code (case-insensitive)</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id if found; otherwise null</returns>
    /// <example>
    /// var statusId = await "PENDING".TryIdAsync(_lookupResolver, typeId: 10);
    /// </example>
    public static async Task<uint?> TryIdAsync(
        this string valueCode,
        ILookupResolver resolver,
        ushort typeId,
        CancellationToken ct = default)
    {
        if (resolver == null)
        {
       throw new ArgumentNullException(nameof(resolver));
        }

    return await resolver.TryGetIdAsync(typeId, valueCode, ct);
    }

    #region Type-Specific Helper Extensions

    /// <summary>
    /// Gets the lookup value_id for an AccountStatusCode enum asynchronously.
    /// Type-specific helper for Account Status lookups (type_id = 1).
    /// </summary>
    /// <param name="statusCode">The account status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the account status</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the status is not found</exception>
    /// <example>
    /// var statusId = await AccountStatusCode.ACTIVE.ToAccountStatusIdAsync(_lookupResolver);
    /// </example>
    public static async Task<uint> ToAccountStatusIdAsync(
  this AccountStatusCode statusCode,
        ILookupResolver resolver,
     CancellationToken ct = default)
    {
        return await statusCode.IdAsync(resolver, (ushort)LookupType.AccountStatus, ct);
    }

    /// <summary>
    /// Attempts to get the lookup value_id for an AccountStatusCode enum asynchronously.
    /// </summary>
    /// <param name="statusCode">The account status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id if found; otherwise null</returns>
    public static async Task<uint?> TryToAccountStatusIdAsync(
this AccountStatusCode statusCode,
        ILookupResolver resolver,
 CancellationToken ct = default)
    {
        return await statusCode.TryIdAsync(resolver, (ushort)LookupType.AccountStatus, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for an OrderStatusCode enum asynchronously.
    /// Type-specific helper for Order Status lookups (type_id = 10).
    /// </summary>
  /// <param name="statusCode">The order status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the order status</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the status is not found</exception>
    /// <example>
    /// var statusId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver);
    /// </example>
    public static async Task<uint> ToOrderStatusIdAsync(
        this OrderStatusCode statusCode,
        ILookupResolver resolver,
     CancellationToken ct = default)
    {
      return await statusCode.IdAsync(resolver, (ushort)LookupType.OrderStatus, ct);
    }

    /// <summary>
    /// Attempts to get the lookup value_id for an OrderStatusCode enum asynchronously.
    /// </summary>
    /// <param name="statusCode">The order status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id if found; otherwise null</returns>
    public static async Task<uint?> TryToOrderStatusIdAsync(
      this OrderStatusCode statusCode,
        ILookupResolver resolver,
        CancellationToken ct = default)
    {
   return await statusCode.TryIdAsync(resolver, (ushort)LookupType.OrderStatus, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for a payment method enum asynchronously.
    /// Type-specific helper for Payment Method lookups (type_id = 11).
    /// </summary>
    /// <param name="methodCode">The payment method code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the payment method</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the method is not found</exception>
    public static async Task<uint> ToPaymentMethodIdAsync(
        this System.Enum methodCode,
        ILookupResolver resolver,
        CancellationToken ct = default)
    {
        return await methodCode.IdAsync(resolver, (ushort)LookupType.PaymentMethod, ct);
    }

    /// <summary>
    /// Attempts to get the lookup value_id for a payment method enum asynchronously.
  /// </summary>
    /// <param name="methodCode">The payment method code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id if found; otherwise null</returns>
    public static async Task<uint?> TryToPaymentMethodIdAsync(
      this System.Enum methodCode,
        ILookupResolver resolver,
   CancellationToken ct = default)
    {
        return await methodCode.TryIdAsync(resolver, (ushort)LookupType.PaymentMethod, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for a TableStatusCode enum asynchronously.
    /// Type-specific helper for Table Status lookups (type_id = 5).
    /// </summary>
    /// <param name="statusCode">The table status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the table status</returns>
    public static async Task<uint> ToTableStatusIdAsync(
        this TableStatusCode statusCode,
        ILookupResolver resolver,
        CancellationToken ct = default)
  {
        return await statusCode.IdAsync(resolver, (ushort)LookupType.TableStatus, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for a ReservationStatusCode enum asynchronously.
    /// Type-specific helper for Reservation Status lookups (type_id = 8).
    /// </summary>
    /// <param name="statusCode">The reservation status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the reservation status</returns>
    public static async Task<uint> ToReservationStatusIdAsync(
        this ReservationStatusCode statusCode,
    ILookupResolver resolver,
        CancellationToken ct = default)
  {
        return await statusCode.IdAsync(resolver, (ushort)LookupType.ReservationStatus, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for a DishStatusCode enum asynchronously.
    /// Type-specific helper for Dish Status lookups (type_id = 12).
 /// </summary>
    /// <param name="statusCode">The dish status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the dish status</returns>
    public static async Task<uint> ToDishStatusIdAsync(
   this DishStatusCode statusCode,
        ILookupResolver resolver,
        CancellationToken ct = default)
    {
        return await statusCode.IdAsync(resolver, (ushort)LookupType.DishStatus, ct);
    }

  /// <summary>
    /// Gets the lookup value_id for an OrderItemStatusCode enum asynchronously.
    /// Type-specific helper for Order Item Status lookups (type_id = 13).
    /// </summary>
    /// <param name="statusCode">The order item status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
  /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the order item status</returns>
    public static async Task<uint> ToOrderItemStatusIdAsync(
        this OrderItemStatusCode statusCode,
        ILookupResolver resolver,
        CancellationToken ct = default)
    {
 return await statusCode.IdAsync(resolver, (ushort)LookupType.OrderItemStatus, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for a PromotionStatusCode enum asynchronously.
    /// Type-specific helper for Promotion Status lookups (type_id = 16).
    /// </summary>
    /// <param name="statusCode">The promotion status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the promotion status</returns>
    public static async Task<uint> ToPromotionStatusIdAsync(
      this PromotionStatusCode statusCode,
        ILookupResolver resolver,
        CancellationToken ct = default)
    {
        return await statusCode.IdAsync(resolver, (ushort)LookupType.PromotionStatus, ct);
    }

    /// <summary>
    /// Gets the lookup value_id for an InventoryTxStatusCode enum asynchronously.
  /// Type-specific helper for Inventory Transaction Status lookups (type_id = 3).
 /// </summary>
    /// <param name="statusCode">The inventory transaction status code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the inventory transaction status</returns>
    public static async Task<uint> ToInventoryTxStatusIdAsync(
        this InventoryTxStatusCode statusCode,
      ILookupResolver resolver,
  CancellationToken ct = default)
    {
      return await statusCode.IdAsync(resolver, (ushort)LookupType.InventoryTxStatus, ct);
    }

    /// <summary>
  /// Gets the lookup value_id for an InventoryTxTypeCode enum asynchronously.
    /// Type-specific helper for Inventory Transaction Type lookups (type_id = 2).
    /// </summary>
    /// <param name="typeCode">The inventory transaction type code enum</param>
    /// <param name="resolver">The lookup resolver instance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value_id for the inventory transaction type</returns>
    public static async Task<uint> ToInventoryTxTypeIdAsync(
        this InventoryTxTypeCode typeCode,
     ILookupResolver resolver,
 CancellationToken ct = default)
    {
        return await typeCode.IdAsync(resolver, (ushort)LookupType.InventoryTxType, ct);
    }

    #endregion
}

/*
 * USAGE EXAMPLES (ALL ASYNC):
 * 
 * // Basic enum extension usage
 * var statusId = await OrderStatusCode.PENDING.IdAsync(_lookupResolver, typeId: 10);
 * 
 * // Safe enum extension usage
 * var statusId = await OrderStatusCode.PENDING.TryIdAsync(_lookupResolver, typeId: 10);
 * if (statusId == null)
 * {
 *   throw new InvalidOperationException("Status not found in lookup table");
 * }
 * 
 * // String extension usage
 * var methodId = await "CREDIT_CARD".IdAsync(_lookupResolver, typeId: 11);
 * 
 * // Type-specific helper usage (cleanest)
 * var orderStatusId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver);
 * var accountStatusId = await AccountStatusCode.ACTIVE.ToAccountStatusIdAsync(_lookupResolver);
 * var paymentMethodId = await paymentEnum.ToPaymentMethodIdAsync(_lookupResolver);
 * 
 * // Safe type-specific usage
 * var statusId = await OrderStatusCode.PENDING.TryToOrderStatusIdAsync(_lookupResolver);
 * if (statusId.HasValue)
 * {
 *     order.StatusLvId = statusId.Value;
 * }
 * 
 * // In a service or repository
 * public class OrderService
 * {
 *     private readonly ILookupResolver _lookupResolver;
 *     
 *     public async Task CreateOrder(...)
 *     {
 * var order = new Order
 *         {
 *   StatusLvId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver),
 *          PaymentMethodLvId = await paymentMethod.ToPaymentMethodIdAsync(_lookupResolver)
 *   };
 *    // ...
 *     }
 * }
 * 
 * // With cancellation token
 * var statusId = await AccountStatusCode.ACTIVE.ToAccountStatusIdAsync(_lookupResolver, cancellationToken);
 */
