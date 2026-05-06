import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AlertService } from '../../services/alert.service';
import { Alert } from '../../models/models';

@Component({
  selector: 'app-alert-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './alert-detail.component.html',
  styleUrl: './alert-detail.component.scss'
})
export class AlertDetailComponent implements OnInit {
  alert: Alert | null = null;
  verificationResult: any = null;
  
  loading = false;
  verifying = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private alertService: AlertService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadAlert(Number(id));
    }
  }

  loadAlert(id: number): void {
    this.loading = true;
    this.alertService.getAlert(id).subscribe({
      next: (res) => {
        this.alert = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load alert details.';
        this.loading = false;
      }
    });
  }

  verifyHash(): void {
    if (!this.alert) return;
    
    this.verifying = true;
    this.alertService.verifyAlert(this.alert.alertId).subscribe({
      next: (res) => {
        this.verificationResult = res;
        this.verifying = false;
      },
      error: (err) => {
        this.error = 'Blockchain verification failed.';
        this.verifying = false;
      }
    });
  }
}
