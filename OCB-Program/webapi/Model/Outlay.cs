using System.ComponentModel;

namespace webapi.Model
{
    public class Outlay
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal ISaldoActive { get; set; }
        public decimal ISaldoPassive { get; set; }
        public decimal TurnoverDebet { get; set; }
        public decimal TurnoverCredit { get; set; }
        public decimal OSaldoActive { get; set; }
        public decimal OSaldoPassive { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int DocumentId { get; set; }
        public Document Document { get; set; }
    }
}
