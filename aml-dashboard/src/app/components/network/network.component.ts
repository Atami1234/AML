import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountService } from '../../services/account.service';
import { TransactionService } from '../../services/transaction.service';
import { AlertService } from '../../services/alert.service';
import { AccountMetrics, Transaction, Alert } from '../../models/models';
import { forkJoin } from 'rxjs';
import { Network, DataSet, Data, Node, Edge, Options } from 'vis-network/standalone';

@Component({
  selector: 'app-network',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './network.component.html',
  styleUrl: './network.component.scss'
})
export class NetworkComponent implements OnInit, OnDestroy {

  loading = true;
  error = '';
  physicsEnabled = true;
  stabilizing = false;

  // Graph stats
  nodesCount = 0;
  edgesCount = 0;
  maxDegree = 0;
  totalVolume = 0;

  // Selected node
  selectedNode: any = null;
  selectedMetrics: AccountMetrics | null = null;
  metricsLoading = false;

  private network: Network | null = null;
  private allTransactions: Transaction[] = [];
  private allAlerts: Alert[] = [];

  constructor(
    private accountService: AccountService,
    private transactionService: TransactionService,
    private alertService: AlertService
  ) {}

  ngOnInit(): void {
    this.loadFullNetwork();
  }

  ngOnDestroy(): void {
    if (this.network) this.network.destroy();
  }

  // ── Auto-load on open ─────────────────────────────────
  loadFullNetwork(): void {
    this.loading = true;
    this.error = '';
    this.selectedNode = null;

    // Fetch both transactions and alerts
    forkJoin({
      transactions: this.transactionService.getTransactions(1, 1000),
      alerts: this.alertService.getAlerts(1, 1000)
    }).subscribe({
      next: (res) => {
        this.allTransactions = res.transactions.items;
        this.allAlerts = res.alerts.items;
        
        if (this.allTransactions.length === 0) {
          this.error = 'No transactions found in the database.';
          this.loading = false;
          return;
        }
        this.buildAndRender(this.allTransactions, this.allAlerts);
      },
      error: () => {
        this.error = 'Failed to load network data.';
        this.loading = false;
      }
    });
  }

