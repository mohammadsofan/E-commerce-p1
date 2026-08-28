using System;
using System.Linq;
using Ecommerce.Domain.DomainEvents;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
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
            Assert.Single(order.Items);
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

            Assert.Equal(OrderStatus.Placed, order.Status);
            Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
            Assert.Equal(FulfillmentStatus.Unfulfilled, order.FulfillmentStatus);
            Assert.NotNull(order.PlacedAt);
            Assert.Equal(5m, order.TotalAmount);
        }

        [Fact]
        public void PlaceOrder_RaisesOrderPlacedDomainEvent()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 1m, 1);

            order.PlaceOrder();

            Assert.Single(order.DomainEvents);
            Assert.IsType<OrderPlacedDomainEvent>(order.DomainEvents.First());
        }

        [Fact]
        public void PlaceOrder_Twice_Throws()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 1m, 1);
            order.PlaceOrder();

            Assert.Throws<DomainException>(() => order.PlaceOrder());
        }

        [Fact]
        public void MarkPaid_FromPlaced_TransitionsToPaid()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();

            order.MarkPaid();

            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
            Assert.NotNull(order.PaidAt);
        }

        [Fact]
        public void Complete_FromUnpaidOrder_Throws()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();

            Assert.Throws<DomainException>(() => order.Complete());
        }

        [Fact]
        public void Complete_FromPaid_TransitionsToCompleted()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();
            order.MarkPaid();

            order.Complete();

            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.NotNull(order.CompletedAt);
        }

        [Fact]
        public void Cancel_FromPaid_SetsCancelled()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();
            order.MarkPaid();

            order.Cancel("customer changed mind");

            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.NotNull(order.CancelledAt);
            Assert.Contains("customer changed mind", order.Notes);
        }

        [Fact]
        public void Cancel_FromCompleted_Throws()
        {
            var order = new Order();
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();
            order.MarkPaid();
            order.Complete();

            Assert.Throws<DomainException>(() => order.Cancel());
        }

        [Fact]
        public void Cancel_FromShipped_Throws()
        {
            var order = new Order { PaymentMethod = "CashOnDelivery" };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();
            order.MarkShipped("TRACK-1", "Carrier");

            Assert.Throws<DomainException>(() => order.Cancel("too late"));
        }

        [Fact]
        public void Cancel_FromDelivered_Throws()
        {
            var order = new Order { PaymentMethod = "CashOnDelivery" };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();
            order.MarkShipped("TRACK-1", "Carrier");
            order.MarkDelivered();

            Assert.Throws<DomainException>(() => order.Cancel("customer changed mind"));
        }
    }
}

