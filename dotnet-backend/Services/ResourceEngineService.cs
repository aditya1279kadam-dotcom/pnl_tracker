using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Backend.Models;
using FuzzySharp;

namespace FinanceOS.Backend.Services
{
    public interface IResourceEngineService
    {
        ResourceCalculationResult Process(ResourceInputData data);
    }

    public class ResourceCalculationResult
    {
        public List<ResourceReportRow> ReportData { get; set; } = new();
        public ResourceSummary Summary { get; set; } = new();
        public QCReport QcReport { get; set; } = new();
    }

    public class ResourceSummary
    {
        public double AvgOverallBillability { get; set; }
        public double AvgExternalProductivity { get; set; }
        public double AvgInternalProductivity { get; set; }
        public double AvgBench { get; set; }
        public List<ResourceReportRow> TopResources { get; set; } = new();
        public List<ResourceReportRow> BottomBillability { get; set; } = new();
    }

    public class QCReport
    {
        public List<string> MissingSheets { get; set; } = new();
        public List<string> MissingRequiredHours { get; set; } = new();
        public List<string> UnknownCategories { get; set; } = new();
        public List<string> MissingJiraIDs { get; set; } = new();
        public List<dynamic> Defaulters { get; set; } = new();
        public List<dynamic> FailedCrossChecks { get; set; } = new();
    }

