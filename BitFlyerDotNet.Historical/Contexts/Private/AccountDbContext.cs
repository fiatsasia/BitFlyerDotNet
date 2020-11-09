//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Microsoft.EntityFrameworkCore;

namespace BitFlyerDotNet.Historical
{
    class AccountDbContext : DbContext
    {
        public DbSet<DbChildOrder> Orders { get; set; }
        public DbSet<DbPrivateExecution> Executions { get; set; }
        public DbSet<DbCollateral> Collaterals { get; set; }
        public DbSet<DbBalance> Balances { get; set; }

        readonly string _connStr;

        public AccountDbContext(string connStr)
            : base(new DbContextOptionsBuilder<AccountDbContext>().Options)
        {
            _connStr = connStr;
            this.Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ProductCode);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ChildOrderId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ChildOrderDate);

            modelBuilder.Entity<DbPrivateExecution>().HasIndex(b => b.ProductCode);
            modelBuilder.Entity<DbPrivateExecution>().HasIndex(b => b.ChildOrderId);
            modelBuilder.Entity<DbPrivateExecution>().HasIndex(b => b.ExecutedTime);

            modelBuilder.Entity<DbCollateral>().HasIndex(b => b.CurrencyCode);
            modelBuilder.Entity<DbCollateral>().HasIndex(b => b.Date);

            modelBuilder.Entity<DbBalance>().HasIndex(b => b.Date);
            modelBuilder.Entity<DbBalance>().HasIndex(b => b.ProductCode);
            modelBuilder.Entity<DbBalance>().HasIndex(b => b.OrderId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connStr);
        }
    }
}
