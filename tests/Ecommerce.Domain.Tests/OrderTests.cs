using System;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Xunit;

namespace Ecommerce.Domain.Tests
{
    public class OrderTests
    {
        [Fact]
        public void AddItem_UpdatesTotals()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product A", 10m, 2);

            Assert.Equal(20m, order.Subtotal);
            Assert.Equal(20m, order.TotalAmount);
            Assert.Equal(1, order.Items.Count);
        }

        [Fact]
        public void PlaceOrder_WithNoItems_Throws()
        {
            var order = new Order();
            Assert.Throws<DomainException>(() => order.PlaceOrder());
        }

        [Fact]
        public void PlaceOrder_SetsStatusAndPlacedAt()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product B", 5m, 1);

            order.PlaceOrder();

            Assert.Equal("Placed", order.Status);
            Assert.NotNull(order.PlacedAt);
            Assert.Equal(5m, order.TotalAmount);
        }
    }
}
