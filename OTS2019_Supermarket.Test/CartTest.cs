using NUnit.Framework;
using OTS_Supermarket.Models;
using OTS_Supermarket;
using System;

namespace OTS_Supermarket.Test
{
    [TestFixture]
    public class CartTest
    {
        private string GetWeekdayDateInRange(int minDaysInclusive, int maxDaysInclusive)
        {
            for (int d = minDaysInclusive; d <= maxDaysInclusive; d++)
            {
                var dt = DateTime.Today.AddDays(d);
                var dow = (int)dt.DayOfWeek; // Sunday = 0, Saturday = 6
                if (dow != 0 && dow != 6)
                    return dt.ToString("yyyy-MM-dd");
            }
            throw new InvalidOperationException("No weekday found in the requested range");
        }

        [Test]
        public void AddOneToCart_SuccessfullyAddsItemAndUpdatesSizeAndAmount()
        {
            // Arrange
            var cart = new Cart();
            var monitor = new Monitor();

            // Act
            cart.AddOneToCart(monitor);

            // Assert
            Assert.That(cart.Size, Is.EqualTo(1));
            Assert.That(cart.Amount, Is.EqualTo(100));
            Assert.That(cart.Monitor_counter, Is.EqualTo(1));
        }

        [Test]
        public void AddOneToCart_IncrementsCorrectTypeCounter()
        {
            // Arrange
            var cart = new Cart();
            var keyboard = new Keyboard();

            // Act
            cart.AddOneToCart(keyboard);

            // Assert
            Assert.That(cart.Size, Is.EqualTo(1));
            Assert.That(cart.Keyboard_counter, Is.EqualTo(1));
            Assert.That(cart.Amount, Is.EqualTo(50));
        }

        [Test]
        public void AddOneToCart_WhenCartHasTenItems_ThrowsException()
        {
            // Arrange
            var cart = new Cart();
            var monitor = new Monitor();
            cart.AddMultipleToCart(monitor, 10);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.AddOneToCart(monitor));
            Assert.That(ex.Message, Does.Contain("Number of items in cart must be 10 or less"));
        }

        [Test]
        public void AddMultipleToCart_AddsMultipleItemsAndUpdatesSizeAndAmount()
        {
            // Arrange
            var cart = new Cart();
            var laptop = new Laptop();

            // Act
            cart.AddMultipleToCart(laptop, 2);

            // Assert
            Assert.That(cart.Size, Is.EqualTo(2));
            Assert.That(cart.Amount, Is.EqualTo(1600));
            Assert.That(cart.Laptop_counter, Is.EqualTo(2));
        }

