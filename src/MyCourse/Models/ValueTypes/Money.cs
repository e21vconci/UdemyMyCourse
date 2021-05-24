using System;
using Microsoft.EntityFrameworkCore;
using MyCourse.Models.Enums;

namespace MyCourse.Models.ValueTypes
{
    public record Money
    {
        public Money() : this(Currency.EUR, 0.00m)
        {
        }
        public Money(Currency currency, decimal amount)
        {
            Amount = amount;
            Currency = currency;
        }
        private decimal amount = 0;
        public decimal Amount
        { 
            get
            {
                return amount;
            }
            init
            {
                if (value < 0) {
                    throw new InvalidOperationException("The amount cannot be negative");
                }
                amount = value;
            }
        }
        public Currency Currency
        {
            get; init;
        }

        // Con l'utilizzo dei record non c'è più bisogno di una logica personalizzata di uguaglianza. 
        //public override bool Equals(object obj)
        //{
        //    var money = obj as Money;
        //    return money != null &&
        //           Amount == money.Amount &&
        //           Currency == money.Currency;
        //}

        //public override int GetHashCode()
        //{
        //    return HashCode.Combine(Amount, Currency);
        //}
        
        public override string ToString()
        {
            return $"{Currency} {Amount:0.00}";
        }
    }
}