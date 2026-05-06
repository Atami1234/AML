import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { AccountMetrics } from '../models/models';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AccountService {

  constructor(private api: ApiService) { }

  getAccountMetrics(id: number): Observable<AccountMetrics> {
    return this.api.get<AccountMetrics>(`accounts/${id}/metrics`);
  }

  getAccountNetwork(id: number): Observable<any> {
    return this.api.get<any>(`accounts/${id}/network`);
  }
}
