using Microsoft.AspNetCore.Mvc;
using FinanceOS.Backend.Models;
using FinanceOS.Backend.Services;
using FuzzySharp;
using System.Linq;

namespace FinanceOS.Backend.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IFileProcessingService _fileService;
        private readonly IAppState _appState;

        public UploadController(IFileProcessingService fileService, IAppState appState)
        {
            _fileService = fileService;
            _appState = appState;
        }

        [HttpPost("{type}")]
        public IActionResult Upload(string type, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                using var stream = file.OpenReadStream();
                var extension = Path.GetExtension(file.FileName);
                var data = _fileService.ParseDynamicFile(stream, extension);

                switch (type.ToLower())
                {
                    case "jiradump":
                        _appState.FinanceState.JiraDump = MapToJiraWorklogs(data);
                        _appState.ResourceState.JiraDump = _appState.FinanceState.JiraDump;
                        break;
                    case "ratecard":
                        _appState.FinanceState.RateCard = MapToRateCard(data);
                        _appState.ResourceState.RateCard = _appState.FinanceState.RateCard;
                        break;
                    case "resourcelist":
                        _appState.FinanceState.ResourceList = MapToResourceEntries(data);
                        _appState.ResourceState.ResourceMaster = _appState.FinanceState.ResourceList;
                        break;
                    case "projectmaster":
                        _appState.FinanceState.ProjectMaster = MapToProjectMaster(data);
                        _appState.ResourceState.ProjectMaster = _appState.FinanceState.ProjectMaster;
                        break;
                }

                _appState.LastRefreshed = DateTime.Now;
                return Ok(new { success = true, rowCount = data.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "File parse error", details = ex.Message });
            }
        }

        [HttpPost("attendance")]
        public IActionResult UploadAttendance(IFormFile file)
        {
             if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                using var stream = file.OpenReadStream();
                var extension = Path.GetExtension(file.FileName);
                var data = _fileService.ParseDynamicFile(stream, extension);

                // Porting attendance matching logic
                var employeeData = new Dictionary<string, (double leaveDays, int rowCount)>();
                foreach (var row in data)
                {
                    var name = row.GetValueOrDefault("employeename")?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var fh = row.GetValueOrDefault("firsthalf")?.ToString()?.ToUpper().Trim() ?? "P";
                    var sh = row.GetValueOrDefault("secondhalf")?.ToString()?.ToUpper().Trim() ?? "P";

                    double dayLeave = 0;
                    if (fh == "A" && sh == "A") dayLeave = 1.0;
                    else if (fh == "A" || sh == "A") dayLeave = 0.5;

                    if (!employeeData.ContainsKey(name))
                        employeeData[name] = (0, 0);

                    var current = employeeData[name];
                    employeeData[name] = (current.leaveDays + dayLeave, current.rowCount + 1);
                }

                var jiraResources = _appState.FinanceState.ResourceList.Select(r => r.Name).ToList();
                var attendanceSummary = new List<AttendanceSummary>();
                var matchingReport = new List<dynamic>();

                foreach (var name in employeeData.Keys)
                {
                    var info = employeeData[name];
                    var bestMatch = Process.ExtractOne(name, jiraResources);
                    
                    string? matchedName = null;
                    string matchType = "Unmatched";
                    int score = 0;

                    if (bestMatch != null)
                    {
                        score = bestMatch.Score;
                        if (score >= 90)
                        {
                            matchedName = bestMatch.Value;
                            matchType = "Exact/Fuzzy (Auto)";
                        }
                    }

                    attendanceSummary.Add(new AttendanceSummary
                    {
                        ResourceName = name,
                        MatchedName = matchedName,
                        ActualLeaveDays = info.leaveDays,
                        ActualLeaveHours = info.leaveDays * 8
                    });

                    matchingReport.Add(new {
                        HRName = name,
                        MatchedNameInJira = matchedName,
                        MatchType = matchType,
                        Score = score + "%"
                    });
                }

                _appState.AttendanceSummary = attendanceSummary;
                _appState.FinanceState.AttendanceSummary = attendanceSummary;
                _appState.ResourceState.AttendanceSummary = attendanceSummary;
                _appState.MatchingReport = matchingReport;

                return Ok(new { success = true, rowCount = data.Count, resourceCount = employeeData.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Attendance Parse Error", details = ex.Message });
            }
        }

        private List<JiraWorklog> MapToJiraWorklogs(List<Dictionary<string, object>> data)
        {
            return data.Select(d => new JiraWorklog {
                Author = d.GetValueOrDefault("author")?.ToString() ?? "",
                Project = d.GetValueOrDefault("project")?.ToString() ?? "",
                Issue = d.GetValueOrDefault("issue")?.ToString() ?? "",
                Epic = d.GetValueOrDefault("epic")?.ToString(),
                ProjectCategory = d.GetValueOrDefault("projectcategory")?.ToString() ?? d.GetValueOrDefault("project category")?.ToString(),
                TimeSpentHrs = double.TryParse(d.GetValueOrDefault("timespent(hours)")?.ToString() ?? d.GetValueOrDefault("time spent (hours)")?.ToString() ?? "0", out var ts) ? ts : 0,
                OriginalEstimateHrs = double.TryParse(d.GetValueOrDefault("originalestimate")?.ToString() ?? "0", out var oe) ? oe : null,
                Date = DateTime.TryParse(d.GetValueOrDefault("startedday")?.ToString(), out var dt) ? dt : null
            }).ToList();
        }

        private List<RateCardEntry> MapToRateCard(List<Dictionary<string, object>> data)
        {
            return data.Select(d => new RateCardEntry {
                Name = d.GetValueOrDefault("name")?.ToString() ?? d.GetValueOrDefault("jiraname")?.ToString() ?? "",
                JiraName = d.GetValueOrDefault("jiraname")?.ToString(),
                MonthlySalary = double.TryParse(d.GetValueOrDefault("monthlysalary")?.ToString() ?? d.GetValueOrDefault("monthly salary")?.ToString() ?? "0", out var s) ? s : 0,
                Function = d.GetValueOrDefault("function")?.ToString() ?? ""
            }).ToList();
        }

        private List<ResourceEntry> MapToResourceEntries(List<Dictionary<string, object>> data)
        {
            return data.Select(d => new ResourceEntry {
                Name = d.GetValueOrDefault("name")?.ToString() ?? "",
                JiraName = d.GetValueOrDefault("jiraname")?.ToString() ?? d.GetValueOrDefault("jira name")?.ToString() ?? "",
                DOJ = DateTime.TryParse(d.GetValueOrDefault("dateofjoining")?.ToString() ?? d.GetValueOrDefault("doj")?.ToString(), out var doj) ? doj : null,
                DOS = DateTime.TryParse(d.GetValueOrDefault("dateofseparation")?.ToString() ?? d.GetValueOrDefault("dos")?.ToString(), out var dos) ? dos : null
            }).ToList();
        }

        private List<ProjectMasterEntry> MapToProjectMaster(List<Dictionary<string, object>> data)
        {
            return data.Select(d => new ProjectMasterEntry {
                ProjectName = d.GetValueOrDefault("customername")?.ToString() ?? d.GetValueOrDefault("projectname")?.ToString() ?? d.GetValueOrDefault("project")?.ToString() ?? "",
                Product = d.GetValueOrDefault("product")?.ToString(),
                Category = d.GetValueOrDefault("projectcategory")?.ToString() ?? d.GetValueOrDefault("category")?.ToString(),
                ProjectKey = d.GetValueOrDefault("clientcode")?.ToString() ?? d.GetValueOrDefault("projectkey")?.ToString() ?? "",
                ProjectStatus = d.GetValueOrDefault("projectstatus")?.ToString() ?? "Active",
                PoAmount = double.TryParse(d.GetValueOrDefault("poamount")?.ToString() ?? "0", out var po) ? po : 0,
                RevenueFY25 = double.TryParse(d.GetValueOrDefault("revenuefy25")?.ToString() ?? "0", out var r25) ? r25 : 0,
                RevenueFY26 = double.TryParse(d.GetValueOrDefault("revenuefy26")?.ToString() ?? "0", out var r26) ? r26 : 0,
                TotalSignedHRCost = double.TryParse(d.GetValueOrDefault("totalsignedhrcost")?.ToString() ?? "0", out var tc) ? tc : 0,
                CostTillLastQuarter = double.TryParse(d.GetValueOrDefault("costtilllastquarter")?.ToString() ?? "0", out var ctlq) ? ctlq : 0
            }).ToList();
        }
    }
}
