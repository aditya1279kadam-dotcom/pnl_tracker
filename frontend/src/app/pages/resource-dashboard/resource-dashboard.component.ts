import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '../../components/header/header.component';
import { FinanceApiService } from '../../services/finance-api.service';
import { DataProcessingService } from '../../services/data-processing.service';
import { FilterService } from '../../services/filter.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-resource-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, HeaderComponent],
  template: `
    <app-header title="Resource Analytics" subtitle="Timesheet & Cost Allocation Performance" [lastRefreshed]="data?.lastRefreshed" (toggleSidebar)="toggleSidebar()">
      <button (click)="exportCSV()" class="btn-gold" style="height: 44px;">Download Report (CSV)</button>
    </app-header>

    <div *ngIf="!data" style="height: 60vh; display: flex; flex-direction: column; align-items: center; justify-content: center; text-align: center;">
        <div style="background: rgba(129, 140, 248, 0.1); padding: 40px; border-radius: 24px; border: 1px dashed rgba(129, 140, 248, 0.3);">
            <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="#818cf8" stroke-width="1.5" style="margin-bottom: 24px;"><path d="M21 12V7a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h7"></path><path d="M16 5V3"></path><path d="M8 5V3"></path><path d="M3 9h18"></path><path d="M20 21l-3-3 3-3"></path><path d="M17 18h4"></path></svg>
            <h2 style="font-weight: 800; margin-bottom: 12px;">No Resource Intelligence</h2>
            <p style="color: var(--text-muted); max-width: 400px; margin-bottom: 32px;">We couldn't find any resource timesheet results. Please go to the Sync Center to upload your timesheet extracts.</p>
            <a href="/" class="btn-gold" style="text-decoration: none; padding: 12px 32px; display: inline-block;">Go to Sync Center</a>
        </div>
    </div>

    <ng-container *ngIf="data">
    <ng-container *ngIf="data">
      <div class="kpi-row" [style.opacity]="loading ? '0.5' : '1'">
          <div class="kpi-panel">
              <div class="tag">Total Resources</div>
              <div class="amount">{{ summary.totalResources }}</div>
          </div>
          <div class="kpi-panel">
              <div class="tag">Avg Billability</div>
              <div class="amount">{{ (summary.avgOverallBillability * 100).toFixed(1) }}%</div>
          </div>
          <div class="kpi-panel">
              <div class="tag">Avg External Productivity</div>
              <div class="amount">{{ (summary.avgExternalProductivity * 100).toFixed(1) }}%</div>
          </div>
          <div class="kpi-panel">
              <div class="tag">Bench %</div>
              <div class="amount">{{ (summary.avgBench * 100).toFixed(1) }}%</div>
          </div>
      </div>

      <div class="data-table-container" style="margin-top: 24px;" [style.opacity]="loading ? '0.5' : '1'">
          <div style="padding: 24px; border-bottom: 1px solid var(--border); border-top-left-radius: 12px; border-top-right-radius: 12px; background: white;">
              <h3 style="margin:0; font-weight: 800;">Resource Extraction Ledger</h3>
          </div>
          <div style="overflow-x: auto; border-radius: 0 0 12px 12px; margin-bottom: 20px;">
              <table style="width: 100%; border-collapse: separate; border-spacing: 0; min-width: 1800px;">
                  <thead>
                      <tr>
                          <th style="position: sticky; left: 0; z-index: 10; background: #f8fafc;">Resource Name</th>
                          <th>Function</th>
                          <th>FY-Quarter</th>
                          <th>Jira Total Hours</th>
                          <th>Required Hours</th>
                          <th>Capped Hours</th>
                          <th>External</th>
                          <th>Internal</th>
                          <th>CAAPL</th>
                          <th>LND</th>
                          <th>Sales</th>
                          <th>Leaves</th>
                          <th>Bench</th>
                          <th>Productivity</th>
                          <th>Flags</th>
                      </tr>
                  </thead>
                  <tbody>
                      <tr *ngFor="let row of paginatedData">
                          <td style="position: sticky; left: 0; z-index: 5; background: white; font-weight: 700;">
                               {{ row.formalName || row.resourceName }}
                          </td>
                          <td><span class="badge-pill" style="background:#f1f5f9; color:#475569">{{ row.function }}</span></td>
                          <td>{{ filters.year }}-{{ filters.quarter }}</td>
                          <td style="font-weight: 600;">{{ row.totalJiraHours.toFixed(1) }}h</td>
                          <td style="color: var(--text-muted)">{{ row.requiredHours.toFixed(1) }}h</td>
                          <td style="font-weight: 700;">{{ row.totalCappedHours.toFixed(1) }}h</td>
                          <td>{{ row.externalHours.toFixed(1) }}</td>
                          <td>{{ row.internalHours.toFixed(1) }}</td>
                          <td>{{ row.caapL_Hours.toFixed(1) }}</td>
                          <td>{{ row.lnD_Hours.toFixed(1) }}</td>
                          <td>{{ row.sales_Hours.toFixed(1) }}</td>
                          <td>{{ row.leaves_Hours_Final.toFixed(1) }}</td>
                          <td>{{ row.adjustedBench.toFixed(1) }}</td>
                          <td>
                              <div style="display: flex; flex-direction: column; gap: 4px; font-size: 0.75rem;">
                                  <span>Bill: {{ (row.overallBillability * 100).toFixed(0) }}%</span>
                                  <span>Ext: {{ (row.externalProductivity * 100).toFixed(0) }}%</span>
                              </div>
                          </td>
                          <td>
                              <div style="display: flex; gap: 4px;">
                                  <span *ngIf="row.missingJiraID" class="badge-pill bg-amber">Missing ID</span>
                                  <span *ngIf="row.totalJiraHours === 0" class="badge-pill bg-red">No Logs</span>
                              </div>
                          </td>
                      </tr>
                      <tr *ngIf="paginatedData.length === 0">
                          <td colspan="15" style="text-align: center; padding: 32px; color: var(--text-muted);">No resource records matched.</td>
                      </tr>
                  </tbody>
              </table>
          </div>
          <div class="pager" *ngIf="data.report?.length > 0">
              <div style="font-size: 0.8rem; font-weight: 600; color: var(--text-muted)">
                  Record {{ ((currentPage - 1) * pageSize) + 1 }} to {{ Math.min(currentPage * pageSize, data.report.length) }} of {{ data.report.length }}
              </div>
              <div style="display: flex; gap: 8px;">
                  <button class="btn-ghost" [disabled]="currentPage === 1" (click)="setPage(currentPage - 1)">Prev</button>
                  <button class="btn-ghost" [disabled]="currentPage === totalPages" (click)="setPage(currentPage + 1)">Next</button>
              </div>
          </div>
      </div>
    </ng-container>
  `,
  styles: ``
})
export class ResourceDashboardComponent implements OnInit {
  data: any = null;
  loading = false;
  Math = Math;

