using System;
using System.Collections.Generic;

namespace FinanceOS.Backend.Models
{
    public record JiraWorklog
    {
        public string Author { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string? Epic { get; set; }
        public double? OriginalEstimateHrs { get; set; }
        public string? ProjectCategory { get; set; }
        public DateTime? Date { get; set; }
        public double TimeSpentHrs { get; set; }
        
        // Calculated fields for engine
        public string ProjectKey { get; set; } = string.Empty;
        public double HourlyRate { get; set; }
        public double CappedHours { get; set; }
        public double RowCost { get; set; }
    }

    public record RateCardEntry
    {
        public string Name { get; set; } = string.Empty;
        public double MonthlySalary { get; set; }
        public string Function { get; set; } = string.Empty;
    }

    public record ResourceEntry
    {
        public string Name { get; set; } = string.Empty;
        public string JiraName { get; set; } = string.Empty;
        public DateTime? DOJ { get; set; }
        public DateTime? DOS { get; set; }
    }

    public record ProjectMasterEntry
    {
        public string ProjectName { get; set; } = string.Empty;
        public string? Product { get; set; }
        public string? Category { get; set; }
        public string ProjectKey { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = "Active";
        public double PoAmount { get; set; }
        public double RevenueFY25 { get; set; }
        public double RevenueFY26 { get; set; }
        public double TotalSignedHRCost { get; set; }
        public double CostTillLastQuarter { get; set; }
    }

    public record AttendanceEntry
    {
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime RosterDate { get; set; }
        public string FirstHalf { get; set; } = "P";
        public string SecondHalf { get; set; } = "P";
    }

    public record OverheadPool
    {
        public double HrOverheadTotal { get; set; }
        public double InfraTotal { get; set; }
        public double OpeTotal { get; set; }
        public double CommissionTotal { get; set; }
    }

    public class CalculationFilters
    {
        public string Year { get; set; } = "All";
        public string Quarter { get; set; } = "All";
        public string Month { get; set; } = "All";
    }

    public class AttendanceSummary
    {
        public string ResourceName { get; set; } = string.Empty;
        public string? MatchedName { get; set; }
        public double ActualLeaveDays { get; set; }
        public double ActualLeaveHours { get; set; }
    }

    public class FinanceInputData
    {
        public List<JiraWorklog> JiraDump { get; set; } = new();
        public List<RateCardEntry> RateCard { get; set; } = new();
        public List<ResourceEntry> ResourceList { get; set; } = new();
        public List<ProjectMasterEntry> ProjectMaster { get; set; } = new();
        public List<OverheadPool> OverheadPool { get; set; } = new();
        public List<AttendanceSummary> AttendanceSummary { get; set; } = new();
        public CalculationFilters Filters { get; set; } = new();
    }

    public class ResourceInputData
    {
        public List<JiraWorklog> JiraDump { get; set; } = new();
        public List<ResourceEntry> ResourceMaster { get; set; } = new();
        public List<RateCardEntry> RateCard { get; set; } = new();
        public List<ProjectMasterEntry> ProjectMaster { get; set; } = new();
        public List<AttendanceSummary> AttendanceSummary { get; set; } = new();
        public CalculationFilters Filters { get; set; } = new();
    }

    public class ResourceReportRow
    {
        public string ResourceName { get; set; } = string.Empty;
        public string FormalName { get; set; } = string.Empty;
        public string Function { get; set; } = "Unknown";
        public double RequiredHours { get; set; }
        public double ExternalHours { get; set; }
        public double InternalHours { get; set; }
        public double CAAPL_Hours { get; set; }
        public double LND_Hours { get; set; }
        public double Sales_Hours { get; set; }
        public double Leaves_Hours_Jira { get; set; }
        public double Bench_Hours_Jira { get; set; }
        public double ActualLeaveDays { get; set; }
        public double Leaves_Hours_Final { get; set; }
        public double AdjustedBench { get; set; }
        public double FinalBench { get; set; }
        public double TotalAllocated { get; set; }
        public double CrossCheckDelta { get; set; }
        public double? ExternalProductivity { get; set; }
        public double? InternalProductivity { get; set; }
        public double? BenchPercent { get; set; }
        public double? OverallBillability { get; set; }
        public bool MissingJiraID { get; set; }
    }
}
