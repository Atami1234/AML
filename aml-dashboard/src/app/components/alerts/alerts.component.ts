import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AlertService } from '../../services/alert.service';
import { Alert } from '../../models/models';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './alerts.component.html',
  styleUrl: './alerts.component.scss'
})
export class AlertsComponent implements OnInit {
  alerts: Alert[] = [];
  page = 1;
  pageSize = 20;
  totalPages = 1;
  
  loading = false;
  error = '';

  get countHigh()   { return this.alerts.filter(a => a.riskLevel === 'High').length; }
  get countMedium() { return this.alerts.filter(a => a.riskLevel === 'Medium').length; }
  get countLow()    { return this.alerts.filter(a => a.riskLevel === 'Low').length; }

  getRiskColor(riskLevel: string): string {
    switch(riskLevel?.toLowerCase()) {
      case 'high': return '#dc2626';
      case 'medium': return '#ea580c';
      case 'low': return '#16a34a';
      default: return '#64748b';
    }
  }

  /** Color based on numeric score thresholds (mirrors backend logic) */
  getRiskScoreColor(score: number): string {
    if (score >= 85.7) return '#dc2626'; // High
    if (score >= 60)   return '#ea580c'; // Medium
    return '#16a34a';                     // Low
  }

  constructor(private alertService: AlertService) {}

  ngOnInit(): void {
    this.loadAlerts();
  }

  loadAlerts(): void {
    this.loading = true;
    this.error = '';
    this.alertService.getAlerts(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.alerts = res.items;
        this.totalPages = res.totalPages;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load alerts.';
        this.loading = false;
      }
    });
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadAlerts();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadAlerts();
    }
  }
}