  filters: any = {};
  summary: any = {};
  paginatedData: any[] = [];
  currentPage = 1;
  pageSize = 12;

  private filterSub: Subscription | null = null;

  constructor(
    private api: FinanceApiService, 
    public dataProc: DataProcessingService,
    private filterService: FilterService
  ) { }

  ngOnInit() {
    this.filterSub = this.filterService.currentFilter$.subscribe(f => {
      this.filters = f;
      this.applyFilters();
    });
  }

  ngOnDestroy() {
    if (this.filterSub) this.filterSub.unsubscribe();
  }

  async applyFilters() {
    this.loading = true;
    try {
      const newData = await this.api.calculateResourceReport({
        year: this.filters.year,
        quarter: this.filters.quarter,
        month: this.filters.month
      });
      this.data = newData;
      this.currentPage = 1;
      this.processData();
    } catch (err) {
      console.error('Failed to calculate resource report', err);
    } finally {
      this.loading = false;
    }
  }

  processData() {
    if (!this.data || !this.data.reportData) return;
    
    let report = this.data.reportData;
    this.summary = this.data.summary || {};

    // Search query filter
    if (this.filters.searchQuery) {
      const q = this.filters.searchQuery.toLowerCase();
      report = report.filter((r: any) => 
        r.resourceName.toLowerCase().includes(q) || 
        r.formalName.toLowerCase().includes(q)
      );
    }

    this.data.report = report; // for pagination
    this.updatePagination();
  }

  get totalPages() {
    return this.data?.report ? Math.max(1, Math.ceil(this.data.report.length / this.pageSize)) : 1;
  }

  setPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePagination();
    }
  }

  private updatePagination() {
    if (!this.data?.report) return;
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.paginatedData = this.data.report.slice(start, end);
  }

  exportCSV() {
    if (!this.data || !this.data.report) return;

    const headers = [
      "Resource Name", "Function", "FY-Quarter", "Jira Total Hours", "Required Hours", 
      "Capped Hours", "External", "Internal", "CAAPL", "LND", "Sales", "Leaves", "Bench"
    ];

    let csv = headers.join(',') + '\n';

    this.data.report.forEach((row: any) => {
      const values = [
        row.formalName || row.resourceName, row.function, `${this.filters.year}-${this.filters.quarter}`,
        row.totalJiraHours, row.requiredHours, row.totalCappedHours,
        row.externalHours, row.internalHours, row.caapL_Hours,
        row.lnD_Hours, row.sales_Hours, row.leaves_Hours_Final, row.adjustedBench
      ];
      csv += values.map((v: any) => `"${v}"`).join(',') + '\n';
    });

    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `FinanceOS_Resource_Matrix_${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
  }

  toggleSidebar() {
    const event = new CustomEvent('toggleSidebar');
    window.dispatchEvent(event);
  }
}
