namespace webapi.Model
{
    public class Document
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string BankName { get; set; }
        public List<Outlay> Outlays { get; set; }
        public int? ExcelFileId { get; set; }
        public ExcelFile? ExcelFile { get; set; }
    }
}
