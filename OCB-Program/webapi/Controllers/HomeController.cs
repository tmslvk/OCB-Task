﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using webapi.Interfaces;
using webapi.Model;
using webapi.Services;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IConfiguration _config;
        ExcelService service { get; set; }

        public HomeController(IConfiguration config, ExcelService service)
        {
            _config = config;
            this.service = service;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        //запрос для добавления файла эксель в базу данных
        [HttpPost("upload-excel")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    var excelFile = await service.UploadExcelFileAsync(stream, file.FileName);
                    int documentId = await service.UploadDocumentAndDataAsync(stream, file.FileName, excelFile);
                    return Ok(new { DocumentId = documentId });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        // запрос для просмотра списка загруженных файлов
        [HttpGet("files")]
        public async Task<IActionResult> GetUploadedFiles()
        {
            try
            {
                var files = await service.GetUploadedFilesAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        // запрос для отображения данных из СУБД по документу
        [HttpGet("document/{id}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            try
            {
                var document = await service.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound();
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }
    }
}
