using System.ComponentModel;

namespace webapi.Model
{
    public class Outlay
    {
        public int AccountId { get; set; }
        public int ISaldoActive { get; set; }
        public int ISaldoPassive { get; set; }
        public int TurnoverDebet { get; set; }
        public int TurnoverCredit { get; set; }
        public int OSaldoActive { get; set; }
        public int OSaldoPassive { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int DocumentId { get; set; }
        public Document Document { get; set; }
    }
}
