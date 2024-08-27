using webapi.Model;

namespace webapi.Interfaces
{
    public interface IExcelService
    {
        Task UploadExcelFileAsync(Stream fileStream, string fileName);
        Task<int> UploadDocumentAndDataAsync(Stream fileStream, string fileName);
        Task<List<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentByIdAsync(int id);
        Task<List<ExcelFile>> GetUploadedFilesAsync();
    }
}
