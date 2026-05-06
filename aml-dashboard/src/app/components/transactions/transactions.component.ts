import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';
import { Transaction } from '../../models/models';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss'
})
export class TransactionsComponent implements OnInit {
  transactions: Transaction[] = [];
  page = 1;
  pageSize = 20;
  totalPages = 1;
  
  loading = false;
  error = '';

  constructor(private transactionService: TransactionService) {}

  ngOnInit(): void {
    this.loadTransactions();
  }

  loadTransactions(): void {
    this.loading = true;
    this.error = '';
    this.transactionService.getTransactions(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.transactions = res.items;
        this.totalPages = res.totalPages;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load transactions.';
        this.loading = false;
      }
    });
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadTransactions();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadTransactions();
    }
  }
}