        [Test]
        public void AddMultipleToCart_WhenQuantityExceedsLimit_ThrowsException()
        {
            // Arrange
            var cart = new Cart();
            var monitor = new Monitor();

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.AddMultipleToCart(monitor, 11));
            Assert.That(ex.Message, Does.Contain("Number of items in cart must be 10 or less"));
        }

        [Test]
        public void DeleteAll_SuccessfullyClearsAllItemsAndResetsCounters()
        {
            // Arrange
            var cart = new Cart();
            cart.AddOneToCart(new Monitor());
            cart.AddOneToCart(new Keyboard());

            // Act
            cart.DeleteAll();

            // Assert
            Assert.That(cart.Size, Is.EqualTo(0));
            Assert.That(cart.Items.Count, Is.EqualTo(0));
            Assert.That(cart.Monitor_counter, Is.EqualTo(0));
            Assert.That(cart.Keyboard_counter, Is.EqualTo(0));
        }

        [Test]
        public void DeleteAll_WhenCartIsEmpty_ThrowsException()
        {
            // Arrange
            var cart = new Cart();

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.DeleteAll());
            Assert.That(ex.Message, Does.Contain("Cannot restore empty cart"));
        }


        [Test]
        public void Print_WhenCartEmpty_ThrowsException()
        {
            // Arrange
            var cart = new Cart();

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.Print());
            Assert.That(ex.Message, Does.Contain("Cannot print empty cart"));
        }

        [Test]
        public void Calculate_InvalidDateFormat_ThrowsException()
        {
            // Arrange
            var cart = new Cart();
            cart.Budget = 1000;

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.Calculate("31-12-2025"));
            Assert.That(ex.Message, Does.Contain("Wrong date format"));
        }

        [Test]
        public void Calculate_DateIsToday_ThrowsException()
        {
            // Arrange
            var cart = new Cart();
            cart.Budget = 1000;
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.Calculate(today));
            Assert.That(ex.Message, Does.Contain("Date of delivery can't be today's date"));
        }

        [Test]
        public void Calculate_DateMoreThanSevenDays_ThrowsException()
        {
            // Arrange
            var cart = new Cart();
            cart.Budget = 10000;
            var far = DateTime.Today.AddDays(8).ToString("yyyy-MM-dd");

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.Calculate(far));
            Assert.That(ex.Message, Does.Contain("Days for delivery must be less than 7"));
        }

        [Test]
        public void Calculate_FinalPriceExceedsBudget_ThrowsException()
        {
            // Arrange
            var cart = new Cart();
            cart.AddMultipleToCart(new Monitor(), 3); // 300
            cart.Budget = 100; // too small
            var date = GetWeekdayDateInRange(1, 3);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => cart.Calculate(date));
            Assert.That(ex.Message, Does.Contain("Not enough budget"));
        }

        [Test]
        public void Calculate_DeliveryWithin3Days_Weekday_QualifiesFor10PercentDiscount()
        {
            // Arrange
            var cart = new Cart();
            // 3 computers, 3 monitors, 3 keyboards = size 9, amount > 1500
            cart.AddMultipleToCart(new Computer(), 3); // 3600
            cart.AddMultipleToCart(new Monitor(), 3); // 300
            cart.AddMultipleToCart(new Keyboard(), 3); // 150
            cart.Budget = 10000;
            var date = GetWeekdayDateInRange(1, 3);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 10% discount expected
            var expectedPrice = cart.Amount * 0.9; // Amount after 10% discount
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

        [Test]
        public void Calculate_DeliveryWithin3Days_Weekday_QualifiesFor8PercentDiscount()
        {
            // Arrange
            var cart = new Cart();
            // size >8, laptop between 1 and 7 (use 1)
            cart.AddMultipleToCart(new Laptop(), 1); // 800
            cart.AddMultipleToCart(new Monitor(), 8); // 800 -> total size 9
            cart.Budget = 10000;
            var date = GetWeekdayDateInRange(1, 3);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 8% discount expected
            var expectedPrice = cart.Amount * 0.92;
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

        [Test]
        public void Calculate_SizeGreaterThan5_WithLaptopComputerChair_QualifiesFor5PercentDiscount()
        {
            // Arrange
            var cart = new Cart();
            cart.AddOneToCart(new Laptop());
            cart.AddOneToCart(new Computer());
            cart.AddOneToCart(new Chair());
            // add three more small items to make size >5
            cart.AddMultipleToCart(new Monitor(), 3);
            cart.Budget = 10000;
            var date = GetWeekdayDateInRange(1, 3);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 5% discount expected
            var expectedPrice = cart.Amount * 0.95;
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

        [Test]
        public void Calculate_SizeBetween6And7_AmountGreaterThan1200_QualifiesFor5PercentDiscount()
        {
            // Arrange
            var cart = new Cart();
            // Create 6 items with amount > 1200
            cart.AddOneToCart(new Computer()); //1200
            cart.AddMultipleToCart(new Monitor(), 5); //500 -> total 1700
            cart.Budget = 10000;
            var date = GetWeekdayDateInRange(1, 3);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 5% discount expected
            var expectedPrice = cart.Amount * 0.95;
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

        [Test]
        public void Calculate_Delivery4To7Days_Weekday_QualifiesFor20PercentDiscount()
        {
            // Arrange
            var cart = new Cart();
            // similar to 10% case but delivery 4-7 days
            cart.AddMultipleToCart(new Computer(), 3);
            cart.AddMultipleToCart(new Monitor(), 3);
            cart.AddMultipleToCart(new Keyboard(), 3);
            cart.Budget = 20000;
            var date = GetWeekdayDateInRange(4, 7);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 20% discount expected
            var expectedPrice = cart.Amount * 0.8;
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

        [Test]
        public void Calculate_Delivery4To7Days_Weekday_QualifiesFor18PercentDiscount_ByChairCondition()
        {
            // Arrange
            var cart = new Cart();
            // Size >5 and Amount >1200 and chair >=1
            cart.AddMultipleToCart(new Computer(), 1); //1200
            cart.AddMultipleToCart(new Monitor(), 3); //300
            cart.AddOneToCart(new Chair()); //120 -> size 5? need >5 -> add one more
            cart.AddOneToCart(new Keyboard());
            // Now size 6
            cart.Budget = 10000;
            var date = GetWeekdayDateInRange(4, 7);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 18% discount expected
            var expectedPrice = cart.Amount * 0.82;
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

        [Test]
        public void Calculate_Delivery4To7Days_Weekday_QualifiesFor18PercentDiscount_ByKeyboardMonitorCondition()
        {
            // Arrange
            var cart = new Cart();
            // keyboard >=1 AND monitor >=1 should trigger 18% even if size small
            cart.AddOneToCart(new Keyboard());
            cart.AddOneToCart(new Monitor());
            cart.Budget = 5000;
            var date = GetWeekdayDateInRange(4, 7);
            var initialBudget = cart.Budget;

            // Act
            cart.Calculate(date);

            // Assert: 18% discount expected
            var expectedPrice = cart.Amount * 0.82;
            Assert.That(initialBudget - cart.Budget, Is.EqualTo(expectedPrice).Within(0.01));
        }

       
    }
}