  // ── Build graph from data ─────────────────
  private buildAndRender(transactions: Transaction[], alerts: Alert[]): void {
    // Compute per-account degree (number of edges)
    const degreeMap = new Map<number, number>();
    const outVolume = new Map<number, number>();

    transactions.forEach(tx => {
      degreeMap.set(tx.fromAccountId, (degreeMap.get(tx.fromAccountId) || 0) + 1);
      degreeMap.set(tx.toAccountId,   (degreeMap.get(tx.toAccountId)   || 0) + 1);
      outVolume.set(tx.fromAccountId, (outVolume.get(tx.fromAccountId) || 0) + tx.amount);
    });

    const degrees = Array.from(degreeMap.values());
    this.maxDegree  = Math.max(...degrees);
    this.totalVolume = transactions.reduce((s, t) => s + t.amount, 0);

    // Unique account IDs
    const accountIds = new Set<number>();
    transactions.forEach(tx => { accountIds.add(tx.fromAccountId); accountIds.add(tx.toAccountId); });

    // Build vis nodes — driven by risk alerts
    const visNodes: any[] = Array.from(accountIds).map(id => {
      const degree = degreeMap.get(id) || 1;
      const vol = outVolume.get(id) || 0;
      
      // Find matching alert
      const alert = alerts.find(a => a.accountId === id);
      const riskLevel = alert ? alert.riskLevel : 'None';

      let bg: string, border: string, glow: string, nodeSize: number;
      
      switch (riskLevel) {
        case 'High':
          bg = '#ef4444'; border = '#fca5a5'; glow = 'rgba(239, 68, 68, 0.8)';
          nodeSize = 40;
          break;
        case 'Medium':
          bg = '#f97316'; border = '#fdba74'; glow = 'rgba(249, 115, 22, 0.65)';
          nodeSize = 32;
          break;
        case 'Low': // Though logic says < 60 no alert, if somehow one exists
          bg = '#16a34a'; border = '#86efac'; glow = 'rgba(22, 163, 74, 0.55)';
          nodeSize = 24;
          break;
        case 'None':
          bg = '#3b82f6'; border = '#93c5fd'; glow = 'rgba(59, 130, 246, 0.4)';
          nodeSize = 24;
          break;
        default:
          bg = '#94a3b8'; border = '#cbd5e1'; glow = 'rgba(148, 163, 184, 0.3)';
          nodeSize = 24;
      }

      return {
        id,
        label: `${id}`,
        title: [
          `Account #${id}`,
          `Degree: ${degree} connections`,
          `Risk Level: ${riskLevel}`,
          vol > 0 ? `Volume: SAR ${vol.toLocaleString()}` : ''
        ].filter(Boolean).join('\n'),
        color: {
          background: bg,
          border: border,
          highlight: { background: bg, border: '#ffd700' },
          hover:      { background: bg, border: '#ffd700' }
        },
        size: nodeSize,
        borderWidth: riskLevel === 'High' ? 3 : 2,
        borderWidthSelected: 6,
        shadow: {
          enabled: true,
          color: glow,
          size: riskLevel === 'High' ? 28 : (riskLevel === 'Medium' ? 18 : 10),
          x: 0, y: 0
        },
        font: {
          color: '#ffffff',
          size: nodeSize > 30 ? 13 : 10,
          face: 'Inter',
          strokeWidth: 3,
          strokeColor: 'rgba(5,13,20,0.98)'
        },
        // store for click handler
        _degree: degree,
        _risk: riskLevel,
        _volume: vol
      };
    });

    // Build vis edges — thickness and color by amount
    const visEdges: any[] = transactions.map((tx, i) => ({
      id: i + 1,
      from: tx.fromAccountId,
      to: tx.toAccountId,
      label: `SAR ${tx.amount?.toLocaleString()}`,
      color: {
        color: tx.amount > 50000 ? '#f97316' : '#3b82f6',
        highlight: '#f59e0b'
      },
      width: tx.amount > 50000 ? 3 : 1.5,
      arrows: { to: { enabled: true, scaleFactor: 0.6, type: 'arrow' } },
      smooth: { enabled: true, type: 'curvedCW', roundness: 0.2 },
      font: {
        color: tx.amount > 50000 ? '#ea580c' : '#64748b',
        size: 10,
        background: 'rgba(5,13,20,0.85)',
        strokeWidth: 0,
        align: 'middle'
      }
    }));

    console.log('edges:', visEdges);

    this.nodesCount = visNodes.length;
    this.edgesCount = visEdges.length;

    setTimeout(() => this.renderGraph(visNodes, visEdges), 80);
  }

