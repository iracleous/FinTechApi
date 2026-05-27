using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinTechApi.Models;
 
    public enum Currency
    {
        USD,
        EUR,
        GBP,
        JPY,
        CHF,
        AUD,
        CAD,
        // extend as needed
    }

    public enum AccountType
    {
        Checking,
        Savings,
        Brokerage,
        Margin,
        Retirement
    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer,
        Fee,
        Trade,
        Interest,
        Dividend
    }

    public enum OrderSide
    {
        Buy,
        Sell
    }

    public enum OrderStatus
    {
        New,
        PartiallyFilled,
        Filled,
        Canceled
    }

    public class Customer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<Account> Accounts { get; set; } = new();
    }

    public class Account
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public string AccountNumber { get; set; } = string.Empty;

        public AccountType Type { get; set; }

        public Currency Currency { get; set; } = Currency.USD;

        // Current ledger balance
        public decimal Balance { get; set; }

        public bool IsClosed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<Transaction> Transactions { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Trade> Trades { get; set; } = new();
    }

    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AccountId { get; set; }

        public TransactionType Type { get; set; }

        // Positive for credits, negative for debits (or use Type to interpret)
        public decimal Amount { get; set; }

        public Currency Currency { get; set; } = Currency.USD;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }

        // Optional links
        public Guid? RelatedAccountId { get; set; }
        public Guid? TradeId { get; set; }
    }

    public class Instrument
    {
        [Key]
        public string Symbol { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? ISIN { get; set; }

        public string? InstrumentType { get; set; } // e.g., Stock, Bond, ETF

        public Currency Currency { get; set; } = Currency.USD;
    }

    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public string InstrumentSymbol { get; set; } = string.Empty;

        public OrderSide Side { get; set; }

        public int Quantity { get; set; }

        // For market orders this is null
        public decimal? LimitPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.New;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExecutedAt { get; set; }

        // Optional reference(s)
        public List<Guid> TradeIds { get; set; } = new();
    }

    public class Trade
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public string InstrumentSymbol { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Commission { get; set; }

        public OrderSide Side { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Guid? OrderId { get; set; }
    }

    // Represents a position inside a portfolio/account
    public class Position
    {
        [Required]
        public string InstrumentSymbol { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal AvgPrice { get; set; }

        // MarketPrice should be updated from pricing service
        public decimal MarketPrice { get; set; }

        public decimal MarketValue => Quantity * MarketPrice;
    }

    public class Portfolio
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AccountId { get; set; }

        // Key = InstrumentSymbol
        public Dictionary<string, Position> Positions { get; set; } = new();

        // Computed helper
        public decimal TotalMarketValue
        {
            get
            {
                decimal total = 0m;
                foreach (var p in Positions.Values)
                {
                    total += p.MarketValue;
                }

                return total;
            }
        }
    }

    // Simple ledger entry for audit/history
    public class LedgerEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AccountId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public decimal Amount { get; set; }

        public Currency Currency { get; set; } = Currency.USD;

        public string? Description { get; set; }

        public Guid? TransactionId { get; set; }
    }
 