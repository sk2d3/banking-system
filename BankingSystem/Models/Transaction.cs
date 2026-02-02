using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystem.Models
{
    class Transaction
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // "Deposit", "Withdrawal", "Transfer"
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
