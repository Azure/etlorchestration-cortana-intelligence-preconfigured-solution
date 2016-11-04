// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdventureWorksModel.cs" company="Microsoft">
//   Copyright (c) Microsoft.  All rights reserved.
// </copyright>
// <summary>
//   Adventure Works SQL Database OLTP Model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace DataGenerator.DAL
{
    using System.Data.Entity;

    /// <summary>
    /// Adventure Works SQL Database OLTP Model.
    /// </summary>
    public class AdventureWorksModel : DbContext
    {
        public AdventureWorksModel(string connectionString)
            : base(connectionString)
        {
        }

        public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
        public virtual DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SalesOrderDetail>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<SalesOrderDetail>()
                .Property(e => e.UnitPriceDiscount)
                .HasPrecision(19, 4);

            modelBuilder.Entity<SalesOrderDetail>()
                .Property(e => e.LineTotal)
                .HasPrecision(38, 6);

            modelBuilder.Entity<SalesOrderHeader>()
                .Property(e => e.CreditCardApprovalCode)
                .IsUnicode(false);

            modelBuilder.Entity<SalesOrderHeader>()
                .Property(e => e.SubTotal)
                .HasPrecision(19, 4);

            modelBuilder.Entity<SalesOrderHeader>()
                .Property(e => e.TaxAmt)
                .HasPrecision(19, 4);

            modelBuilder.Entity<SalesOrderHeader>()
                .Property(e => e.Freight)
                .HasPrecision(19, 4);

            modelBuilder.Entity<SalesOrderHeader>()
                .Property(e => e.TotalDue)
                .HasPrecision(19, 4);
        }
    }
}