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

        //парсинг дат в определенном формате
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

        //создание модели ExcelFile
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

        //создание модели Document и последующее его использование в загрузках данных из файла Excel
        public async Task<int> UploadDocumentAndDataAsync(Stream fileStream, string fileName, ExcelFile excelFile)
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;

            // Извлекаем даты из документа
            var dates = GetDates(worksheet.Cells[3, 1].Text);
            var document = new Document
            {
                BankName = worksheet.Cells[1, 1].Text,
                StartDate = dates[0],
                EndDate = dates[1],
                ExcelFile = excelFile,
                ExcelFileId = excelFile.Id
            };

            if (await _context.Documents.AnyAsync(d => d.BankName == document.BankName && d.EndDate == document.EndDate && d.StartDate == document.StartDate))
            {
                var documentFromDb = await GetDocumentByName(document.BankName, document.StartDate, document.EndDate);

                return documentFromDb.Id;
            }
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Загружаем категории
            await UploadCategoriesFromWorksheetAsync(worksheet);

            // Загружаем расходы
            await UploadOutlaysFromWorksheetAsync(worksheet, document.Id);
            return document.Id;
        }

        //добавление категорий
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
        //извлечение категорий из листа excel
        private List<string> ExtractCategoriesFromWorksheet(ExcelWorksheet worksheet)
        {
            var categories = new List<string>();
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                var cellValue = worksheet.Cells[row, 1].Text;
                //проверка на наличие слова "КЛАСС" и извлечение из строки сути расходов
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
        //перенос в базу данных данных об расчетах
        private async Task UploadOutlaysFromWorksheetAsync(ExcelWorksheet worksheet, int documentId)
        {
            var rowCount = worksheet.Dimension.Rows;
            Category? currentCategory = null;
            //получение категорий
            var categories = await GetCategories();
            //создание списка встречающихся категорий
            var seenCategories = new List<Category>();

            for (int row = 8; row <= rowCount; row++)
            {
                //создание переменной, которая будет хранить строку
                var cells = worksheet.Cells[row, 1, row, 7].Select(cell => cell.Text.Trim()).ToArray();

                var categoryName = ExtractCategoryName(cells[0]);

                //если встречается КЛАСС Х то добавляем его в список просмотренных категорий
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
                    //получаем значения ячеек и очищаем их от лишних символов
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
                                //при переносе данных из Excel в БД берется последняя добавленная категория из seenCategories
                                Category = seenCategories.Last(),
                                CategoryId = seenCategories.Last().Id,
                                Document = await GetDocumentByIdAsync(documentId),
                            };
                            _context.Outlays.Add(outlay);
                        }
                        else
                        {
                            //логирование ошибки
                            Console.WriteLine($"Failed to parse values at row {row}: {string.Join(", ", cells)}");
                        }
                    }
                    else
                    {
                        //логирование ошибки парсинга AccountId
                        Console.WriteLine($"Failed to parse AccountId at row {row}: {accountIdText}");
                    }
                }
                else
                {
                    //логирование ошибки недостаточного количества ячеек
                    Console.WriteLine($"Insufficient cells at row {row}: {string.Join(", ", cells)}");
                }
            }

            await _context.SaveChangesAsync();
        }

        //преобразование строки в воспринимаемую парсером decimal строку
        private string CleanNumber(string text)
        {
            return new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray()).Replace(",", ".");
        }

        //получение документа по имени
        public async Task<Document?> GetDocumentByName(string bankName, DateTime startDate, DateTime endDate)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.BankName == bankName && d.StartDate == startDate && d.EndDate == endDate);
        }

        //получение списка документов
        public async Task<List<Document>> GetDocumentsAsync()
        {
            return await _context.Documents
                .Include(d => d.Outlays)
                .ThenInclude(o => o.Category)
                .ToListAsync();
        }

        //получение списка категорий
        public async Task<List<Category>> GetCategories()
        {
            return await _context.Categories.ToListAsync(); 
        }
        //получение категории из строки Excel
        private string ExtractCategoryName(string cellText)
        {
            if (string.IsNullOrWhiteSpace(cellText))
            {
                return string.Empty;
            }

            //регулярное выражение для удаления текста "КЛАСС" и последующих цифр и пробелов
            var match = Regex.Match(cellText, @"^\D*\d*\s*(.*)$");

            //возвращаем очищенное название категории
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        //получение документа по айди
        public async Task<Document> GetDocumentByIdAsync(int id)
        {
            return await _context.Documents
                .Include(d => d.Outlays)
                .ThenInclude(o => o.Category)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        //получение загруженных файлов
        public async Task<List<ExcelFile>> GetUploadedFilesAsync()
        {
            return await _context.ExcelFiles.ToListAsync();
        }
    }

}
