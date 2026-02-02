using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystem.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public int CustomerId { get; set; }
        public string AccountType { get; set; } // "Savings", "Checking"
        public decimal Balance { get; set; }
        public DateTime DateOpened { get; set; }
    }
}
