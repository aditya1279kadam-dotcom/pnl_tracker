using System;
using System.Collections.Generic;
using FinanceOS.Backend.Models;

namespace FinanceOS.Backend.Services
{
    public interface IAppState
    {
        FinanceInputData FinanceState { get; set; }
        ResourceInputData ResourceState { get; set; }
        DateTime? LastRefreshed { get; set; }
        List<AttendanceSummary> AttendanceSummary { get; set; }
        List<dynamic> MatchingReport { get; set; }
        List<string> AttendanceQC { get; set; }
        void Clear();
    }

    public class AppState : IAppState
    {
        public FinanceInputData FinanceState { get; set; } = new();
        public ResourceInputData ResourceState { get; set; } = new();
        public DateTime? LastRefreshed { get; set; }
        public List<AttendanceSummary> AttendanceSummary { get; set; } = new();
        public List<dynamic> MatchingReport { get; set; } = new();
        public List<string> AttendanceQC { get; set; } = new();

        public void Clear()
        {
            FinanceState = new();
            ResourceState = new();
            AttendanceSummary = new();
            MatchingReport = new();
            AttendanceQC = new();
            LastRefreshed = null;
        }
    }
}
