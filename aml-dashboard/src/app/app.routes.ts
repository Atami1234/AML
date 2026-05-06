import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TransactionsComponent } from './components/transactions/transactions.component';
import { UploadComponent } from './components/upload/upload.component';
import { AlertsComponent } from './components/alerts/alerts.component';
import { AlertDetailComponent } from './components/alert-detail/alert-detail.component';
import { NetworkComponent } from './components/network/network.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'transactions', component: TransactionsComponent },
  { path: 'upload', component: UploadComponent },
  { path: 'alerts', component: AlertsComponent },
  { path: 'alerts/:id', component: AlertDetailComponent },
  { path: 'network', component: NetworkComponent },
  { path: '**', redirectTo: 'dashboard' }
];
