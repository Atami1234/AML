import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Transaction, PagedResult } from '../models/models';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {

  constructor(private api: ApiService) { }

  getTransactions(page: number = 1, pageSize: number = 20): Observable<PagedResult<Transaction>> {
    return this.api.get<PagedResult<Transaction>>('transactions', { page, pageSize });
  }

  uploadCsv(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.api.postFormData<any>('transactions/upload-csv', formData);
  }

  runDetection(): Observable<any> {
    return this.api.post<any>('detection/run', {});
  }
}
