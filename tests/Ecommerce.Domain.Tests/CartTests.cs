using System;
using System.Linq;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Xunit;

namespace Ecommerce.Domain.Tests
{
    public class CartTests
    {
        [Fact]
        public void Create_NewCart_IsActiveAndEmpty()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);

            Assert.Equal(CartStatus.Active, cart.Status);
            Assert.Empty(cart.Items);
            Assert.Equal(0m, cart.TotalAmount);
        }

        [Fact]
        public void AddItem_NewProduct_AddsLineAndTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);

            cart.AddItem(Guid.NewGuid(), null, "Product A", 10m, 2);

            Assert.Single(cart.Items);
            Assert.Equal(20m, cart.TotalAmount);
        }

        [Fact]
        public void AddItem_SameProduct_MergesQuantity()
        {
            var productId = Guid.NewGuid();
            var cart = Cart.Create(Guid.NewGuid(), null);

            cart.AddItem(productId, null, "Product A", 10m, 1);
            cart.AddItem(productId, null, "Product A", 10m, 2);

            Assert.Single(cart.Items);
            Assert.Equal(3, cart.Items.First().Quantity);
            Assert.Equal(30m, cart.TotalAmount);
        }

        [Fact]
        public void AddItem_SameProduct_SameOptions_MergesQuantity()
        {
            var productId = Guid.NewGuid();
            var cart = Cart.Create(Guid.NewGuid(), null);

            cart.AddItem(productId, null, "Shirt", 25m, 1, "Size: M, Color: Blue");
            cart.AddItem(productId, null, "Shirt", 25m, 2, "Size: M, Color: Blue");

            Assert.Single(cart.Items);
            Assert.Equal(3, cart.Items.First().Quantity);
            Assert.Equal(75m, cart.TotalAmount);
            Assert.Equal("Size: M, Color: Blue", cart.Items.First().SelectedOptions);
        }

        [Fact]
        public void AddItem_SameProduct_DifferentOptions_CreatesSeparateCartItems()
        {
            var productId = Guid.NewGuid();
            var cart = Cart.Create(Guid.NewGuid(), null);

            cart.AddItem(productId, null, "Shirt", 25m, 1, "Size: M, Color: Blue");
            cart.AddItem(productId, null, "Shirt", 25m, 2, "Size: XL, Color: Red");

            Assert.Equal(2, cart.Items.Count);
            Assert.Equal(75m, cart.TotalAmount);
            Assert.Contains(cart.Items, i => i.SelectedOptions == "Size: M, Color: Blue" && i.Quantity == 1);
            Assert.Contains(cart.Items, i => i.SelectedOptions == "Size: XL, Color: Red" && i.Quantity == 2);
        }

        [Fact]
        public void AddItem_LegacyProduct_NullOptions_HandlesGracefully()
        {
            var productId = Guid.NewGuid();
            var cart = Cart.Create(Guid.NewGuid(), null);

            cart.AddItem(productId, null, "Classic Table", 100m, 1, null);
            cart.AddItem(productId, null, "Classic Table", 100m, 1, "");

            Assert.Single(cart.Items);
            Assert.Equal(2, cart.Items.First().Quantity);
            Assert.Null(cart.Items.First().SelectedOptions);
            Assert.Equal(200m, cart.TotalAmount);
        }


        [Fact]
        public void AddItem_InvalidQuantity_Throws()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);

            Assert.Throws<DomainException>(() => cart.AddItem(Guid.NewGuid(), null, "P", 10m, 0));
        }

        [Fact]
        public void AddItem_NegativePrice_Throws()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);

            Assert.Throws<DomainException>(() => cart.AddItem(Guid.NewGuid(), null, "P", -1m, 1));
        }

        [Fact]
        public void UpdateItemQuantity_RemovesLine_WhenZeroOrLess()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Product A", 10m, 2);
            var itemId = cart.Items.First().Id;

            cart.UpdateItemQuantity(itemId, 0);

            Assert.Empty(cart.Items);
            Assert.Equal(0m, cart.TotalAmount);
        }

        [Fact]
        public void UpdateItemQuantity_SetsQuantity_WhenPositive()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Product A", 10m, 2);
            var itemId = cart.Items.First().Id;

            cart.UpdateItemQuantity(itemId, 5);

            Assert.Equal(5, cart.Items.First().Quantity);
            Assert.Equal(50m, cart.TotalAmount);
        }

        [Fact]
        public void RemoveItem_RemovesFromCart()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Product A", 10m, 1);
            var itemId = cart.Items.First().Id;

            cart.RemoveItem(itemId);

            Assert.Empty(cart.Items);
        }

        [Fact]
        public void RemoveItem_Unknown_Throws()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);

            Assert.Throws<DomainException>(() => cart.RemoveItem(Guid.NewGuid()));
        }

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "A", 1m, 1);
            cart.AddItem(Guid.NewGuid(), null, "B", 2m, 1);

            cart.Clear();

            Assert.Empty(cart.Items);
            Assert.Equal(0m, cart.TotalAmount);
        }

        [Fact]
        public void MarkOrdered_TransitionsStatus()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);

            cart.MarkOrdered();

            Assert.Equal(CartStatus.Ordered, cart.Status);
        }

        [Fact]
        public void ApplyCoupon_PercentageDiscount_CalculatesDiscountAndSubtotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Chair", 100m, 2); // Subtotal = 200

            // 15% discount = 30
            var coupon = new Coupon { Code = "SAVE15", Type = "percentage", Value = 15m };
            cart.ApplyCoupon(coupon.Code);

            Assert.Equal("SAVE15", cart.AppliedCouponCode);
            Assert.Equal(200m, cart.Subtotal);
            
            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(30m, calculatedDiscount);
            Assert.Equal(170m, calculatedTotal);
        }

        [Fact]
        public void ApplyCoupon_FixedAmountDiscount_CalculatesCorrectTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Sofa", 250m, 1); // Subtotal = 250

            var coupon = new Coupon { Code = "FIXED50", Type = "fixed_amount", Value = 50m };
            cart.ApplyCoupon(coupon.Code);

            Assert.Equal("FIXED50", cart.AppliedCouponCode);
            Assert.Equal(250m, cart.Subtotal);
            
            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(50m, calculatedDiscount);
            Assert.Equal(200m, calculatedTotal);
        }

        [Fact]
        public void ApplyCoupon_FixedAmountDiscountWithCap_CalculatesCorrectTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Sofa", 250m, 1); // Subtotal = 250

            // Cap is 20, coupon value is 50. Discount should be 20.
            var coupon = new Coupon { Code = "FIXED50", Type = "fixed_amount", Value = 50m, MaxDiscountAmount = 20m };
            cart.ApplyCoupon(coupon.Code);

            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(20m, calculatedDiscount);
            Assert.Equal(230m, calculatedTotal);
        }

        [Fact]
        public void ApplyCoupon_FixedAmountDiscountWithLargeCap_CalculatesCorrectTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Sofa", 250m, 1); // Subtotal = 250

            // Cap is 100, coupon value is 50. Discount should be 50.
            var coupon = new Coupon { Code = "FIXED50", Type = "fixed_amount", Value = 50m, MaxDiscountAmount = 100m };
            cart.ApplyCoupon(coupon.Code);

            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(50m, calculatedDiscount);
            Assert.Equal(200m, calculatedTotal);
        }

        [Fact]
        public void ApplyCoupon_FixedAmountDiscountWithCapExceedsSubtotal_ClampsToZeroTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Lamp", 40m, 1); // Subtotal = 40

            // Coupon value is 100, Cap is 80. Both exceed 40. Total discount should be 40.
            var coupon = new Coupon { Code = "MEGA100", Type = "fixed_amount", Value = 100m, MaxDiscountAmount = 80m };
            cart.ApplyCoupon(coupon.Code);

            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(40m, calculatedDiscount);
            Assert.Equal(0m, calculatedTotal);
        }

        [Fact]
        public void ApplyCoupon_DiscountExceedsSubtotal_ClampsToZeroTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Lamp", 40m, 1); // Subtotal = 40

            var coupon = new Coupon { Code = "MEGA100", Type = "fixed_amount", Value = 100m };
            cart.ApplyCoupon(coupon.Code);

            Assert.Equal(40m, cart.Subtotal);
            
            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(40m, calculatedDiscount);
            Assert.Equal(0m, calculatedTotal);
        }

        [Fact]
        public void ApplyCoupon_ZeroValueCart_ReturnsZero()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            // No items, subtotal = 0

            var coupon = new Coupon { Code = "FIXED50", Type = "fixed_amount", Value = 50m };
            cart.ApplyCoupon(coupon.Code);

            var calculatedTotal = cart.CalculateTotals(coupon, out var calculatedDiscount);
            Assert.Equal(0m, calculatedDiscount);
            Assert.Equal(0m, calculatedTotal);
            Assert.Null(cart.AppliedCouponCode);
        }

        [Fact]
        public void RemoveCoupon_ResetsDiscountAndTotal()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Table", 300m, 1);
            var coupon = new Coupon { Code = "PROMO20", Type = "fixed_amount", Value = 60m };
            cart.ApplyCoupon(coupon.Code);

            var total = cart.CalculateTotals(coupon, out _);
            Assert.Equal(240m, total);

            cart.RemoveCoupon();
            var totalAfterRemove = cart.CalculateTotals(coupon, out var discountAfter);

            Assert.Null(cart.AppliedCouponCode);
            Assert.Equal(0m, discountAfter);
            Assert.Equal(300m, totalAfterRemove);
        }

        [Fact]
        public void Clear_WithAppliedCoupon_ClearsItemsAndCoupon()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Table", 300m, 1);
            var coupon = new Coupon { Code = "PROMO20", Type = "fixed_amount", Value = 60m };
            cart.ApplyCoupon(coupon.Code);

            Assert.Equal("PROMO20", cart.AppliedCouponCode);
            
            cart.Clear();

            Assert.Empty(cart.Items);
            Assert.Null(cart.AppliedCouponCode);
            var total = cart.CalculateTotals(coupon, out var discount);
            Assert.Equal(0m, discount);
            Assert.Equal(0m, total);
        }

        [Fact]
        public void MarkOrdered_WithAppliedCoupon_ClearsCouponAndSetsStatus()
        {
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(Guid.NewGuid(), null, "Table", 300m, 1);
            var coupon = new Coupon { Code = "PROMO20", Type = "fixed_amount", Value = 60m };
            cart.ApplyCoupon(coupon.Code);

            cart.MarkOrdered();

            Assert.Equal(CartStatus.Ordered, cart.Status);
            Assert.Null(cart.AppliedCouponCode);
        }
    }
}