    public class ResourceEngineService : IResourceEngineService
    {
        public ResourceCalculationResult Process(ResourceInputData data)
        {
            var jiraDump = data.JiraDump ?? new();
            var resourceMaster = data.ResourceMaster ?? new();
            var rateCard = data.RateCard ?? new();
            var attendanceSummary = data.AttendanceSummary ?? new();
            var filters = data.Filters ?? new();

            var qcReport = new QCReport();
            var (periodStart, periodEnd) = GetPeriodBounds(filters);
            var filteredJira = jiraDump.Where(j => j.Date.HasValue && j.Date >= periodStart && j.Date <= periodEnd).ToList();
            var workingDaysInPeriod = GetWorkingDays(periodStart, periodEnd);

            // 1. Initialize resource map
            var resources = new Dictionary<string, ResourceReportRow>();
            var rateMap = rateCard.Where(r => !string.IsNullOrEmpty(r.Name))
                                  .ToDictionary(r => r.Name.Trim(), r => r.Function ?? "Unknown");

            var jiraAuthors = filteredJira.Select(j => (j.Author ?? "").Trim()).Where(a => !string.IsNullOrEmpty(a)).Distinct().ToHashSet();

            foreach (var row in resourceMaster)
            {
                var name = (row.JiraName ?? row.Name ?? "").Trim();
                var formalName = (row.Name ?? name).Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var doj = row.DOJ ?? periodStart;
                var dos = row.DOS;

                var effStart = doj > periodStart ? doj : periodStart;
                var effEnd = dos.HasValue && dos < periodEnd ? dos.Value : periodEnd;

                var reqH = effStart <= effEnd ? GetWorkingDays(effStart, effEnd) * 8 : 0;
                var func = rateMap.ContainsKey(formalName) ? rateMap[formalName] : "Unknown";

                var missingJiraId = !jiraAuthors.Contains(name);

                resources[name] = new ResourceReportRow
                {
                    ResourceName = name,
                    FormalName = formalName,
                    Function = func,
                    RequiredHours = reqH,
                    MissingJiraID = missingJiraId 
                };
            }

            // Fallback for Jira authors not in Resource List
            foreach (var author in jiraAuthors) {
                if (!resources.ContainsKey(author)) {
                    resources[author] = new ResourceReportRow {
                        ResourceName = author,
                        FormalName = author,
                        RequiredHours = workingDaysInPeriod * 8, 
                        MissingJiraID = false
                    };
                }
            }

            // Tracking total uncapped for each author
            var totalUncappedJira = filteredJira.GroupBy(j => j.Author?.Trim() ?? "")
                                             .ToDictionary(g => g.Key, g => g.Sum(j => j.TimeSpentHrs));

            // 3. Process Jira and map categories
            foreach (var row in filteredJira)
            {
                var author = (row.Author ?? "").Trim();
                if (string.IsNullOrEmpty(author) || !resources.ContainsKey(author)) continue;

                var resObj = resources[author];
                var reqH = resObj.RequiredHours;
                var totalU = totalUncappedJira.GetValueOrDefault(author, row.TimeSpentHrs);

                double cappedHours = row.TimeSpentHrs;
                if (totalU > reqH && reqH > 0)
                {
                    cappedHours = (row.TimeSpentHrs / totalU) * reqH;
                }

                var catLower = (row.ProjectCategory ?? "").ToLower();
                if (new[] { "implementation", "cr", "support", "consulting", "valuation" }.Any(c => catLower.Contains(c)) || catLower.Contains("external") || catLower.Contains("billable"))
                    resObj.ExternalHours += cappedHours;
                else if (catLower == "capex" || catLower.Contains("internal billable"))
                    resObj.InternalHours += cappedHours;
                else if (new[] { "hr", "admin", "laptop", "caapl", "internal", "general" }.Any(c => catLower.Contains(c)))
                    resObj.CAAPL_Hours += cappedHours;
                else if (new[] { "lnd", "learning", "development" }.Any(c => catLower.Contains(c)))
                    resObj.LND_Hours += cappedHours;
                else if (new[] { "sales", "pre-sales", "demo" }.Any(c => catLower.Contains(c)))
                    resObj.Sales_Hours += cappedHours;
                else if (new[] { "leave", "leaves" }.Any(c => catLower.Contains(c)))
                    resObj.Leaves_Hours_Jira += cappedHours;
                else if (new[] { "bench" }.Any(c => catLower.Contains(c)))
                    resObj.Bench_Hours_Jira += cappedHours;
                else if (!string.IsNullOrEmpty(row.ProjectCategory))
                    qcReport.UnknownCategories.Add(row.ProjectCategory);
            }

            // 4. Incorporate Attendance (Override Leaves)
            foreach (var att in attendanceSummary)
            {
                var matchedName = att.MatchedName ?? att.ResourceName;
                if (!string.IsNullOrEmpty(matchedName) && resources.ContainsKey(matchedName))
                {
                    resources[matchedName].ActualLeaveDays = att.ActualLeaveDays;
                }
            }

            // 5. Compute metrics & Defaulters
            var reportData = new List<ResourceReportRow>();
            foreach (var r in resources.Values)
            {
                var authTotalUncapped = totalUncappedJira.GetValueOrDefault(r.ResourceName, 0);
                r.TotalJiraHours = authTotalUncapped;

                var actualLeaves = r.ActualLeaveDays > 0 ? r.ActualLeaveDays : (r.Leaves_Hours_Jira / 8);
                r.Leaves_Hours_Final = Math.Max(r.Leaves_Hours_Jira, actualLeaves * 8);

                // Bench Hours
                double loggedNonBench = r.ExternalHours + r.InternalHours + r.CAAPL_Hours + r.LND_Hours + r.Sales_Hours + r.Leaves_Hours_Final;
                double nativeBench = r.RequiredHours > loggedNonBench ? r.RequiredHours - loggedNonBench : 0;
                r.Bench_Hours_Jira = authTotalUncapped == 0 ? r.RequiredHours : nativeBench; // Spec adjusted bench
                r.AdjustedBench = r.Bench_Hours_Jira; 

                r.TotalCappedHours = r.ExternalHours + r.InternalHours + r.CAAPL_Hours + r.LND_Hours + r.Sales_Hours + r.Leaves_Hours_Final + r.AdjustedBench;

                var R = r.RequiredHours;

                // Productivity metrics
                var denom = R - r.Leaves_Hours_Final;
                if (denom <= 0)
                {
                    r.ExternalProductivity = null;
                    r.OverallBillability = null;
                }
                else
                {
                    r.ExternalProductivity = r.ExternalHours / denom;
                    r.OverallBillability = (r.InternalHours + r.ExternalHours) / denom;
                }
                r.InternalProductivity = R > 0 ? (r.InternalHours + r.CAAPL_Hours + r.LND_Hours) / R : 0;
                r.BenchPercent = R > 0 ? r.AdjustedBench / R : 0;

                // Defaulter Logic
                var missingHours = Math.Max(0, R - authTotalUncapped);
                
                string flag = "";
                if (r.MissingJiraID) flag = "No Jira ID";
                else if (authTotalUncapped == 0) flag = "No logs";
                else if (missingHours > 0) flag = "Partial logs";

                // Mismatch Logic: if Jira leaves differ from Attendance leaves
                if (Math.Abs(r.ActualLeaveDays * 8 - r.Leaves_Hours_Jira) > 4) {
                    qcReport.FailedCrossChecks.Add(new {
                        ResourceName = r.FormalName,
                        JiraLeaves = r.Leaves_Hours_Jira,
                        AttendanceLeaves = r.ActualLeaveDays * 8,
                        Issue = "Leaves Mismatch"
                    });
                }

                if (!string.IsNullOrEmpty(flag)) {
                    qcReport.Defaulters.Add(new {
                        ResourceName = r.FormalName,
                        JiraTotalHours = authTotalUncapped,
                        RequiredHours = R,
                        MissingHours = missingHours,
                        Flag = flag
                    });
                }

                reportData.Add(r);
            }

            qcReport.Defaulters = qcReport.Defaulters.OrderBy(d => (double)d.GetType().GetProperty("JiraTotalHours").GetValue(d, null)).ToList();
            
            // 6. Summary Aggregates
            var filteredExt = reportData.Where(r => r.ExternalProductivity.HasValue).ToList();
            var filteredInt = reportData.Where(r => r.InternalProductivity.HasValue).ToList();
            var filteredBench = reportData.Where(r => r.BenchPercent.HasValue).ToList();
            var filteredOv = reportData.Where(r => r.OverallBillability.HasValue).ToList();

            var summary = new ResourceSummary
            {
                AvgOverallBillability = filteredOv.Any() ? filteredOv.Average(r => r.OverallBillability ?? 0) : 0,
                AvgExternalProductivity = filteredExt.Any() ? filteredExt.Average(r => r.ExternalProductivity ?? 0) : 0,
                AvgInternalProductivity = filteredInt.Any() ? filteredInt.Average(r => r.InternalProductivity ?? 0) : 0,
                AvgBench = filteredBench.Any() ? filteredBench.Average(r => r.BenchPercent ?? 0) : 0,
                TopResources = reportData.OrderByDescending(r => r.RequiredHours).Take(10).ToList(),
                BottomBillability = reportData.Where(r => r.OverallBillability.HasValue).OrderBy(r => r.OverallBillability).Take(10).ToList()
            };

            return new ResourceCalculationResult
            {
                ReportData = reportData.OrderBy(r => r.ResourceName).ToList(),
                Summary = summary,
                QcReport = qcReport
            };
        }

