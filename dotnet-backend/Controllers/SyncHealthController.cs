using Microsoft.AspNetCore.Mvc;
using FinanceOS.Backend.Models;
using FinanceOS.Backend.Services;
using System.Linq;
using System.Text.Json;

namespace FinanceOS.Backend.Controllers
{
    [ApiController]
    [Route("api")]
    public class SyncHealthController : ControllerBase
    {
        private readonly IAppState _appState;
        private readonly IResourceEngineService _resourceEngine;
        private readonly IExportService _exportService;

        public SyncHealthController(IAppState appState, IResourceEngineService resourceEngine, IExportService exportService)
        {
            _appState = appState;
            _resourceEngine = resourceEngine;
            _exportService = exportService;
        }

        [HttpGet("sync-health")]
        public IActionResult GetStatus()
        {
            var res = new {
                jira = _appState.FinanceState.JiraDump.Count > 0,
                ratecard = _appState.FinanceState.RateCard.Count > 0,
                resourcelist = _appState.FinanceState.ResourceList.Count > 0,
                projectmaster = _appState.FinanceState.ProjectMaster.Count > 0,
                attendance = _appState.AttendanceSummary.Count > 0,
                lastRefresh = _appState.LastRefreshed?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"
            };
            return Ok(res);
        }

        [HttpGet("matching-report")]
        public IActionResult GetMatchingReport()
        {
            return Ok(_appState.MatchingReport);
        }

        [HttpGet("export-action-required")]
        public IActionResult ExportActionRequired()
        {
            var results = _resourceEngine.Process(_appState.ResourceState);
            var actionRequired = results.ReportData.Where(r => r.OverallBillability == null || r.RequiredHours == 0 || r.MissingJiraID).ToList();
            
            var bytes = _exportService.GenerateExcel(actionRequired, "Action Required");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "action_required.xlsx");
        }

        [HttpGet("jira-defaulters")]
        public IActionResult ExportJiraDefaulters()
        {
            var results = _resourceEngine.Process(_appState.ResourceState);
            var byteData = _exportService.GenerateExcel(results.QcReport.Defaulters, "Jira Defaulters");
             return File(byteData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "jira_defaulters.xlsx");
        }
    }
}
