export interface Alert {
  alertId: number;
  accountId: number;
  accountNumber: string;
  riskScore: number;
  riskLevel: string;
  status: string;
  createdAt: string;
  reasons: AlertReason[];
  blockchainHash: string;
  blockReference: string;
}

export interface AlertReason {
  ruleName: string;
  description: string;
  score: number;
}

export interface Transaction {
  transactionId: number;
  fromAccountId: number;
  toAccountId: number;
  amount: number;
  currency: string;
  transactionDate: string;
  channel: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AccountMetrics {
  incomingCount: number;
  outgoingCount: number;
  uniqueSenders: number;
  uniqueReceivers: number;
  isFunnelAccount: boolean;
  isCircularPatternDetected: boolean;
}
