using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Configuration;
using SalesforceApp.Enums;
using SalesforceApp.Mapping;
using SalesforceApp.Models;
using SalesforceApp.Services;
using System.Text.Json;
using static SalesforceWebApp.UIEnum;

namespace SalesforceWebApp.Pages
{
    public class SalesforceAccountMatchingModel
    : PageModel
    {
        [BindProperty]
        public int HeaderRowNumber { get; set; } = 1; // Default to 1, can be changed by user input

        [BindProperty]
        public IFormFile? UploadFile { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new List<string>();
        public UIEnum.MatchingStep CurrentStep { get; set; } = UIEnum.MatchingStep.Upload;
        public List<MappingRow> MappingRows { get; set; } = new();
        public List<SalesforceField> SalesforceFields =>
        Enum.GetValues<SalesforceField>()
        .ToList();

        [BindProperty]
        public string NewMappingConfigurationName { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedMappingConfigurationName { get; set; } = string.Empty;

        public List<string> SavedMappingConfigurationNames { get; set; } = new();

        private readonly IConfiguration _configuration;
        private readonly MappingConfigurationService _mappingConfigurationService;

        public SalesforceAccountMatchingModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _mappingConfigurationService = new MappingConfigurationService();
        }

        public void OnGet()
        {

        }
        public IActionResult OnPostAcceptHeaders()
        {
            LoadHeaders();

            MappingRows = SalesforceFields
            .Select(field => new MappingRow
            {
                SalesforceField = field
            }).ToList();

            LoadSavedMappingConfigurationNames();

            CurrentStep = MatchingStep.MapFields;
            return Page();
        }
        public IActionResult OnPostStartOver()
        {
            DeleteTempFileIfExists(HttpContext.Session.GetString("TemporaryFilePath"));

            HttpContext.Session.Remove("TemporaryFilePath");
            HttpContext.Session.Remove("Headers");
            HttpContext.Session.Remove("HeaderRowNumber");
            HttpContext.Session.Remove("MatchResultsFile");
            CurrentStep = MatchingStep.Upload;
            return Page();
        }

        /// <summary>
        /// Saves the currently selected header mappings under a user-provided name so they
        /// can be applied automatically to future uploads.
        /// </summary>
        public IActionResult OnPostSaveMapping()
        {
            LoadHeaders();

            MappingRows = SalesforceFields
            .Select(field => new MappingRow
            {
                SalesforceField = field,
                SelectedHeader = Request.Form[$"MappingRows[{(int)field}].SelectedHeader"]
            }).ToList();

            if (string.IsNullOrWhiteSpace(NewMappingConfigurationName))
            {
                Message = "Please enter a name for the mapping configuration before saving.";
                LoadSavedMappingConfigurationNames();
                CurrentStep = MatchingStep.MapFields;
                return Page();
            }

            int headerRowNumber = HttpContext.Session.GetInt32("HeaderRowNumber") ?? 1;

            var configuration = new MappingConfiguration
            {
                Name = NewMappingConfigurationName.Trim(),
                HeaderRowNumber = headerRowNumber,
                FieldMappings = MappingRows
                    .Where(row => !string.IsNullOrEmpty(row.SelectedHeader))
                    .ToDictionary(row => row.SalesforceField, row => row.SelectedHeader!)
            };

            _mappingConfigurationService.Save(configuration);

            Message = $"Mapping configuration '{configuration.Name}' saved.";
            NewMappingConfigurationName = string.Empty;
            SelectedMappingConfigurationName = configuration.Name;

            LoadSavedMappingConfigurationNames();
            CurrentStep = MatchingStep.MapFields;
            return Page();
        }

        /// <summary>
        /// Loads a previously saved mapping configuration and applies it to the current
        /// set of imported headers (header row number is not changed since headers were
        /// already read for the current upload).
        /// </summary>
        public IActionResult OnPostLoadMapping()
        {
            LoadHeaders();

            var configuration = string.IsNullOrEmpty(SelectedMappingConfigurationName)
                ? null
                : _mappingConfigurationService.GetByName(SelectedMappingConfigurationName);

            MappingRows = SalesforceFields
            .Select(field => new MappingRow
            {
                SalesforceField = field,
                SelectedHeader = configuration != null
                    && configuration.FieldMappings.TryGetValue(field, out var header)
                    && Headers.Contains(header)
                        ? header
                        : null
            }).ToList();

            Message = configuration != null
                ? $"Mapping configuration '{configuration.Name}' loaded."
                : "Mapping configuration not found.";

            LoadSavedMappingConfigurationNames();
            CurrentStep = MatchingStep.MapFields;
            return Page();
        }

        /// <summary>
        /// Deletes a previously saved mapping configuration by name, keeping the saved list
        /// from becoming cluttered with outdated or one-off mappings.
        /// </summary>
        public IActionResult OnPostDeleteMapping()
        {
            LoadHeaders();

            MappingRows = SalesforceFields
            .Select(field => new MappingRow
            {
                SalesforceField = field,
                SelectedHeader = Request.Form[$"MappingRows[{(int)field}].SelectedHeader"]
            }).ToList();

            if (string.IsNullOrEmpty(SelectedMappingConfigurationName))
            {
                Message = "Please select a saved mapping to delete.";
            }
            else
            {
                _mappingConfigurationService.Delete(SelectedMappingConfigurationName);
                Message = $"Mapping configuration '{SelectedMappingConfigurationName}' deleted.";
                SelectedMappingConfigurationName = string.Empty;
            }

            LoadSavedMappingConfigurationNames();
            CurrentStep = MatchingStep.MapFields;
            return Page();
        }

        public IActionResult OnPostMapFields()
        {
            int headerRowNumber = HttpContext.Session.GetInt32("HeaderRowNumber") ?? 1;
            LoadHeaders();
            MappingRows = SalesforceFields
            .Select(field => new MappingRow
            {
                SalesforceField = field,
                SelectedHeader = Request.Form[$"MappingRows[{(int)field}].SelectedHeader"]
            }).ToList();
            int x = MappingRows.Count;

            string filePath = HttpContext.Session.GetString("TemporaryFilePath") ?? string.Empty;
            if (string.IsNullOrEmpty(filePath))
            {
                Message = "Temporary file path not found in session.";
                return Page();
            }

            AccountMatchingResult result;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var accountMatchingRequest = new AccountMatchingRequest
                (
                    headerRowNumber,
                    filePath,
                    stream,
                    MappingRows
                );
                string connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                result = new AccountMatchingService().MatchAccounts(accountMatchingRequest, connectionString);
            }

            // The uploaded file has been fully read and is no longer needed once matching completes.
            DeleteTempFileIfExists(filePath);
            HttpContext.Session.Remove("TemporaryFilePath");

            Message = result.ResultsMessage;
            HttpContext.Session.SetString(
                "MatchResultsFile",
                Convert.ToBase64String(result.GetWorkbookBytes()));

            CurrentStep = MatchingStep.Results;
            return Page();
        }

        public IActionResult OnGetDownloadResults()
        {
            var base64 = HttpContext.Session.GetString("MatchResultsFile");
            if (string.IsNullOrEmpty(base64))
            {
                return NotFound();
            }

            var bytes = Convert.FromBase64String(base64);
            const string contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(bytes, contentType, "AccountMatchResults.xlsx");
        }

        public async Task OnPostAsync()
        {
            Message = "Post Received";
            if (UploadFile == null)
            {
                Message = "Please select a file.";
                return;
            }

            // If the user re-uploads a new file mid-flow, clean up any previous temp file first.
            DeleteTempFileIfExists(HttpContext.Session.GetString("TemporaryFilePath"));

            var tempPath = SaveUploadFile(UploadFile);

            var extension = tempPath != null ? Path.GetExtension(tempPath).ToLower() : string.Empty;
            if (string.IsNullOrEmpty(extension))
            {
                Message = "Could not determine the file extension.";
                return;
            }
            if (string.IsNullOrEmpty(tempPath))
            {
                Message = "Could not save the uploaded file.";
                return;
            }
            HttpContext.Session.SetString("TemporaryFilePath", tempPath);

            var allowedExtensions = new[]
            {
                ".csv",
                ".xls",
                ".xlsx"
            };

            if (!allowedExtensions.Contains(extension))
            {
                Message =
                $"File type {extension} is not supported.";
                CurrentStep = UIEnum.MatchingStep.Upload;
                DeleteTempFileIfExists(tempPath);
                HttpContext.Session.Remove("TemporaryFilePath");
                return;
            }

            using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
            if (UploadFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                CsvImportService csvImportService = new CsvImportService();
                Headers = csvImportService.GetHeaders(stream, HeaderRowNumber);
                if (Headers != null && Headers.Count > 0)
                {
                    Message = $"Found {Headers.Count} headers. Accept and continue to map fields. " +
                        $"\nTo start over, select Header Row and Upload a new File.";
                    HttpContext.Session.SetString("Headers", JsonSerializer.Serialize(Headers));
                }
                else
                {
                    CurrentStep = UIEnum.MatchingStep.Upload;
                    Message = "No headers found in the uploaded file.";
                    return;
                }
                HttpContext.Session.SetInt32("HeaderRowNumber", HeaderRowNumber);
                CurrentStep = UIEnum.MatchingStep.ReviewHeaders;
            }
            else if (UploadFile.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) || UploadFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ExcelImportService excelImportService = new ExcelImportService();
                Headers = excelImportService.GetHeaders(stream, HeaderRowNumber);
                if (Headers != null)
                {
                    Message = $"Found {Headers.Count} headers. Accept and continue to map fields. " +
                        $"\nTo start over, select Header Row and Upload a new File.";
                    HttpContext.Session.SetString("Headers", JsonSerializer.Serialize(Headers));
                }
                else
                {
                    CurrentStep = UIEnum.MatchingStep.Upload;
                    Message = "No headers found in the uploaded file.";
                    return;
                }
                HttpContext.Session.SetInt32("HeaderRowNumber", HeaderRowNumber);
                CurrentStep = UIEnum.MatchingStep.ReviewHeaders;

            }
            else if (UploadFile.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) || UploadFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ExcelImportService excelImportService = new ExcelImportService();
                Headers = excelImportService.GetHeaders(stream, HeaderRowNumber);
                if (Headers != null)
                {
                    Message = $"Found {Headers.Count} headers. Accept and continue to map fields. " +
                        $"\nTo start over, select Header Row and Upload a new File.";
                    HttpContext.Session.SetString("Headers", JsonSerializer.Serialize(Headers));
                }
                else
                {
                    CurrentStep = UIEnum.MatchingStep.Upload;
                    Message = "No headers found in the uploaded file.";
                    return;
                }
                HttpContext.Session.SetInt32("HeaderRowNumber", HeaderRowNumber);
                CurrentStep = UIEnum.MatchingStep.ReviewHeaders;

            }
        }
        private void LoadHeaders()
        {
            var json = HttpContext.Session.GetString("Headers");

            if (!string.IsNullOrEmpty(json))
            {
                Headers =
                JsonSerializer.Deserialize<List<string>>(json)
                ?? new();
            }
        }

        private void LoadSavedMappingConfigurationNames()
        {
            SavedMappingConfigurationNames = _mappingConfigurationService
                .GetAll()
                .Select(c => c.Name)
                .OrderBy(n => n)
                .ToList();
        }

        private string SaveUploadFile(IFormFile uploadFile)
        {
            var extension = Path.GetExtension(uploadFile.FileName);

            var fileName = $"{Guid.NewGuid()}{extension}";

            var uploadFolder = Path.Combine(Path.GetTempPath(), "SalesforceAccountMatching");
            Directory.CreateDirectory(uploadFolder);

            var tempPath = Path.Combine(
                uploadFolder,
                fileName);

            using var stream = new FileStream(tempPath, FileMode.Create);

            uploadFile.CopyTo(stream);

            return tempPath;
        }

        /// <summary>
        /// Deletes the given temp file if it exists, swallowing any I/O errors (e.g. file already
        /// deleted, in use, or inaccessible) since cleanup failures should never break the user's
        /// workflow.
        /// </summary>
        private void DeleteTempFileIfExists(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Intentionally ignored - cleanup is best-effort. An orphaned file here will be
                // caught by the background cleanup service (TempFileCleanupService).
            }
        }
    }
}