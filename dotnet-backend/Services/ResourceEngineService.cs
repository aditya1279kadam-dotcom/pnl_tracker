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

            // 1. Initialize resource map
            var resources = new Dictionary<string, ResourceReportRow>();
            var rateMap = rateCard.Where(r => !string.IsNullOrEmpty(r.Name))
                                  .ToDictionary(r => r.Name.Trim(), r => r.Function ?? "Unknown");

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

                resources[name] = new ResourceReportRow
                {
                    ResourceName = name,
                    FormalName = formalName,
                    Function = func,
                    RequiredHours = reqH
                };
            }

            // 2. Pre-scan Jira for Total Uncapped Hours
            foreach (var row in jiraDump)
            {
                var author = (row.Author ?? "").Trim();
                var timeSpent = row.TimeSpentHrs;
                if (!string.IsNullOrEmpty(author))
                {
                    if (resources.ContainsKey(author))
                    {
                        // Custom property would be needed, but we can use a local tracking dict or just add to the model
                        // For simplicity, let's assume ExternalHours (temporary) or some other way.
                    }
                    else if (timeSpent > 0)
                    {
                        resources[author] = new ResourceReportRow
                        {
                            ResourceName = author,
                            FormalName = author,
                            Function = "Unknown",
                            RequiredHours = 0
                        };
                    }
                }
            }

            // Tracking total uncapped for each author
            var totalUncappedJira = jiraDump.GroupBy(j => j.Author?.Trim() ?? "")
                                            .ToDictionary(g => g.Key, g => g.Sum(j => j.TimeSpentHrs));

            // 3. Process Jira and map categories
            foreach (var row in jiraDump)
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

            // 4. Incorporate Attendance
            foreach (var att in attendanceSummary)
            {
                var matchedName = att.MatchedName ?? att.ResourceName;
                if (!string.IsNullOrEmpty(matchedName) && resources.ContainsKey(matchedName))
                {
                    resources[matchedName].ActualLeaveDays = att.ActualLeaveDays;
                }
            }

            // 5. Compute metrics
            var reportData = new List<ResourceReportRow>();
            foreach (var r in resources.Values)
            {
                var actualLeaves = r.ActualLeaveDays > 0 ? r.ActualLeaveDays : (r.Leaves_Hours_Jira / 8);
                double extraLeaveHours = 0;
                if (actualLeaves > (r.Leaves_Hours_Jira / 8))
                {
                    extraLeaveHours = (actualLeaves - (r.Leaves_Hours_Jira / 8)) * 8;
                    r.Leaves_Hours_Final = r.Leaves_Hours_Jira + extraLeaveHours;
                }
                else
                {
                    r.Leaves_Hours_Final = r.Leaves_Hours_Jira;
                }
                r.ActualLeaveDays = actualLeaves;

                var R = r.RequiredHours;
                var totalNonBenchJira = r.ExternalHours + r.InternalHours + r.CAAPL_Hours + r.LND_Hours + r.Sales_Hours + r.Leaves_Hours_Jira;
                var missing = R - totalNonBenchJira;
                r.AdjustedBench = missing > 0 ? missing : 0;

                double finalBench = r.Bench_Hours_Jira + r.AdjustedBench;
                if (actualLeaves > (r.Leaves_Hours_Jira / 8))
                {
                    finalBench -= extraLeaveHours;
                }
                r.FinalBench = Math.Max(0, finalBench);

                r.TotalAllocated = r.ExternalHours + r.InternalHours + r.CAAPL_Hours + r.LND_Hours + r.Sales_Hours + r.Leaves_Hours_Final + r.FinalBench;
                r.CrossCheckDelta = r.TotalAllocated - R;

                if (Math.Abs(r.CrossCheckDelta) > 0.01)
                    qcReport.FailedCrossChecks.Add(new { name = r.ResourceName, delta = r.CrossCheckDelta });

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
                r.BenchPercent = R > 0 ? r.FinalBench / R : 0;

                reportData.Add(r);
            }

            // 6. Summary Aggregates
            var filteredExt = reportData.Where(r => r.ExternalProductivity.HasValue).ToList();
            var filteredInt = reportData.Where(r => r.InternalProductivity.HasValue).ToList();
            var filteredBench = reportData.Where(r => r.BenchPercent.HasValue).ToList();
            var filteredOv = reportData.Where(r => r.OverallBillability.HasValue).ToList();

            var summary = new ResourceSummary
            {
                AvgOverallBillability = filteredOv.Any() ? filteredOv.Average(r => r.OverallBillability.Value) : 0,
                AvgExternalProductivity = filteredExt.Any() ? filteredExt.Average(r => r.ExternalProductivity.Value) : 0,
                AvgInternalProductivity = filteredInt.Any() ? filteredInt.Average(r => r.InternalProductivity.Value) : 0,
                AvgBench = filteredBench.Any() ? filteredBench.Average(r => r.BenchPercent.Value) : 0,
                TopResources = reportData.OrderByDescending(r => r.RequiredHours).Take(10).ToList(),
                BottomBillability = reportData.Where(r => r.OverallBillability.HasValue).OrderBy(r => r.OverallBillability).Take(10).ToList()
            };

            return new ResourceCalculationResult
            {
                ReportData = reportData,
                Summary = summary,
                QcReport = qcReport
            };
        }

        private (DateTime start, DateTime end) GetPeriodBounds(CalculationFilters filters)
        {
            var start = new DateTime(2024, 4, 1);
            var end = DateTime.Now;
            // Mirror logic from Node.js if needed
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