        private (DateTime start, DateTime end) GetPeriodBounds(CalculationFilters filters)
        {
            int year = 0;
            if (filters.Year.StartsWith("FY") && int.TryParse(filters.Year.Substring(2), out int fyNum)) {
                year = 2000 + fyNum;
            } else {
                year = DateTime.Now.Month >= 4 ? DateTime.Now.Year + 1 : DateTime.Now.Year;
            }

            var start = new DateTime(year - 1, 4, 1);
            var end = new DateTime(year, 3, 31);

            if (filters.Quarter != "All" && filters.Quarter.StartsWith("Q")) {
                int q = int.Parse(filters.Quarter.Substring(1));
                if (q == 1) { start = new DateTime(year - 1, 4, 1); end = new DateTime(year - 1, 6, 30); }
                else if (q == 2) { start = new DateTime(year - 1, 7, 1); end = new DateTime(year - 1, 9, 30); }
                else if (q == 3) { start = new DateTime(year - 1, 10, 1); end = new DateTime(year - 1, 12, 31); }
                else if (q == 4) { start = new DateTime(year, 1, 1); end = new DateTime(year, 3, 31); }
            }

            if (filters.Month != "All") {
                var months = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                int mNum = Array.IndexOf(months, filters.Month) + 1;
                if (mNum > 0) {
                    int mYear = (mNum >= 4) ? year - 1 : year;
                    start = new DateTime(mYear, mNum, 1);
                    end = start.AddMonths(1).AddDays(-1);
                }
            }
            return (start, end);
        }

        private int GetWorkingDays(DateTime start, DateTime end)
        {
            int days = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    days++;
            }
            return days;
        }
    }
}
