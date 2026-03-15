import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface GlobalFilter {
  year: string;
  quarter: string;
  month: string;
  projectCategory: string[];
  searchQuery: string;
}

@Injectable({
  providedIn: 'root'
})
export class FilterService {
  private initialState: GlobalFilter = {
    year: 'All',
    quarter: 'All',
    month: 'All',
    projectCategory: [],
    searchQuery: ''
  };

  private filterSubject = new BehaviorSubject<GlobalFilter>(this.initialState);
  currentFilter$ = this.filterSubject.asObservable();

  updateFilter(filter: Partial<GlobalFilter>) {
    this.filterSubject.next({ ...this.filterSubject.value, ...filter });
  }

  get currentFilter(): GlobalFilter {
    return this.filterSubject.value;
  }
}
