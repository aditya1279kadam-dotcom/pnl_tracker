using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Backend.Models;

namespace FinanceOS.Backend.Services
{
    public interface IFinanceEngineService
    {
        FinanceCalculationResult Process(FinanceInputData data);
    }

    public class FinanceCalculationResult
    {
        public List<ProjectReportRow> Report { get; set; } = new();
        public FinanceSummary Summary { get; set; } = new();
        public string? LastRefreshed { get; set; }
    }

    public class ProjectReportRow
    {
        public string Project { get; set; } = string.Empty;
        public string? Product { get; set; }
        public string? Category { get; set; }
        public string ProjectKey { get; set; } = string.Empty;
        public double CumulativeRevenue { get; set; }
        public double TotalFullyLoadedCost { get; set; }
        public double GrossProfit { get; set; }
        public double GrossMargin { get; set; }
        public double DirectCost { get; set; } 
    }

    public class FinanceSummary
    {
        public double TotalRevenue { get; set; }
        public double TotalFullyLoaded { get; set; }
        public double TotalProfit { get; set; }
        public double AvgMargin { get; set; }
    }

    public class FinanceEngineService : IFinanceEngineService
    {
        public FinanceCalculationResult Process(FinanceInputData data)
        {
            var processedJira = ProcessJiraDump(data.JiraDump, data.RateCard, data.ResourceList, data.Filters);
            var projectDirectCosts = processedJira.GroupBy(j => j.ProjectKey)
                .ToDictionary(g => g.Key, g => g.Sum(j => j.RowCost));

            var totalDirectCost = projectDirectCosts.Values.Sum();
            var peopleOverhead = CalculatePeopleOverhead(data.ResourceList, data.RateCard, data.AttendanceSummary, data.Filters);
            var allocations = CalculateAllocations(projectDirectCosts, totalDirectCost, data.OverheadPool, peopleOverhead);

            var report = GenerateReport(data.ProjectMaster, projectDirectCosts, allocations);

            return new FinanceCalculationResult
            {
                Report = report,
                Summary = CalculateKPIs(report)
            };
        }

        private List<JiraWorklog> ProcessJiraDump(List<JiraWorklog> jiraDump, List<RateCardEntry> rateCard, List<ResourceEntry> resourceList, CalculationFilters filters)
        {
            var rateMap = rateCard.Where(r => !string.IsNullOrEmpty(r.Name)).ToDictionary(r => r.Name, r => r.MonthlySalary);
            var resourceMap = resourceList.Where(r => !string.IsNullOrEmpty(r.Name)).ToDictionary(r => r.Name, r => r);
            var (periodStart, periodEnd) = GetPeriodBounds(filters);

            var authorTotalHours = jiraDump.GroupBy(j => j.Author)
                .ToDictionary(g => g.Key, g => g.Sum(j => j.TimeSpentHrs));

            return jiraDump.Select(row => {
                var author = row.Author;
                var issue = row.Issue;
                var projectKey = issue.Contains("-") ? issue.Split("-")[0] : "UNKNOWN";
                var timeSpent = row.TimeSpentHrs;
                var monthlySalary = rateMap.ContainsKey(author) ? rateMap[author] : 0;
                var hourlyRate = monthlySalary / 22 / 8;

                var reqH = 176.0;
                if (resourceMap.ContainsKey(author)) {
                    var resource = resourceMap[author];
                    var effStart = resource.DOJ.HasValue && resource.DOJ > periodStart ? resource.DOJ.Value : periodStart;
                    var effEnd = resource.DOS.HasValue && resource.DOS < periodEnd ? resource.DOS.Value : periodEnd;
                    reqH = effStart <= effEnd ? GetWorkingDays(effStart, effEnd) * 8 : 0;
                }

                var cappedHours = timeSpent;
                if (authorTotalHours.ContainsKey(author) && authorTotalHours[author] > reqH && reqH > 0) {
                    cappedHours = (timeSpent / authorTotalHours[author]) * reqH;
                }

                row.ProjectKey = projectKey;
                row.HourlyRate = hourlyRate;
                row.CappedHours = cappedHours;
                row.RowCost = cappedHours * hourlyRate;

                return row;
            }).ToList();
        }

        private (DateTime start, DateTime end) GetPeriodBounds(CalculationFilters filters)
        {
            var start = new DateTime(2024, 4, 1);
            var end = DateTime.Now;
            return (start, end);
        }

        private int GetWorkingDays(DateTime start, DateTime end)
        {
            int days = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1)) {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    days++;
            }
            return days;
        }

        private double CalculatePeopleOverhead(List<ResourceEntry> resourceList, List<RateCardEntry> rateCard, List<AttendanceSummary> attendanceSummary, CalculationFilters filters)
        {
            return 0;
        }

        private Dictionary<string, dynamic> CalculateAllocations(Dictionary<string, double> projectDirectCosts, double totalDirectCost, List<OverheadPool> overheadPool, double peopleOverhead)
        {
            return projectDirectCosts.Keys.ToDictionary(k => k, k => (dynamic)new { hr = 0.0, infra = 0.0, ope = 0.0, commission = 0.0 });
        }

        private List<ProjectReportRow> GenerateReport(List<ProjectMasterEntry> projectMaster, Dictionary<string, double> projectDirectCosts, Dictionary<string, dynamic> allocations)
        {
            return projectMaster.Select(pm => new ProjectReportRow {
                Project = pm.ProjectName,
                ProjectKey = pm.ProjectKey,
                CumulativeRevenue = pm.RevenueFY25 + pm.RevenueFY26
            }).ToList();
        }

        private FinanceSummary CalculateKPIs(List<ProjectReportRow> report)
        {
            var totalRevenue = report.Sum(r => r.CumulativeRevenue);
            var totalFullyLoaded = report.Sum(r => r.TotalFullyLoadedCost);
            return new FinanceSummary {
                TotalRevenue = totalRevenue,
                TotalFullyLoaded = totalFullyLoaded,
                TotalProfit = totalRevenue - totalFullyLoaded,
                AvgMargin = totalRevenue > 0 ? (totalRevenue - totalFullyLoaded) / totalRevenue : 0
            };
        }
    }
}
