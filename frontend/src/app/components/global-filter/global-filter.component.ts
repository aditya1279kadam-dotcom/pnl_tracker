import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FilterService, GlobalFilter } from '../../services/filter.service';
import { FinanceApiService } from '../../services/finance-api.service';

@Component({
  selector: 'app-global-filter',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="filter-bar">
      <div class="filter-group">
        <label>Financial Year</label>
        <select [(ngModel)]="localFilter.year" (change)="emitChange()">
          <option value="All">All Time</option>
          <option value="FY25">FY 2024-25</option>
          <option value="FY26">FY 2025-26</option>
        </select>
      </div>
      <div class="filter-group">
        <label>Quarter</label>
        <select [(ngModel)]="localFilter.quarter" (change)="onQuarterChange()">
          <option value="All">All Quarters</option>
          <option value="Q1">Q1 (Apr-Jun)</option>
          <option value="Q2">Q2 (Jul-Sep)</option>
          <option value="Q3">Q3 (Oct-Dec)</option>
          <option value="Q4">Q4 (Jan-Mar)</option>
        </select>
      </div>
      <div class="filter-group">
        <label>Month</label>
        <select [(ngModel)]="localFilter.month" (change)="emitChange()">
          <option value="All">All Months</option>
          <option *ngFor="let m of availableMonths" [value]="m">{{m}}</option>
        </select>
      </div>
      <div class="filter-group">
        <label>Project Category</label>
        <select [(ngModel)]="localFilter.projectCategory" (change)="emitChange()" multiple style="height: 38px;">
          <option *ngFor="let cat of categories" [value]="cat">{{cat}}</option>
        </select>
      </div>
      <div class="filter-group search-group" style="flex: 1;">
        <label>Search Project / Resource</label>
        <div style="position: relative;">
          <input type="text" [(ngModel)]="localFilter.searchQuery" (input)="emitChange()" placeholder="Search..." class="search-input">
        </div>
      </div>
      <div style="display: flex; gap: 8px; margin-top: auto; padding-bottom: 4px;">
        <button (click)="resetFilters()" class="btn-ghost" style="padding: 8px 12px; height: 38px;">Reset</button>
      </div>
    </div>
  `,
  styles: [`
    .filter-bar {
      display: flex;
      flex-wrap: wrap;
      gap: 20px;
      padding: 20px 24px;
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid var(--border);
      border-radius: 16px;
      margin-bottom: 24px;
      align-items: flex-end;
    }
    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .filter-group label {
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    select, .search-input {
      background: var(--bg-card);
      border: 1px solid var(--border);
      color: var(--text-bright);
      padding: 8px 12px;
      border-radius: 8px;
      outline: none;
      font-weight: 500;
      min-width: 140px;
    }
    select:focus, .search-input:focus {
      border-color: #818cf8;
      box-shadow: 0 0 0 2px rgba(129, 140, 248, 0.1);
    }
    .search-input {
      width: 100%;
    }
  `]
})
export class GlobalFilterComponent implements OnInit {
  localFilter: GlobalFilter = {
    year: 'All',
    quarter: 'All',
    month: 'All',
    projectCategory: [],
    searchQuery: ''
  };

  categories: string[] = ['Implementation', 'Support', 'Consulting', 'CR', 'Valuation'];
  availableMonths: string[] = [];

  constructor(
    private filterService: FilterService,
    private api: FinanceApiService
  ) {}

  async ngOnInit() {
    this.localFilter = { ...this.filterService.currentFilter };
    await this.fetchMetadata();
  }

  async fetchMetadata() {
    try {
      const meta = await this.api.getMetadata();
      if (meta && meta.categories) {
        this.categories = meta.categories;
      }
    } catch (e) {
      console.error('Failed to fetch metadata', e);
    }
  }

  onQuarterChange() {
    const qMap: any = {
      'Q1': ['Apr', 'May', 'Jun'],
      'Q2': ['Jul', 'Aug', 'Sep'],
      'Q3': ['Oct', 'Nov', 'Dec'],
      'Q4': ['Jan', 'Feb', 'Mar']
    };
    this.availableMonths = qMap[this.localFilter.quarter] || [];
    this.localFilter.month = 'All';
    this.emitChange();
  }

  emitChange() {
    this.filterService.updateFilter(this.localFilter);
  }

  resetFilters() {
    this.localFilter = {
      year: 'All',
      quarter: 'All',
      month: 'All',
      projectCategory: [],
      searchQuery: ''
    };
    this.availableMonths = [];
    this.emitChange();
  }
}