  private renderGraph(visNodes: any[], visEdges: any[]): void {
    const container = document.getElementById('network-graph');
    if (!container) return;

    this.loading = false;
    this.stabilizing = true;

    const data: Data = {
      nodes: new DataSet<Node>(visNodes),
      edges: new DataSet<Edge>(visEdges)
    };

    // Choose solver based on graph size
    const isLarge = visNodes.length > 100;

    const options: Options = {
      physics: {
        enabled: true,
        solver: isLarge ? 'barnesHut' : 'forceAtlas2Based',
        barnesHut: {
          gravitationalConstant: -12000,
          centralGravity: 0.1,
          springLength: 150,
          springConstant: 0.04,
          damping: 0.09,
          avoidOverlap: 0.8
        },
        forceAtlas2Based: {
          gravitationalConstant: -120,
          centralGravity: 0.005,
          springLength: 200,
          springConstant: 0.02,
          damping: 0.4,
          avoidOverlap: 1.5
        },
        stabilization: {
          enabled: true,
          iterations: isLarge ? 200 : 500,
          updateInterval: 10,
          fit: true
        },
        minVelocity: 0.4,
        maxVelocity: 100,
        timestep: 0.5
      },
      nodes: {
        shape: 'dot',
        borderWidth: 2,
        borderWidthSelected: 6,
        shadow: { enabled: true, size: 12, color: 'rgba(0,0,0,0.7)', x: 0, y: 0 }
      },
      edges: {
        smooth: { enabled: true, type: 'continuous', roundness: 0.3 },
        arrows: { to: { enabled: true, scaleFactor: 0.6, type: 'arrow' } }
      },
      interaction: {
        hover: true,
        hoverConnectedEdges: true,
        selectConnectedEdges: true,
        tooltipDelay: 60,
        zoomView: true,
        dragView: true,
        navigationButtons: false,
        keyboard: false
      },
      layout: {
        randomSeed: 42,
        improvedLayout: !isLarge
      }
    };

    if (this.network) this.network.destroy();
    this.network = new Network(container, data, options);

    this.network.on('stabilizationIterationsDone', () => {
      this.stabilizing = false;
      this.physicsEnabled = false;   // freeze after settle
      this.network!.setOptions({ physics: { enabled: false } });
      this.network!.fit({ animation: { duration: 1000, easingFunction: 'easeInOutQuad' } });
    });

    this.network.on('stabilizationProgress', (params) => {
      // Update stabilization progress
    });

    this.network.on('click', (params) => {
      if (params.nodes.length > 0) {
        const nodeId = params.nodes[0] as number;
        const node   = visNodes.find(n => n.id === nodeId);
        if (node) this.selectNode(node);
      } else {
        this.selectedNode = null;
        this.selectedMetrics = null;
      }
    });

    this.network.on('hoverNode', () => { container.style.cursor = 'pointer'; });
    this.network.on('blurNode',  () => { container.style.cursor = 'default'; });
  }

  selectNode(node: any): void {
    this.selectedNode = node;
    this.selectedMetrics = null;
    this.metricsLoading = true;

    this.accountService.getAccountMetrics(node.id).subscribe({
      next: (res) => { this.selectedMetrics = res; this.metricsLoading = false; },
      error: () => { this.metricsLoading = false; }
    });
  }

  closePanel(): void {
    this.selectedNode = null;
    this.selectedMetrics = null;
    if (this.network) this.network.unselectAll();
  }

  expandNode(): void {
    if (!this.selectedNode) return;
    // Reload but focused on this account's 2nd-degree network
    // Re-run physics and fit to selected node
    if (this.network) {
      this.network.selectNodes([this.selectedNode.id]);
      this.network.focus(this.selectedNode.id, { scale: 1.5, animation: { duration: 700, easingFunction: 'easeInOutQuad' } });
    }
  }

  fitGraph(): void {
    if (this.network) this.network.fit({ animation: { duration: 700, easingFunction: 'easeInOutQuad' } });
  }

  togglePhysics(): void {
    this.physicsEnabled = !this.physicsEnabled;
    if (this.network) this.network.setOptions({ physics: { enabled: this.physicsEnabled } });
  }

  get tierColor(): string {
    if (!this.selectedNode) return '#3b82f6';
    switch (this.selectedNode._tier) {
      case 'High Hub':   return '#dc2626';
      case 'Medium':     return '#ea580c';
      case 'Active':     return '#2563ab';
      default:           return '#475569';
    }
  }

  get tierBadgeClass(): string {
    if (!this.selectedNode) return '';
    switch (this.selectedNode._risk) {
      case 'High':   return 'badge-high';
      case 'Medium': return 'badge-medium';
      case 'Low':    return 'badge-low';
      default:       return 'badge-blue';
    }
  }

  getNodeTransactions(id: number): { incoming: Transaction[], outgoing: Transaction[] } {
    return {
      incoming: this.allTransactions.filter(t => t.toAccountId === id).slice(0, 5),
      outgoing: this.allTransactions.filter(t => t.fromAccountId === id).slice(0, 5)
    };
  }
}
