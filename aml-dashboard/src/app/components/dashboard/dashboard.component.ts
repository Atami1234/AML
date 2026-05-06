import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  totalTransactions = 0;
  totalAlerts = 0;
  highRiskAlerts = 0;
  mediumRiskAlerts = 0;

  loading = true;
  detecting = false;
  detectionMessage = '';
  error = '';

  constructor(
    private transactionService: TransactionService,
    private alertService: AlertService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.loading = true;
    this.error = '';
    this.callsCompleted = 0;
    
    // Fetch total transactions
    this.transactionService.getTransactions(1, 1).subscribe({
      next: (res) => {
        this.totalTransactions = res.totalCount;
        this.checkLoading();
      },
      error: (err) => {
        this.error = 'Failed to load transactions count.';
        this.checkLoading();
      }
    });

    // Fetch total alerts
    this.alertService.getAlerts(1, 1).subscribe({
      next: (res) => {
        this.totalAlerts = res.totalCount;
        
        // Fetch high risk alerts
        this.alertService.getHighRiskAlerts().subscribe({
          next: (highRiskRes) => {
            this.highRiskAlerts = highRiskRes.length;
            this.mediumRiskAlerts = this.totalAlerts - this.highRiskAlerts;
            this.checkLoading();
          },
          error: (err) => {
            this.error = 'Failed to load high risk alerts.';
            this.checkLoading();
          }
        });
      },
      error: (err) => {
        this.error = 'Failed to load total alerts.';
        this.checkLoading();
      }
    });
  }

  runDetection(): void {
    this.detecting = true;
    this.detectionMessage = '';
    this.alertService.runDetection().subscribe({
      next: (res) => {
        this.detecting = false;
        this.detectionMessage = res.message;
        this.loadDashboardData(); // Refresh counts
      },
      error: (err) => {
        this.detecting = false;
        this.error = 'Detection failed to run.';
      }
    });
  }

  private callsCompleted = 0;
  private checkLoading() {
    this.callsCompleted++;
    if (this.callsCompleted >= 2) {
      this.loading = false;
    }
  }
}
