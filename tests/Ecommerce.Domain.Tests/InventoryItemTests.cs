using System;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Xunit;

namespace Ecommerce.Domain.Tests
{
    public class InventoryItemTests
    {
        [Fact]
        public void Reserve_WithSufficientStock_ReservesQuantity()
        {
            var item = new InventoryItem { Id = Guid.NewGuid(), AllowBackorder = false };
            item.AddStock(10);
            item.Reserve(3);

            Assert.Equal(3, item.QuantityReserved);
            Assert.Equal(7, item.Available);
        }

        [Fact]
        public void Reserve_InsufficientStock_ThrowsInventoryException()
        {
            var item = new InventoryItem { Id = Guid.NewGuid(), AllowBackorder = false };
            item.AddStock(2);
            Assert.Throws<InventoryException>(() => item.Reserve(5));
        }

        [Fact]
        public void Release_MoreThanReserved_ThrowsInventoryException()
        {
            var item = new InventoryItem { Id = Guid.NewGuid(), AllowBackorder = false };
            item.AddStock(10);
            item.Reserve(2);
            Assert.Throws<InventoryException>(() => item.Release(3));
        }
    }
}

