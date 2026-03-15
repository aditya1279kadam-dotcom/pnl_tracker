using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;

namespace FinanceOS.Backend.Models
{
    public record JiraWorklog
    {
        [Name("Author")]
        public string Author { get; set; } = string.Empty;
        [Name("Project")]
        public string Project { get; set; } = string.Empty;
        [Name("Issue")]
        public string Issue { get; set; } = string.Empty;
        [Name("Epic")]
        [Optional]
        public string? Epic { get; set; }
        [Name("Original Estimate")]
        [Optional]
        public double? OriginalEstimateHrs { get; set; }
        [Name("Project Category")]
        [Optional]
        public string? ProjectCategory { get; set; }
        [Name("Started Day")]
        [Optional]
        public DateTime? Date { get; set; }
        [Name("Time Spent (hours)")]
        [Optional]
        public double TimeSpentHrs { get; set; }
        
        // Calculated fields for engine
        [Ignore]
        public string ProjectKey { get; set; } = string.Empty;
        [Ignore]
        public double HourlyRate { get; set; }
        [Ignore]
        public double CappedHours { get; set; }
        [Ignore]
        public double RowCost { get; set; }
        [Ignore]
        public string FinalCategory { get; set; } = string.Empty;
        [Ignore]
        public string QuarterKey { get; set; } = string.Empty;
    }

    public record RateCardEntry
    {
        [Name("Name")]
        public string Name { get; set; } = string.Empty;
        [Name("Jira Name")]
        [Optional]
        public string? JiraName { get; set; }
        [Name("Monthly Salary")]
        [Optional]
        public double MonthlySalary { get; set; }
        [Name("Function")]
        [Optional]
        public string Function { get; set; } = string.Empty;
    }

    public record ResourceEntry
    {
        [Name("Name")]
        public string Name { get; set; } = string.Empty;
        [Name("Jira Name")]
        [Optional]
        public string JiraName { get; set; } = string.Empty;
        [Name("Date of Joining")]
        [Optional]
        public DateTime? DOJ { get; set; }
        [Name("Date of Separation", "Date of Seperation")]
        [Optional]
        public DateTime? DOS { get; set; }
    }

    public record ProjectMasterEntry
    {
        [Name("Customer Name", "Project Name", "Name")]
        public string ProjectName { get; set; } = string.Empty;
        [Name("Product")]
        [Optional]
        public string? Product { get; set; }
        [Name("Project Category")]
        [Optional]
        public string? Category { get; set; }
        [Name("Client Code", "Project Key", "Key")]
        public string ProjectKey { get; set; } = string.Empty;
        [Name("Status", "Project Status")]
        [Optional]
        public string ProjectStatus { get; set; } = "Active";
        [Name("PO Amount")]
        [Optional]
        public double PoAmount { get; set; }
        [Name("Revenue FY25")]
        [Optional]
        public double RevenueFY25 { get; set; }
        [Name("Revenue FY26")]
        [Optional]
        public double RevenueFY26 { get; set; }
        [Name("Total Signed HR Cost")]
        [Optional]
        public double TotalSignedHRCost { get; set; }
        [Name("Cost Till Last Quarter")]
        [Optional]
        public double CostTillLastQuarter { get; set; }
    }

    public record AttendanceEntry
    {
        [Name("Name")]
        public string EmployeeName { get; set; } = string.Empty;
        [Name("Date")]
        [Optional]
        public DateTime RosterDate { get; set; }
        [Name("Status")]
        [Optional]
        public string Status { get; set; } = "Present";
        [Name("Approved")]
        [Optional]
        public string Approved { get; set; } = "Yes";
    }

    public record OverheadPool
    {
        [Name("Quarter")]
        [Optional]
        public string Quarter { get; set; } = "All";
        [Name("HR Overhead")]
        [Optional]
        public double HrOverheadTotal { get; set; }
        [Name("Infra Cost")]
        [Optional]
        public double InfraTotal { get; set; }
        [Name("OPE Cost")]
        [Optional]
        public double OpeTotal { get; set; }
        [Name("Partnership Commission")]
        [Optional]
        public double CommissionTotal { get; set; }
        [Name("Other Expenses")]
        [Optional]
        public double OtherExpenses { get; set; }
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

    public class ProjectReportRow
    {
        public string Project { get; set; } = string.Empty;
        public string? Product { get; set; }
        public string? Category { get; set; }
        public string ProjectKey { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = "Active";
        public double PoAmount { get; set; }
        public double RevenueFY25 { get; set; }
        public double RevenueFY26 { get; set; }
        public double CumulativeRevenue { get; set; }
        public double BudgetToGo { get; set; }
        public double TotalSignedHRCost { get; set; }
        public double CostTillLastQuarter { get; set; }
        public double OpeningRemainingSignedHRCost { get; set; }
        public double CostIncurredCurrentQuarter { get; set; }
        public double TotalDirectCostTillDate { get; set; }
        public double ClosingRemainingSignedHRCost { get; set; }
        public double AllocatedHROverhead { get; set; }
        public double OpeCost { get; set; }
        public double InfraCost { get; set; }
        public double PartnershipCommission { get; set; }
        public double TotalFullyLoadedCost { get; set; }
        public double GrossProfit { get; set; }
        public double GrossMargin { get; set; }
        public double DirectCost { get; set; } 
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
        public double TotalJiraHours { get; set; }
        public double TotalCappedHours { get; set; }
        public bool MissingJiraID { get; set; }
    }
}
