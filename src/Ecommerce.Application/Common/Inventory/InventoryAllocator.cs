using System;
using System.Collections.Generic;
using System.Linq;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Common.Inventory
{
    /// <summary>
    /// Single place that decides how a quantity is spread across the inventory rows of a
    /// product/variant. Reservation, release and fulfilment all route through here so the
    /// three paths can never drift apart.
    /// </summary>
    public static class InventoryAllocator
    {
        /// <summary>
        /// All inventory rows that can satisfy the given product/variant pair.
        /// A variant line matches only variant rows; a base line matches only rows
        /// without a variant.
        /// </summary>
        public static List<InventoryItem> CandidatesFor(
            IEnumerable<InventoryItem> pool,
            Guid productId,
            Guid? productVariantId)
        {
            var hasVariant = productVariantId.HasValue && productVariantId.Value != Guid.Empty;

            return pool
                .Where(inv => hasVariant
                    ? inv.ProductVariantId == productVariantId!.Value
                    : inv.ProductId == productId && !inv.ProductVariantId.HasValue)
                .ToList();
        }

        /// <summary>
        /// Total quantity that can be reserved for a line. Rows flagged for backorder are
        /// treated as unbounded, so the requested quantity is always satisfiable there.
        /// </summary>
        public static int AvailableFor(IEnumerable<InventoryItem> candidates, int requestedQuantity)
        {
            return candidates.Sum(inv => inv.AllowBackorder ? requestedQuantity : Math.Max(0, inv.Available));
        }

        public static bool AllowsBackorder(IEnumerable<InventoryItem> candidates)
        {
            return candidates.Any(inv => inv.AllowBackorder);
        }

        /// <summary>
        /// Greedily reserves <paramref name="quantity"/> across the candidate rows, draining
        /// the fullest warehouse first. <paramref name="allowBackorder"/> carries the effective
        /// policy for the line (catalog flag OR warehouse flag). Returns the amount that could
        /// not be reserved.
        /// </summary>
        public static int Reserve(List<InventoryItem> candidates, int quantity, bool allowBackorder = false)
        {
            var remaining = quantity;
            var ordered = candidates.OrderByDescending(i => i.Available).ToList();

            foreach (var inv in ordered)
            {
                if (remaining <= 0) break;

                var unbounded = allowBackorder || inv.AllowBackorder;
                var take = unbounded
                    ? remaining
                    : Math.Min(remaining, Math.Max(0, inv.Available));

                if (take > 0)
                {
                    inv.Reserve(take, unbounded);
                    remaining -= take;
                }
            }

            // A backorderable line must always be fully reserved somewhere, even when every
            // warehouse currently reports zero availability.
            if (remaining > 0 && allowBackorder && ordered.Count > 0)
            {
                ordered[0].Reserve(remaining, true);
                remaining = 0;
            }

            return remaining;
        }

        /// <summary>
        /// Releases up to <paramref name="quantity"/> of an existing reservation, starting
        /// with the most heavily reserved row. Returns the amount that could not be released.
        /// </summary>
        public static int Release(List<InventoryItem> candidates, int quantity)
        {
            var remaining = quantity;

            foreach (var inv in candidates.OrderByDescending(i => i.QuantityReserved))
            {
                if (remaining <= 0) break;
                if (inv.QuantityReserved <= 0) continue;

                var take = Math.Min(remaining, inv.QuantityReserved);
                inv.Release(take);
                remaining -= take;
            }

            return remaining;
        }

        /// <summary>
        /// Converts a reservation into a physical stock deduction (goods shipped).
        /// Returns the amount that could not be consumed, e.g. when the reservation was
        /// already released by an earlier operation.
        /// </summary>
        public static int ConsumeReservation(List<InventoryItem> candidates, int quantity)
        {
            var remaining = quantity;

            foreach (var inv in candidates.OrderByDescending(i => i.QuantityReserved))
            {
                if (remaining <= 0) break;
                if (inv.QuantityReserved <= 0) continue;

                var take = Math.Min(remaining, inv.QuantityReserved);
                inv.ConsumeReservation(take);
                remaining -= take;
            }

            return remaining;
        }
    }
}
