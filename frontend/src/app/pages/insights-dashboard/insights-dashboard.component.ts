import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../components/header/header.component';
import { FinanceApiService } from '../../services/finance-api.service';
import { DataProcessingService } from '../../services/data-processing.service';
import { FilterService } from '../../services/filter.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-insights-dashboard',
  standalone: true,
  imports: [CommonModule, HeaderComponent],
  template: `
    <app-header title="Actionable Insights" subtitle="Financial Efficiency & Overhead Intelligence" (toggleSidebar)="toggleSidebar()"></app-header>

    <div *ngIf="loading" style="padding: 40px; text-align: center;">
      <div class="loader">Synthesizing Insights...</div>
    </div>

    <ng-container *ngIf="!loading && data">
      <div class="kpi-row">
          <div class="kpi-panel">
              <div class="tag">Direct Portfolio Cost</div>
              <div class="amount">{{ dataProc.formatCurrency(summary.totalFullyLoaded - summary.totalPeopleOverhead - summary.totalManualOverhead) }}</div>
          </div>
          <div class="kpi-panel">
              <div class="tag">Total People Overhead</div>
              <div class="amount" style="color: #818cf8;">{{ dataProc.formatCurrency(summary.totalPeopleOverhead) }}</div>
          </div>
          <div class="kpi-panel">
              <div class="tag">Total Manual Overhead</div>
              <div class="amount" style="color: #f472b6;">{{ dataProc.formatCurrency(summary.totalManualOverhead) }}</div>
          </div>
          <div class="kpi-panel">
              <div class="tag">Efficiency Ratio</div>
              <div class="amount">{{ ((1 - (summary.totalManualOverhead + summary.totalPeopleOverhead) / summary.totalRevenue) * 100).toFixed(1) }}%</div>
          </div>
      </div>

      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 24px; margin-top: 32px;">
          <!-- Overhead Distribution Card -->
          <div class="data-table-container" style="background: white; border-radius: 20px; padding: 24px;">
              <h3 style="margin-top: 0; font-weight: 800; border-bottom: 1px solid #f1f5f9; padding-bottom: 16px;">Overhead Pool Breakdown</h3>
              
              <div style="display: flex; flex-direction: column; gap: 24px; padding-top: 20px;">
                  <div class="overhead-item">
                      <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                          <span style="font-weight: 600;">People (Non-Tech/Product)</span>
                          <span style="font-weight: 800;">{{ dataProc.formatCurrency(data.peopleOverhead) }}</span>
                      </div>
                      <div class="progress-bar"><div class="fill" [style.width.%]="getPercent(data.peopleOverhead)" style="background: #818cf8;"></div></div>
                  </div>
                  
                  <div class="overhead-item">
                      <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                          <span style="font-weight: 600;">Manual: HR Overhead</span>
                          <span style="font-weight: 800;">{{ dataProc.formatCurrency(data.manualOverhead_HR) }}</span>
                      </div>
                      <div class="progress-bar"><div class="fill" [style.width.%]="getPercent(data.manualOverhead_HR)" style="background: #fbbf24;"></div></div>
                  </div>

                  <div class="overhead-item">
                      <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                          <span style="font-weight: 600;">Manual: Infra Cost</span>
                          <span style="font-weight: 800;">{{ dataProc.formatCurrency(data.manualOverhead_Infra) }}</span>
                      </div>
                      <div class="progress-bar"><div class="fill" [style.width.%]="getPercent(data.manualOverhead_Infra)" style="background: #34d399;"></div></div>
                  </div>

                  <div class="overhead-item">
                      <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                          <span style="font-weight: 600;">Manual: OPE Cost</span>
                          <span style="font-weight: 800;">{{ dataProc.formatCurrency(data.manualOverhead_OPE) }}</span>
                      </div>
                      <div class="progress-bar"><div class="fill" [style.width.%]="getPercent(data.manualOverhead_OPE)" style="background: #f87171;"></div></div>
                  </div>

                  <div class="overhead-item">
                      <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                          <span style="font-weight: 600;">Manual: Commission</span>
                          <span style="font-weight: 800;">{{ dataProc.formatCurrency(data.manualOverhead_Commission) }}</span>
                      </div>
                      <div class="progress-bar"><div class="fill" [style.width.%]="getPercent(data.manualOverhead_Commission)" style="background: #a78bfa;"></div></div>
                  </div>
              </div>
          </div>

          <!-- Summary Stats -->
          <div class="data-table-container" style="background: white; border-radius: 20px; padding: 24px;">
              <h3 style="margin-top: 0; font-weight: 800; border-bottom: 1px solid #f1f5f9; padding-bottom: 16px;">Portfolio Health</h3>
              <div style="padding-top: 20px;">
                  <div style="background: #f8fafc; padding: 24px; border-radius: 16px; margin-bottom: 20px;">
                      <div style="color: var(--text-muted); font-size: 0.875rem; font-weight: 600; margin-bottom: 8px;">Total Fully Loaded Portfolio Cost</div>
                      <div style="font-size: 2rem; font-weight: 900; color: var(--primary);">{{ dataProc.formatCurrency(summary.totalFullyLoaded) }}</div>
                  </div>
                  
                  <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                      <div style="background: #fdf2f8; padding: 20px; border-radius: 16px;">
                          <div style="color: #db2777; font-size: 0.75rem; font-weight: 700; text-transform: uppercase;">Overhead %</div>
                          <div style="font-size: 1.5rem; font-weight: 800;">{{ (((summary.totalManualOverhead + summary.totalPeopleOverhead) / summary.totalRevenue) * 100).toFixed(1) }}%</div>
                      </div>
                      <div style="background: #f0fdf4; padding: 20px; border-radius: 16px;">
                          <div style="color: #16a34a; font-size: 0.75rem; font-weight: 700; text-transform: uppercase;">Portfolio Margin</div>
                          <div style="font-size: 1.5rem; font-weight: 800;">{{ (summary.avgMargin * 100).toFixed(1) }}%</div>
                      </div>
                  </div>
              </div>
          </div>
      </div>
    </ng-container>
  `,
  styles: [`
    .overhead-item { margin-bottom: 16px; }
    .progress-bar { height: 8px; background: #f1f5f9; border-radius: 4px; overflow: hidden; }
    .fill { height: 100%; transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1); }
  `]
})
export class InsightsDashboardComponent implements OnInit, OnDestroy {
  data: any = null;
  summary: any = {};
  filters: any = {};
  loading = false;
  private filterSub: Subscription | null = null;

  constructor(
    private api: FinanceApiService,
    public dataProc: DataProcessingService,
    private filterService: FilterService
  ) {}

  ngOnInit() {
    this.filterSub = this.filterService.currentFilter$.subscribe(f => {
      this.filters = f;
      this.fetchInsights();
    });
  }

  ngOnDestroy() {
    if (this.filterSub) this.filterSub.unsubscribe();
  }

  async fetchInsights() {
    this.loading = true;
    try {
      const result = await this.api.calculateReport({
        year: this.filters.year,
        quarter: this.filters.quarter,
        month: this.filters.month
      });
      this.data = result;
      this.summary = result.summary || {};
    } catch (e) {
      console.error('Failed to fetch insights', e);
    } finally {
      this.loading = false;
    }
  }

  getPercent(val: number) {
    const total = this.data.manualOverhead_HR + this.data.manualOverhead_Infra + this.data.manualOverhead_OPE + this.data.manualOverhead_Commission + this.data.peopleOverhead;
    return total > 0 ? (val / total) * 100 : 0;
  }

  toggleSidebar() {
    window.dispatchEvent(new CustomEvent('toggleSidebar'));
  }
}
