namespace webapi.Model
{
    public class ExcelFile
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public DateTime UploadDate { get; set; }
        public byte[] FileContent { get; set; }
    }
}
