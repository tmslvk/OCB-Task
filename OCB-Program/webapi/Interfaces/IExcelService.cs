using webapi.Model;

namespace webapi.Interfaces
{
    public interface IExcelService
    {
        Task<ExcelFile> UploadExcelFileAsync(Stream fileStream, string fileName);
        Task<int> UploadDocumentAndDataAsync(Stream fileStream, string fileName, ExcelFile excelFile);
        Task<List<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentByIdAsync(int id);
        Task<List<ExcelFile>> GetUploadedFilesAsync();
    }
}
