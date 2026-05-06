import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Alert, PagedResult } from '../models/models';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AlertService {

  constructor(private api: ApiService) { }

  getAlerts(page: number = 1, pageSize: number = 20): Observable<PagedResult<Alert>> {
    return this.api.get<PagedResult<Alert>>('alerts', { page, pageSize });
  }

  getAlert(id: number): Observable<Alert> {
    return this.api.get<Alert>(`alerts/${id}`);
  }

  getHighRiskAlerts(): Observable<Alert[]> {
    return this.api.get<Alert[]>('alerts/high-risk');
  }

  verifyAlert(id: number): Observable<any> {
    return this.api.get<any>(`blockchain/verify-alert/${id}`);
  }

  runDetection(): Observable<any> {
    return this.api.post<any>('detection/run', {});
  }
}
