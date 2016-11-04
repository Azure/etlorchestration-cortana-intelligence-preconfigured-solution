// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResellerSaleDataGenerator.cs" company="Microsoft">
//   Copyright (c) Microsoft.  All rights reserved.
// </copyright>
// <summary>
//   Data generator to simulate reseller sales data.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace DataGenerator.DataGenerators
{
    using System;
    using System.Linq;
    using DAL;

    /// <summary>
    /// Data generator to simulate reseller sales data.
    /// </summary>
    public class ResellerSaleDataGenerator : IDataGenerator
    {
        private readonly int[] _addressIds = Enumerable.Range(1, 34).ToArray();
        private readonly int[] _creditCardIds = Enumerable.Range(1, 30).ToArray();
        private readonly int[] _currencyRateIds = Enumerable.Range(1, 30).ToArray();
        private readonly int[] _customerIds = Enumerable.Range(1, 100).ToArray();
        private readonly int[] _salesPersonIds = Enumerable.Range(274, 16).ToArray();
        private readonly int[] _salesTerritoryIds = Enumerable.Range(1, 10).ToArray();
        private readonly int[] _shipMethodIds = Enumerable.Range(1, 5).ToArray();

        private readonly SpecialOfferProduct[] _specialOfferProducts =
        {
            new SpecialOfferProduct(Enumerable.Range(707, 100).ToArray(), 1)
        };

        private readonly string _sqlDwConnectionString;

        public ResellerSaleDataGenerator(string sqlDwConnectionString)
        {
            _sqlDwConnectionString = sqlDwConnectionString;
        }

        /// <summary>
        /// Generate Reseller sales order data and insert to SQL DB.
        /// </summary>
        public void Generate()
        {
            using (var db = new AdventureWorksModel(_sqlDwConnectionString))
            {
                var soh = salesOrderHeader();
                var sod = salesOrderDetail();
                soh.SalesOrderDetails.Add(sod);
                db.SalesOrderHeaders.Add(soh);
                db.SaveChanges();
                Console.WriteLine(
                    $"Inserted SalesOrderHeader Id: {soh.SalesOrderID} and SalesOrderDetail Id: {sod.SalesOrderDetailID}..");
            }
        }

        private SalesOrderDetail salesOrderDetail()
        {
            var unitPrice = randomDecimalBetween(0m, 3000m);
            var sop = specialOfferProduct();
            var productId = RandomFromArray(sop.ProductIds);
            var specialOfferId = sop.SpecialOfferId;

            var sod = new SalesOrderDetail
            {
                CarrierTrackingNumber = idString(10),
                OrderQty = (short) randomIntBetween(1, 6),
                ProductID = productId,
                SpecialOfferID = specialOfferId,
                UnitPrice = unitPrice,
                UnitPriceDiscount = 0.1m,
                rowguid = Guid.NewGuid(),
                ModifiedDate = DateTime.Now
            };

            return sod;
        }

        private SalesOrderHeader salesOrderHeader()
        {
            var subTotal = randomDecimalBetween(0.0m, 5000m);
            var orderDate = randomDateBetween(new DateTime(2005, 1, 27), new DateTime(2012, 12, 4));
            var soh = new SalesOrderHeader
            {
                Status = status(),
                Freight = randomDecimalBetween(0.0m, 1000m),
                SubTotal = subTotal,
                AccountNumber = idString(15),
                BillToAddressID = addressId(),
                CreditCardApprovalCode = idString(15),
                CurrencyRateID = currencyRateId(),
                CustomerID = customerId(),
                ShipToAddressID = addressId(),
                SalesPersonID = salesPersonId(),
                PurchaseOrderNumber = idString(25),
                OnlineOrderFlag = onlineOrderFlag(),
                ShipMethodID = shipMethodId(),
                CreditCardID = creditCardId(),
                TerritoryID = salesTerritoryId(),
                TaxAmt = 0.1m*subTotal,
                OrderDate = orderDate,
                ShipDate = randomDateBetween(orderDate, orderDate.AddDays(20)),
                DueDate = randomDateBetween(orderDate, orderDate.AddDays(20)),
                rowguid = Guid.NewGuid(),
                ModifiedDate = DateTime.Now
            };

            return soh;
        }

        private int salesTerritoryId()
        {
            return RandomFromArray(_salesTerritoryIds);
        }

        private int creditCardId()
        {
            return RandomFromArray(_creditCardIds);
        }

        private int shipMethodId()
        {
            return RandomFromArray(_shipMethodIds);
        }

        private bool onlineOrderFlag()
        {
            return new Random().NextDouble() >= 0.5;
        }

        private SpecialOfferProduct specialOfferProduct()
        {
            return RandomFromArray(_specialOfferProducts);
        }

        private int salesPersonId()
        {
            return RandomFromArray(_salesPersonIds);
        }

        private int customerId()
        {
            return RandomFromArray(_customerIds);
        }

        private int currencyRateId()
        {
            return RandomFromArray(_currencyRateIds);
        }

        private int addressId()
        {
            return RandomFromArray(_addressIds);
        }

        private string idString(int maxLength)
        {
            return Guid.NewGuid().ToString().Substring(0, maxLength);
        }

        private byte status()
        {
            return (byte) RandomEnum<StatusEnum>();
        }

        private T RandomEnum<T>()
        {
            var values = (T[]) Enum.GetValues(typeof(T));
            return values[new Random().Next(0, values.Length)];
        }

        private T RandomFromArray<T>(T[] candidatesArray)
        {
            return candidatesArray[new Random().Next(0, candidatesArray.Length)];
        }

        private static decimal randomDecimalBetween(decimal minValue, decimal maxValue)
        {
            var next = (decimal) new Random().NextDouble();

            return minValue + next*(maxValue - minValue);
        }

        private static int randomIntBetween(int minValue, int maxValue)
        {
            var next = new Random().NextDouble();

            return minValue + (int) Math.Round(next*(maxValue - minValue));
        }

        private static DateTime randomDateBetween(DateTime start, DateTime end)
        {
            var range = (end - start).Days;
            return start.AddDays(new Random().Next(range));
        }

        private enum StatusEnum
        {
            InProcess = 1,
            Approved = 2,
            BackOrdered = 3,
            Rejected = 4,
            Shipped = 5,
            Canceled = 5
        }

        private class SpecialOfferProduct
        {
            public SpecialOfferProduct(int[] productIds, int specialOfferId)
            {
                ProductIds = productIds;
                SpecialOfferId = specialOfferId;
            }

            public int[] ProductIds { get; }
            public int SpecialOfferId { get; }
        }
    }
}