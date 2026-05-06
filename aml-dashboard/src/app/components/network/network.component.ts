import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountService } from '../../services/account.service';
import { TransactionService } from '../../services/transaction.service';
import { AccountMetrics, Transaction } from '../../models/models';
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

  constructor(
    private accountService: AccountService,
    private transactionService: TransactionService
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

    // Fetch all transactions in one request (large pageSize)
    this.transactionService.getTransactions(1, 1000).subscribe({
      next: (res) => {
        this.allTransactions = res.items;
        if (res.items.length === 0) {
          this.error = 'No transactions found in the database.';
          this.loading = false;
          return;
        }
        this.buildAndRender(res.items);
      },
      error: () => {
        this.error = 'Failed to load transactions.';
        this.loading = false;
      }
    });
  }

  // ── Build graph from transaction list ─────────────────
  private buildAndRender(transactions: Transaction[]): void {
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

    // Build vis nodes — size and color driven by degree (centrality)
    const visNodes: any[] = Array.from(accountIds).map(id => {
      const degree    = degreeMap.get(id) || 1;
      const ratio     = degree / this.maxDegree;          // 0 → 1
      const nodeSize  = 10 + ratio * 38;                  // 10–48 px
      const vol       = outVolume.get(id) || 0;

      // Risk classification by degree ratio
      let bg: string, border: string, glow: string, tier: string;
      if (ratio >= 0.7) {
        bg = '#dc2626'; border = '#fca5a5'; glow = 'rgba(220,38,38,0.8)'; tier = 'High Hub';
      } else if (ratio >= 0.35) {
        bg = '#ea580c'; border = '#fdba74'; glow = 'rgba(234,88,12,0.65)'; tier = 'Medium';
      } else if (ratio >= 0.15) {
        bg = '#2563ab'; border = '#93c5fd'; glow = 'rgba(37,99,171,0.55)'; tier = 'Active';
      } else {
        bg = '#475569'; border = '#94a3b8'; glow = 'rgba(71,85,105,0.4)'; tier = 'Peripheral';
      }

      return {
        id,
        label: `${id}`,
        title: [
          `Account #${id}`,
          `Degree: ${degree} connections`,
          `Tier: ${tier}`,
          vol > 0 ? `Volume: SAR ${vol.toLocaleString()}` : ''
        ].filter(Boolean).join('\n'),
        color: {
          background: bg,
          border: border,
          highlight: { background: bg, border: '#ffd700' },
          hover:      { background: bg, border: '#ffd700' }
        },
        size: nodeSize,
        borderWidth: ratio >= 0.7 ? 3 : 2,
        borderWidthSelected: 6,
        shadow: {
          enabled: true,
          color: glow,
          size: ratio >= 0.7 ? 28 : (ratio >= 0.35 ? 18 : 10),
          x: 0, y: 0
        },
        font: {
          color: '#ffffff',
          size: ratio >= 0.5 ? 13 : 10,
          face: 'Inter',
          strokeWidth: 3,
          strokeColor: 'rgba(5,13,20,0.98)'
        },
        // store for click handler
        _degree: degree,
        _tier: tier,
        _volume: vol
      };
    });

    // Build vis edges — thickness by amount
    const amounts   = transactions.map(t => t.amount);
    const maxAmount = Math.max(...amounts);

    const visEdges: any[] = transactions.map((tx, i) => {
      const ratio  = tx.amount / maxAmount;
      const width  = 0.8 + ratio * 3.5;     // 0.8–4.3 px
      const alpha  = 0.25 + ratio * 0.5;    // 0.25–0.75

      // High-value = orange glow, normal = blue
      const color = ratio >= 0.6
        ? `rgba(234,88,12,${alpha})`
        : `rgba(59,130,246,${alpha})`;

      return {
        id: i + 1,
        from: tx.fromAccountId,
        to:   tx.toAccountId,
        label: ratio >= 0.4 ? tx.amount.toLocaleString() : '',   // only label big edges
        color: { color, highlight: '#ffd700', hover: '#ffd700' },
        width,
        selectionWidth: 5,
        hoverWidth: width + 1.5,
        arrows: { to: { enabled: true, scaleFactor: 0.6, type: 'arrow' } },
        smooth: { enabled: true, type: 'continuous', roundness: 0.3 },
        font: {
          color: ratio >= 0.6 ? '#ea580c' : '#64748b',
          size: 10,
          background: 'rgba(5,13,20,0.85)',
          strokeWidth: 0,
          align: 'middle'
        }
      };
    });

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
    switch (this.selectedNode._tier) {
      case 'High Hub': return 'badge-high';
      case 'Medium':   return 'badge-medium';
      case 'Active':   return 'badge-blue';
      default:         return 'badge-muted';
    }
  }

  getNodeTransactions(id: number): { incoming: Transaction[], outgoing: Transaction[] } {
    return {
      incoming: this.allTransactions.filter(t => t.toAccountId === id).slice(0, 5),
      outgoing: this.allTransactions.filter(t => t.fromAccountId === id).slice(0, 5)
    };
  }
}
