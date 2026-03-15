import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../components/header/header.component';
import { FinanceApiService } from '../../services/finance-api.service';
import { FilterService } from '../../services/filter.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-defaulters-report',
  standalone: true,
  imports: [CommonModule, HeaderComponent],
  template: `
    <app-header title="Defaulters List" subtitle="People Governance & Jira Compliance Report" (toggleSidebar)="toggleSidebar()"></app-header>
    
    <div *ngIf="loading" style="padding: 40px; text-align: center;">
      <div class="loader">Loading Governance Report...</div>
    </div>

    <div *ngIf="!loading && (!defaulters || defaulters.length === 0)" style="height: 60vh; display: flex; flex-direction: column; align-items: center; justify-content: center; text-align: center;">
        <div style="background: rgba(16, 185, 129, 0.1); padding: 40px; border-radius: 24px; border: 1px dashed rgba(16, 185, 129, 0.3);">
            <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="1.5" style="margin-bottom: 24px;"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
            <h2 style="font-weight: 800; margin-bottom: 12px; color: #10b981;">100% Compliance</h2>
            <p style="color: var(--text-muted); max-width: 400px;">All resources have completed their logs and have valid IDs for the selected period.</p>
        </div>
    </div>

    <ng-container *ngIf="!loading && defaulters && defaulters.length > 0">
        <div class="kpi-row">
            <div class="kpi-panel" style="border-color: rgba(239, 68, 68, 0.3);">
                <div class="tag" style="color: #f87171;">Non-Compliant Resources</div>
                <div class="amount">{{ defaulters.length }}</div>
                <div class="growth" style="color: #fca5a5;">Manual Correction Required</div>
            </div>
            <div class="kpi-panel">
                <div class="tag">Flag: No Jira ID</div>
                <div class="amount">{{ countFlag('No Jira ID') }}</div>
            </div>
            <div class="kpi-panel">
                <div class="tag">Flag: No logs</div>
                <div class="amount">{{ countFlag('No logs') }}</div>
            </div>
        </div>

        <div style="margin-top: 32px;" class="data-table-container">
            <div style="padding: 24px; border-bottom: 1px solid var(--border); background: white;">
                <h3 style="margin:0; font-weight: 800;">Defaulters Ledger (Sorted by Filled Hours Low to High)</h3>
            </div>

            <div style="overflow-x: auto;">
                <table class="compact-table">
                    <thead>
                        <tr>
                            <th style="text-align: left;">Resource Name</th>
                            <th>FY-Quarter</th>
                            <th>Jira Total Hours</th>
                            <th>Required Hours</th>
                            <th>Missing Hours</th>
                            <th>Reason / Flag</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr *ngFor="let item of defaulters">
                            <td style="font-weight: 700; text-align: left;">{{ item.resourceName }}</td>
                            <td>{{ filters.year }}-{{ filters.quarter }}</td>
                            <td style="font-weight: 600;">{{ item.jiraTotalHours.toFixed(1) }}h</td>
                            <td>{{ item.requiredHours.toFixed(1) }}h</td>
                            <td style="color: #ef4444; font-weight: 600;">{{ item.missingHours.toFixed(1) }}h</td>
                            <td>
                                <span class="badge-pill" [ngClass]="getFlagClass(item.flag)">
                                    {{ item.flag }}
                                </span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </ng-container>
  `,
  styles: [`
    .compact-table {
        width: 100%;
        border-collapse: collapse;
    }
    .compact-table th, .compact-table td {
        padding: 16px 24px;
        text-align: center;
        border-bottom: 1px solid var(--border);
    }
    .compact-table th {
        font-size: 0.75rem;
        text-transform: uppercase;
        color: var(--text-muted);
        letter-spacing: 0.05em;
        background: #f8fafc;
    }
    .badge-pill.bg-red { background: #fee2e2; color: #b91c1c; }
    .badge-pill.bg-orange { background: #ffedd5; color: #9a3412; }
    .badge-pill.bg-amber { background: #fef3c7; color: #92400e; }
  `]
})
export class DefaultersReportComponent implements OnInit, OnDestroy {
  defaulters: any[] = [];
  filters: any = {};
  loading = false;
  private filterSub: Subscription | null = null;

  constructor(
    private api: FinanceApiService,
    private filterService: FilterService
  ) {}

  ngOnInit() {
    this.filterSub = this.filterService.currentFilter$.subscribe(f => {
      this.filters = f;
      this.fetchDefaulters();
    });
  }

  ngOnDestroy() {
    if (this.filterSub) this.filterSub.unsubscribe();
  }

  async fetchDefaulters() {
    this.loading = true;
    try {
      const result = await this.api.calculateResourceReport({
        year: this.filters.year,
        quarter: this.filters.quarter,
        month: this.filters.month
      });
      if (result && result.qcReport) {
        this.defaulters = result.qcReport.defaulters || [];
      }
    } catch (e) {
      console.error('Failed to fetch defaulters', e);
    } finally {
      this.loading = false;
    }
  }

  countFlag(flag: string) {
    return this.defaulters.filter(d => d.flag === flag).length;
  }

  getFlagClass(flag: string) {
    if (flag === 'No Jira ID') return 'bg-orange';
    if (flag === 'No logs') return 'bg-red';
    return 'bg-amber'; // Partial logs
  }

  toggleSidebar() {
    window.dispatchEvent(new CustomEvent('toggleSidebar'));
  }
}
