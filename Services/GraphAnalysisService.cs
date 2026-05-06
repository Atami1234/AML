using AmlDetectionApi.Models;
using System.Collections.Generic;
using System.Linq;

namespace AmlDetectionApi.Services
{
    public interface IGraphAnalysisService
    {
        List<List<int>> FindCycles(List<Transaction> transactions, int maxLength = 4);
        List<List<int>> FindChains(List<Transaction> transactions, int minLength = 3);
    }

    public class GraphAnalysisService : IGraphAnalysisService
    {
        public List<List<int>> FindCycles(List<Transaction> transactions, int maxLength = 4)
        {
            var adj = BuildAdjacencyList(transactions);
            var cycles = new List<List<int>>();

            foreach (var startNode in adj.Keys)
            {
                DFSFindCycles(startNode, startNode, new List<int> { startNode }, adj, cycles, maxLength);
            }

            // Remove duplicates (rotations of the same cycle)
            return cycles.Select(c => c.OrderBy(x => x).ToList())
                         .GroupBy(c => string.Join(",", c))
                         .Select(g => g.First())
                         .ToList();
        }

        public List<List<int>> FindChains(List<Transaction> transactions, int minLength = 3)
        {
            var adj = BuildAdjacencyList(transactions);
            var chains = new List<List<int>>();

            foreach (var startNode in adj.Keys)
            {
                DFSFindChains(startNode, new List<int> { startNode }, adj, chains, minLength);
            }

            return chains;
        }

        private Dictionary<int, List<int>> BuildAdjacencyList(List<Transaction> transactions)
        {
            var adj = new Dictionary<int, List<int>>();
            foreach (var tx in transactions)
            {
                if (!adj.ContainsKey(tx.FromAccountId)) adj[tx.FromAccountId] = new List<int>();
                if (!adj[tx.FromAccountId].Contains(tx.ToAccountId))
                    adj[tx.FromAccountId].Add(tx.ToAccountId);
            }
            return adj;
        }

        private void DFSFindCycles(int startNode, int currentNode, List<int> path, Dictionary<int, List<int>> adj, List<List<int>> cycles, int maxLength)
        {
            if (path.Count > maxLength) return;

            if (adj.ContainsKey(currentNode))
            {
                foreach (var neighbor in adj[currentNode])
                {
                    if (neighbor == startNode && path.Count >= 3)
                    {
                        cycles.Add(new List<int>(path));
                    }
                    else if (!path.Contains(neighbor))
                    {
                        path.Add(neighbor);
                        DFSFindCycles(startNode, neighbor, path, adj, cycles, maxLength);
                        path.RemoveAt(path.Count - 1);
                    }
                }
            }
        }

        private void DFSFindChains(int currentNode, List<int> path, Dictionary<int, List<int>> adj, List<List<int>> chains, int minLength)
        {
            if (path.Count >= minLength)
            {
                chains.Add(new List<int>(path));
            }

            if (adj.ContainsKey(currentNode))
            {
                foreach (var neighbor in adj[currentNode])
                {
                    if (!path.Contains(neighbor))
                    {
                        path.Add(neighbor);
                        DFSFindChains(neighbor, path, adj, chains, minLength);
                        path.RemoveAt(path.Count - 1);
                    }
                }
            }
        }
    }
}
