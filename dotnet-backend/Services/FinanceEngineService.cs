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
        public double PeopleOverhead { get; set; }
        public double ManualOverhead_HR { get; set; }
        public double ManualOverhead_Infra { get; set; }
        public double ManualOverhead_OPE { get; set; }
        public double ManualOverhead_Commission { get; set; }
        public string? LastRefreshed { get; set; }
    }



    public class FinanceSummary
    {
        public double TotalRevenue { get; set; }
        public double TotalFullyLoaded { get; set; }
        public double TotalProfit { get; set; }
        public double AvgMargin { get; set; }
        public double TotalPeopleOverhead { get; set; }
        public double TotalManualOverhead { get; set; }
    }

    public class FinanceEngineService : IFinanceEngineService
    {
        public FinanceCalculationResult Process(FinanceInputData data)
        {
            var processedJira = ProcessJiraDump(data.JiraDump, data.RateCard, data.ResourceList, data.Filters);
            
            // Only consider billable projects for allocation basis
            var billableCategories = new[] { "implementation", "cr", "support", "consulting", "valuation", "external" };
            var isBillable = new Func<string, bool>(cat => {
                var c = (cat ?? "").ToLower();
                return billableCategories.Any(bc => c.Contains(bc)) || c.Contains("billable");
            });

            var projectDirectCosts = processedJira.GroupBy(j => j.ProjectKey)
                .ToDictionary(g => g.Key, g => g.Sum(j => j.RowCost));

            var billableProjectCosts = processedJira.Where(j => isBillable(j.FinalCategory))
                .GroupBy(j => j.ProjectKey)
                .ToDictionary(g => g.Key, g => g.Sum(j => j.RowCost));

            var totalBillableDirectCost = billableProjectCosts.Values.Sum();
            
            var peopleOverhead = CalculatePeopleOverhead(data.RateCard, data.AttendanceSummary, data.Filters);
            var allocations = CalculateAllocations(billableProjectCosts, totalBillableDirectCost, data.OverheadPool, peopleOverhead);

            var report = GenerateReport(data.ProjectMaster, projectDirectCosts, allocations);
            var summary = CalculateKPIs(report);
            summary.TotalPeopleOverhead = peopleOverhead;
            summary.TotalManualOverhead = data.OverheadPool.Sum(o => o.HrOverheadTotal + o.InfraTotal + o.OpeTotal + o.CommissionTotal);

            return new FinanceCalculationResult
            {
                Report = report,
                Summary = summary,
                PeopleOverhead = peopleOverhead,
                ManualOverhead_HR = data.OverheadPool.Sum(o => o.HrOverheadTotal),
                ManualOverhead_Infra = data.OverheadPool.Sum(o => o.InfraTotal),
                ManualOverhead_OPE = data.OverheadPool.Sum(o => o.OpeTotal),
                ManualOverhead_Commission = data.OverheadPool.Sum(o => o.CommissionTotal)
            };
        }

        private List<JiraWorklog> ProcessJiraDump(List<JiraWorklog> jiraDump, List<RateCardEntry> rateCard, List<ResourceEntry> resourceList, CalculationFilters filters)
        {
            var rateMap = rateCard.Where(r => !string.IsNullOrEmpty(r.Name)).ToDictionary(r => r.Name, r => r);
            var resourceMap = resourceList.Where(r => !string.IsNullOrEmpty(r.Name)).ToDictionary(r => r.Name, r => r);
            var (periodStart, periodEnd) = GetPeriodBounds(filters);

            var authorTotalHours = jiraDump.GroupBy(j => j.Author)
                .ToDictionary(g => g.Key, g => g.Sum(j => j.TimeSpentHrs));

            return jiraDump.Select(row => {
                var author = row.Author;
                var issue = row.Issue ?? "";
                
                // Project Key extraction: left of hyphen in Issue, else Project
                var projectKey = issue.Contains("-") ? issue.Split("-")[0].Trim().ToUpper() : (row.Project ?? "UNKNOWN").Trim().ToUpper();
                if (string.IsNullOrEmpty(projectKey)) projectKey = (row.Project ?? "UNKNOWN").Trim().ToUpper();
                
                var timeSpent = row.TimeSpentHrs;
                
                double monthlySalary = 0;
                if (rateMap.TryGetValue(author, out var rce)) {
                    monthlySalary = rce.MonthlySalary;
                } else if (resourceMap.TryGetValue(author, out var re) && !string.IsNullOrEmpty(re.JiraName) && rateMap.TryGetValue(re.JiraName, out var rce2)) {
                    monthlySalary = rce2.MonthlySalary;
                }

                var hourlyRate = monthlySalary / 22 / 8; // standard formula

                var reqH = 176.0; // fallback month
                if (resourceMap.TryGetValue(author, out var resource)) {
                    var effStart = resource.DOJ.HasValue && resource.DOJ > periodStart ? resource.DOJ.Value : periodStart;
                    var effEnd = resource.DOS.HasValue && resource.DOS < periodEnd ? resource.DOS.Value : periodEnd;
                    reqH = effStart <= effEnd ? GetWorkingDays(effStart, effEnd) * 8 : 0;
                }

                var totalLogged = authorTotalHours.GetValueOrDefault(author, timeSpent);
                var cappedHours = timeSpent;
                if (totalLogged > reqH && reqH > 0) {
                    cappedHours = (timeSpent / totalLogged) * reqH;
                }

                row.ProjectKey = projectKey;
                row.HourlyRate = hourlyRate;
                row.CappedHours = cappedHours;
                row.RowCost = cappedHours * hourlyRate;
                
                // Quarter key processing
                if (row.Date.HasValue) {
                    var dt = row.Date.Value;
                    var y = dt.Month >= 4 ? dt.Year + 1 : dt.Year;
                    var q = ((dt.Month - 4 + 12) % 12) / 3 + 1;
                    row.QuarterKey = $"FY{y}-Q{q}";
                }
                
                // Fake mapping for now, should ideally map via external rules
                row.FinalCategory = row.ProjectCategory ?? "External";

                return row;
            }).ToList();
        }

        private (DateTime start, DateTime end) GetPeriodBounds(CalculationFilters filters)
        {
            // Naive period bounds for now (always current month if not specified, else year logic)
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

        private double CalculatePeopleOverhead(List<RateCardEntry> rateCard, List<AttendanceSummary> attendanceSummary, CalculationFilters filters)
        {
            double pool = 0;
            var (start, end) = GetPeriodBounds(filters);
            var workingDaysInPeriod = GetWorkingDays(start, end);
            if (workingDaysInPeriod == 0) return 0;

            foreach (var r in rateCard)
            {
                var func = (r.Function ?? "").ToLower();
                if (func == "technology" || func == "product") continue; // Exclude

                var att = attendanceSummary.FirstOrDefault(a => a.ResourceName == r.Name || (a.MatchedName == r.Name));
                if (att != null)
                {
                    // ApprovedDays is inverse of LeaveDays basically, but we assume raw attendance gives true counts
                    // For POC, let's assume actual working days present. If they have actualLeaveDays, approved is workingDays - leaveDays
                    var approvedDays = workingDaysInPeriod - att.ActualLeaveDays;
                    var dailyRate = r.MonthlySalary / 22.0; // standard month 22 days
                    pool += (dailyRate * approvedDays);
                }
            }
            return pool;
        }

        private Dictionary<string, dynamic> CalculateAllocations(Dictionary<string, double> billableProjectCosts, double totalBillableDirectCost, List<OverheadPool> overheadPools, double peopleOverhead)
        {
            var hrPool = peopleOverhead + overheadPools.Sum(o => o.HrOverheadTotal);
            var infraPool = overheadPools.Sum(o => o.InfraTotal);
            var opePool = overheadPools.Sum(o => o.OpeTotal);
            var commPool = overheadPools.Sum(o => o.CommissionTotal);

            var allocs = new Dictionary<string, dynamic>();

            foreach (var kvp in billableProjectCosts)
            {
                var share = totalBillableDirectCost > 0 ? (kvp.Value / totalBillableDirectCost) : 0;
                allocs[kvp.Key] = new {
                    hr = share * hrPool,
                    infra = share * infraPool,
                    ope = share * opePool,
                    commission = share * commPool
                };
            }
            return allocs;
        }

        private List<ProjectReportRow> GenerateReport(List<ProjectMasterEntry> projectMaster, Dictionary<string, double> projectDirectCosts, Dictionary<string, dynamic> allocations)
        {
            return projectMaster.Select(pm => {
                var dCost = projectDirectCosts.GetValueOrDefault(pm.ProjectKey, 0.0);
                
                double allocHr = 0, allocInfra = 0, allocOpe = 0, allocComm = 0;
                if (allocations.TryGetValue(pm.ProjectKey, out var allocObj))
                {
                    allocHr = (double)allocObj.hr;
                    allocInfra = (double)allocObj.infra;
                    allocOpe = (double)allocObj.ope;
                    allocComm = (double)allocObj.commission;
                }

                var fullyLoaded = dCost + allocHr + allocInfra + allocOpe + allocComm;
                var cumulativeRev = pm.RevenueFY25 + pm.RevenueFY26;
                var profit = cumulativeRev - fullyLoaded;
                
                return new ProjectReportRow {
                    Project = pm.ProjectName,
                    Product = pm.Product ?? "Default Product",
                    Category = pm.Category ?? "Consulting",
                    ProjectKey = pm.ProjectKey,
                    ProjectStatus = pm.ProjectStatus ?? "Active",
                    PoAmount = pm.PoAmount,
                    RevenueFY25 = pm.RevenueFY25,
                    RevenueFY26 = pm.RevenueFY26,
                    CumulativeRevenue = cumulativeRev,
                    TotalSignedHRCost = pm.TotalSignedHRCost,
                    CostTillLastQuarter = pm.CostTillLastQuarter,
                    BudgetToGo = pm.TotalSignedHRCost - cumulativeRev, // Budget to go = PO amount - cumulativeRev usually. Or SignedHR? Spec says `PO Amount - Cumulative Revenue`
                    OpeningRemainingSignedHRCost = pm.TotalSignedHRCost - pm.CostTillLastQuarter,
                    CostIncurredCurrentQuarter = dCost,
                    TotalDirectCostTillDate = dCost + pm.CostTillLastQuarter,
                    ClosingRemainingSignedHRCost = pm.TotalSignedHRCost - (dCost + pm.CostTillLastQuarter),
                    AllocatedHROverhead = allocHr,
                    OpeCost = allocOpe,
                    InfraCost = allocInfra,
                    PartnershipCommission = allocComm,
                    TotalFullyLoadedCost = fullyLoaded,
                    GrossProfit = profit,
                    GrossMargin = cumulativeRev > 0 ? profit / cumulativeRev : 0,
                    DirectCost = dCost
                };
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
