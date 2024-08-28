using OfficeOpenXml;
using webapi.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;
using webapi.Interfaces;
using webapi.Context;

namespace webapi.Services
{

    public class ExcelService : IExcelService
    {
        private readonly OCBContext _context;

        public ExcelService(OCBContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
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

        public async Task<ExcelFile> UploadExcelFileAsync(Stream fileStream, string fileName)
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
            if(await _context.ExcelFiles.AnyAsync(ef=>ef.Filename == excelFile.Filename))
            {
                return await _context.ExcelFiles.FirstOrDefaultAsync(ef=>ef.Filename == fileName);
            }

            _context.ExcelFiles.Add(excelFile);
            await _context.SaveChangesAsync();
            return excelFile;
        }

        public async Task<int> UploadDocumentAndDataAsync(Stream fileStream, string fileName, ExcelFile excelFile)
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
                EndDate = dates[1],
                ExcelFile = excelFile,
                ExcelFileId = excelFile.Id
            };

            if (!await _context.Documents.AnyAsync(d => d.BankName == document.BankName && d.EndDate == document.EndDate && d.StartDate == document.StartDate))
            {
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // Загружаем категории
                await UploadCategoriesFromWorksheetAsync(worksheet);

                // Загружаем расходы
                await UploadOutlaysFromWorksheetAsync(worksheet, document.Id);
                return document.Id;
            }
            var documentFromDb = await GetDocumentByName(document.BankName, document.StartDate, document.EndDate);

            return documentFromDb.Id;
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
            Category? currentCategory = null;

            var categories = await GetCategories();
            var seenCategories = new List<Category>();

            for (int row = 8; row <= rowCount; row++)
            {
                var cells = worksheet.Cells[row, 1, row, 7].Select(cell => cell.Text.Trim()).ToArray();

                var categoryName = ExtractCategoryName(cells[0]);

                if (!string.IsNullOrEmpty(categoryName))
                {
                    currentCategory = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    
                    if (currentCategory == null)
                    {
                        Console.WriteLine($"Category '{categoryName}' not found in database.");
                        continue;
                    }
                    seenCategories.Add(currentCategory);
                }

                if (seenCategories != null && cells.Length >= 7)
                {
                    // Получаем значения ячеек и очищаем их от лишних символов
                    var accountIdText = cells[0];
                    var iSaldoActiveText = CleanNumber(cells[1]);
                    var iSaldoPassiveText = CleanNumber(cells[2]);
                    var turnoverDebetText = CleanNumber(cells[3]);
                    var turnoverCreditText = CleanNumber(cells[4]);
                    var oSaldoActiveText = CleanNumber(cells[5]);
                    var oSaldoPassiveText = CleanNumber(cells[6]);

                    if (int.TryParse(accountIdText, out int accountId) && accountIdText.Length > 2)
                    {
                        if (decimal.TryParse(iSaldoActiveText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal iSaldoActive) &&
                            decimal.TryParse(iSaldoPassiveText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal iSaldoPassive) &&
                            decimal.TryParse(turnoverDebetText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal turnoverDebet) &&
                            decimal.TryParse(turnoverCreditText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal turnoverCredit) &&
                            decimal.TryParse(oSaldoActiveText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal oSaldoActive) &&
                            decimal.TryParse(oSaldoPassiveText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal oSaldoPassive))
                        {
                            
                            var outlay = new Outlay
                            {
                                AccountId = accountId,
                                ISaldoActive = iSaldoActive,
                                ISaldoPassive = iSaldoPassive,
                                TurnoverDebet = turnoverDebet,
                                TurnoverCredit = turnoverCredit,
                                OSaldoActive = oSaldoActive,
                                OSaldoPassive = oSaldoPassive,
                                DocumentId = documentId,
                                Category = seenCategories.Last(),
                                CategoryId = seenCategories.Last().Id,
                                Document = await GetDocumentByIdAsync(documentId),
                            };
                            _context.Outlays.Add(outlay);
                        }
                        else
                        {
                            // Логирование ошибки
                            Console.WriteLine($"Failed to parse values at row {row}: {string.Join(", ", cells)}");
                        }
                    }
                    else
                    {
                        // Логирование ошибки парсинга AccountId
                        Console.WriteLine($"Failed to parse AccountId at row {row}: {accountIdText}");
                    }
                }
                else
                {
                    // Логирование ошибки недостаточного количества ячеек
                    Console.WriteLine($"Insufficient cells at row {row}: {string.Join(", ", cells)}");
                }
            }

            await _context.SaveChangesAsync();
        }
        private string CleanNumber(string text)
        {
            return new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray()).Replace(",", ".");
        }
        public async Task<Document?> GetDocumentByName(string bankName, DateTime startDate, DateTime endDate)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.BankName == bankName && d.StartDate == startDate && d.EndDate == endDate);
        }

        public async Task<List<Document>> GetDocumentsAsync()
        {
            return await _context.Documents
                .Include(d => d.Outlays)
                .ThenInclude(o => o.Category)
                .ToListAsync();
        }

        public async Task<List<Category>> GetCategories()
        {
            return await _context.Categories.ToListAsync(); 
        }
        private string ExtractCategoryName(string cellText)
        {
            if (string.IsNullOrWhiteSpace(cellText))
            {
                return string.Empty;
            }

            // Регулярное выражение для удаления текста "КЛАСС" и последующих цифр и пробелов
            var match = Regex.Match(cellText, @"^\D*\d*\s*(.*)$");

            // Возвращаем очищенное название категории
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
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
