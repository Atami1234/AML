import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TransactionService } from '../../services/transaction.service';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.scss'
})
export class UploadComponent {
  selectedFile: File | null = null;
  loading = false;
  error = '';
  
  uploadResult: any = null;
  
  // Detection state
  detecting = false;
  detectionResult: any = null;

  constructor(
    private transactionService: TransactionService,
    private alertService: AlertService
  ) {}

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      if (!file.name.toLowerCase().endsWith('.csv')) {
        this.error = 'Please select a valid .csv file.';
        this.selectedFile = null;
      } else {
        this.error = '';
        this.selectedFile = file;
      }
    }
  }

  uploadFile(): void {
    if (!this.selectedFile) return;

    this.loading = true;
    this.error = '';
    this.uploadResult = null;
    this.detectionResult = null;

    this.transactionService.uploadCsv(this.selectedFile).subscribe({
      next: (res) => {
        this.uploadResult = res;
        this.loading = false;
        this.selectedFile = null;
        
        // Step 2: Automatically trigger detection
        this.triggerDetection();
      },
      error: (err) => {
        this.error = err.error?.error || err.error || 'An error occurred during file upload.';
        this.loading = false;
      }
    });
  }

  private triggerDetection(): void {
    this.detecting = true;
    this.alertService.runDetection().subscribe({
      next: (res) => {
        this.detectionResult = res;
        this.detecting = false;
      },
      error: (err) => {
        this.error = 'Upload successful, but automated detection failed to start.';
        this.detecting = false;
      }
    });
  }
}
