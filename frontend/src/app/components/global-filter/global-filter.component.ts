import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
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

      <!-- Custom multi-select dropdown for Project Category -->
      <div class="filter-group" style="position: relative;">
        <label>Project Category</label>
        <button class="multi-select-btn" (click)="toggleDropdown($event)" type="button">
          <span>{{ categoryLabel }}</span>
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none" xmlns="http://www.w3.org/2000/svg"
            [style.transform]="dropdownOpen ? 'rotate(180deg)' : 'rotate(0deg)'"
            style="transition: transform 0.2s ease; flex-shrink: 0;">
            <path d="M2 4L6 8L10 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
        <div class="multi-select-panel" *ngIf="dropdownOpen">
          <label class="checkbox-item">
            <input type="checkbox" [checked]="localFilter.projectCategory.length === 0" (change)="selectAll()">
            <span>All Categories</span>
          </label>
          <div class="divider"></div>
          <label class="checkbox-item" *ngFor="let cat of categories">
            <input type="checkbox" [checked]="localFilter.projectCategory.includes(cat)" (change)="toggleCategory(cat)">
            <span>{{ cat }}</span>
          </label>
        </div>
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
    /* Custom multi-select */
    .multi-select-btn {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      background: var(--bg-card);
      border: 1px solid var(--border);
      color: var(--text-bright);
      padding: 8px 12px;
      border-radius: 8px;
      outline: none;
      font-weight: 500;
      min-width: 160px;
      height: 38px;
      cursor: pointer;
      font-size: inherit;
      text-align: left;
      transition: border-color 0.15s;
    }
    .multi-select-btn:hover, .multi-select-btn:focus {
      border-color: #818cf8;
      box-shadow: 0 0 0 2px rgba(129, 140, 248, 0.1);
    }
    .multi-select-panel {
      position: absolute;
      top: calc(100% + 6px);
      left: 0;
      z-index: 200;
      background: var(--bg-card, #1e1e2e);
      border: 1px solid var(--border);
      border-radius: 10px;
      min-width: 180px;
      padding: 6px 0;
      box-shadow: 0 8px 24px rgba(0,0,0,0.4);
    }
    .checkbox-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 14px;
      cursor: pointer;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--text-bright);
      text-transform: none;
      letter-spacing: 0;
      transition: background 0.12s;
    }
    .checkbox-item:hover {
      background: rgba(129, 140, 248, 0.08);
    }
    .checkbox-item input[type="checkbox"] {
      accent-color: #818cf8;
      width: 14px;
      height: 14px;
      cursor: pointer;
    }
    .divider {
      border-top: 1px solid var(--border);
      margin: 4px 0;
    }
  `]
})
export class GlobalFilterComponent implements OnInit, OnDestroy {
  localFilter: GlobalFilter = {
    year: 'All',
    quarter: 'All',
    month: 'All',
    projectCategory: [],
    searchQuery: ''
  };

  categories: string[] = ['Implementation', 'Support', 'Consulting', 'CR', 'Valuation'];
  availableMonths: string[] = [];
  dropdownOpen = false;

  get categoryLabel(): string {
    const selected = this.localFilter.projectCategory;
    if (!selected || selected.length === 0) return 'All Categories';
    if (selected.length === 1) return selected[0];
    return `${selected.length} selected`;
  }

  constructor(
    private filterService: FilterService,
    private api: FinanceApiService
  ) {}

  async ngOnInit() {
    this.localFilter = { ...this.filterService.currentFilter };
    await this.fetchMetadata();
  }

  ngOnDestroy() {}

  @HostListener('document:click')
  onDocumentClick() {
    this.dropdownOpen = false;
  }

  toggleDropdown(event: MouseEvent) {
    event.stopPropagation();
    this.dropdownOpen = !this.dropdownOpen;
  }

  toggleCategory(cat: string) {
    const idx = this.localFilter.projectCategory.indexOf(cat);
    if (idx === -1) {
      this.localFilter.projectCategory = [...this.localFilter.projectCategory, cat];
    } else {
      this.localFilter.projectCategory = this.localFilter.projectCategory.filter(c => c !== cat);
    }
    this.emitChange();
  }

  selectAll() {
    this.localFilter.projectCategory = [];
    this.emitChange();
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
      'Q1': ['April', 'May', 'June'],
      'Q2': ['July', 'August', 'September'],
      'Q3': ['October', 'November', 'December'],
      'Q4': ['January', 'February', 'March']
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
    this.dropdownOpen = false;
    this.emitChange();
  }
}
