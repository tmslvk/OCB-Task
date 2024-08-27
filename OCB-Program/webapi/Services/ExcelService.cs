using OfficeOpenXml;

using webapi.Model;
using Microsoft.EntityFrameworkCore;
using webapi.Context;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Globalization;
using webapi.Interfaces;

namespace webapi.Services
{
    
    public class ExcelService : IExcelService
    {
        private readonly ApplicationContext _context;

        public ExcelService(ApplicationContext context)
        {
            _context = context;
        }
        private List<DateTime> GetDates(string cellValue)
        {
            var dates = new List<DateTime>();
            string pattern = @"(\d{2}\.\d{2}\.\d{4})";
            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(cellValue);

            foreach (Match match in matches)
            {
                if (DateTime.TryParseExact(match.Value, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    dates.Add(date);
                }
            }

            return dates;
        }

        public async Task UploadExcelFileAsync(Stream fileStream, string fileName)
        {
            // Чтение данных из Excel
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;

            var excelFile = new ExcelFile
            {
                Filename = fileName,
                UploadDate = DateTime.Now,
                FileContent = ((MemoryStream)fileStream).ToArray()
            };

            _context.ExcelFiles.Add(excelFile);
            await _context.SaveChangesAsync();
        }

        public async Task<int> UploadDocumentAndDataAsync(Stream fileStream, string fileName)
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;

            // Извлекаем данные документа
            var dates = GetDates(worksheet.Cells[3, 1].Text);
            var document = new Document
            {
                BankName = worksheet.Cells[1, 1].Text,
                StartDate = dates[0],
                EndDate = dates[1]
            };

            // Сначала сохраняем документ
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Загружаем категории
            await UploadCategoriesFromWorksheetAsync(worksheet);

            // Загружаем расходы
            await UploadOutlaysFromWorksheetAsync(worksheet, document.Id);

            return document.Id;
        }

        private async Task UploadCategoriesFromWorksheetAsync(ExcelWorksheet worksheet)
        {
            var categories = ExtractCategoriesFromWorksheet(worksheet);
            foreach (var categoryName in categories)
            {
                if (!_context.Categories.Any(c => c.Name == categoryName))
                {
                    var category = new Category { Name = categoryName };
                    _context.Categories.Add(category);
                }
            }
            await _context.SaveChangesAsync();
        }
        private List<string> ExtractCategoriesFromWorksheet(ExcelWorksheet worksheet)
        {
            var categories = new List<string>();
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                var cellValue = worksheet.Cells[row, 1].Text;

                if (cellValue.StartsWith("КЛАСС", StringComparison.OrdinalIgnoreCase))
                {
                    int firstSpaceIndex = cellValue.IndexOf(' ', 6);
                    if (firstSpaceIndex != -1)
                    {
                        int secondSpaceIndex = cellValue.IndexOf(' ', firstSpaceIndex + 1);
                        if (secondSpaceIndex != -1)
                        {
                            string category = cellValue.Substring(secondSpaceIndex + 1).Trim();
                            categories.Add(category);
                        }
                    }
                }
            }

            return categories;
        }

        private async Task UploadOutlaysFromWorksheetAsync(ExcelWorksheet worksheet, int documentId)
        {
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                var cells = worksheet.Cells[row, 1, row, 7].Select(cell => cell.Text.Trim()).ToArray();

                if (cells.Length >= 7)
                {
                    var accountIdText = cells[0];
                    if (int.TryParse(accountIdText, out int accountId) && accountIdText.Length > 2)
                    {
                        // Преобразуем текстовые значения в числа
                        if (decimal.TryParse(cells[1].Replace(" ", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal iSaldoActive) &&
                            decimal.TryParse(cells[2].Replace(" ", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal iSaldoPassive) &&
                            decimal.TryParse(cells[3].Replace(" ", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal turnoverDebet) &&
                            decimal.TryParse(cells[4].Replace(" ", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal turnoverCredit) &&
                            decimal.TryParse(cells[5].Replace(" ", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal oSaldoActive) &&
                            decimal.TryParse(cells[6].Replace(" ", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal oSaldoPassive))
                        {
                            var outlay = new Outlay
                            {
                                AccountId = accountId,
                                ISaldoActive = (int)iSaldoActive,
                                ISaldoPassive = (int)iSaldoPassive,
                                TurnoverDebet = (int)turnoverDebet,
                                TurnoverCredit = (int)turnoverCredit,
                                OSaldoActive = (int)oSaldoActive,
                                OSaldoPassive = (int)oSaldoPassive,
                                DocumentId = documentId
                            };

                            _context.Outlays.Add(outlay);
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Document>> GetDocumentsAsync()
        {
            return await _context.Documents
                .Include(d => d.Outlays)
                .ThenInclude(o => o.Category)
                .ToListAsync();
        }

        public async Task<Document> GetDocumentByIdAsync(int id)
        {
            return await _context.Documents
                .Include(d => d.Outlays)
                .ThenInclude(o => o.Category)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
        public async Task<List<ExcelFile>> GetUploadedFilesAsync()
        {
            return await _context.ExcelFiles.ToListAsync();
        }
    }

}
