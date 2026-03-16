import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataProcessingService } from '../../services/data-processing.service';

@Component({
  selector: 'app-category-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card category-card" (click)="onClick.emit(category)" 
         [ngStyle]="{'background': getSoftBgColor(margin), 'border-color': getBorderColor(margin)}"
         style="cursor: pointer; transition: transform 0.2s; border: 1px solid var(--border); border-radius: 20px; padding: 24px;">
      <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px;">
        <div>
          <h3 style="margin: 0; font-size: 1.25rem; font-weight: 800; color: var(--text-bright);">{{ category }}</h3>
          <p style="color: var(--text-muted); font-size: 0.8rem; margin: 4px 0 0 0; font-weight: 600;">PORTFOLIO SEGMENT</p>
        </div>
        <div [style.color]="getIconColor(margin)" style="background: rgba(255,255,255,0.5); padding: 8px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.02);">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
            </svg>
        </div>
      </div>
      
      <div style="margin-bottom: 24px;">
        <div style="font-size: 0.75rem; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.1em; font-weight: 800; margin-bottom: 6px;">Operating Margin</div>
        <div style="font-size: 2.25rem; font-weight: 900; color: var(--text-bright); letter-spacing: -0.02em;">{{ (margin * 100).toFixed(1) }}%</div>
      </div>

      <div style="display: flex; align-items: center; justify-content: space-between; padding-top: 16px; border-top: 1px solid rgba(0,0,0,0.05);">
        <div style="font-size: 0.85rem; color: var(--text-muted); font-weight: 600;">Net Profit</div>
        <div style="font-size: 1rem; font-weight: 800; color: var(--text-bright);">
          {{ dataProc.formatCurrency(profit) }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .category-card:hover {
      transform: translateY(-8px);
      box-shadow: 0 20px 40px rgba(0,0,0,0.08);
      filter: brightness(0.98);
    }
  `]
})
export class CategoryCardComponent {
  @Input() category!: string;
  @Input() profit!: number;
  @Input() margin!: number;

  @Output() onClick = new EventEmitter<string>();

  constructor(public dataProc: DataProcessingService) { }

  getSoftBgColor(margin: number): string {
    if (margin > 0.4) return '#f0fdf4'; // emerald-50
    if (margin > 0.2) return '#fffbeb'; // amber-50
    return '#fef2f2'; // rose-50
  }

  getBorderColor(margin: number): string {
    if (margin > 0.4) return '#bbf7d0'; // emerald-200
    if (margin > 0.2) return '#fde68a'; // amber-200
    return '#fecaca'; // rose-200
  }

  getIconColor(margin: number): string {
    if (margin > 0.4) return '#10b981'; // emerald-500
    if (margin > 0.2) return '#f59e0b'; // amber-500
    return '#ef4444'; // rose-500
  }
}
