using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Nexus.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; }
        public string PasswordHash { get; set; } 
        public DateTimeOffset CreatedAt { get; set; }
        public List<UserLogin> Logins { get; set; } = [];
        //public List<LoginHistory> History { get; set; } = [];
    }


    //public class Transaction
    //{
    //    public Guid Id { get; set; } = Guid.NewGuid();
    //    public decimal Amount { get; set; }
    //    public string Currency { get; set; } = "USD";
    //    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    //    public string PaymentMethod { get; set; } = string.Empty;
    //    public string ReferenceNumber { get; set; } = string.Empty; // External ID from Stripe/PayPal

    //    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    //    // Foreign Key
    //    public Guid UserId { get; set; }
    //    public User User { get; set; } = null!;
    //}

    public class LoginHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTimeOffset LoginDate { get; set; } = DateTimeOffset.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string ProviderUsed { get; set; } = string.Empty; // "Google", "Credentials", etc.
    }
}
