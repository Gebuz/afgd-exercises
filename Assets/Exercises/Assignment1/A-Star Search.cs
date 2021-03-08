using System;
using System.Collections.Generic;
using UnityEngine;
using AfGD.Execise3;

namespace AfGD.Assignment1
{
    public static class AStarSearch
    {
        // Exercise 3.3 - Implement A* search
        // Explore the graph and fill the _cameFrom_ dictionairy with data using uniform cost search.
        // Similar to Exercise 3.1 PathFinding.ReconstructPath() will use the data in cameFrom  
        // to reconstruct a path between the start node and end node. 
        //
        // Notes:
        //      Use the data structures used in Exercise 3.1 and 3.2
        //
        public static void Execute(Graph graph, Node startPoint, Node endPoint, Dictionary<Node, Node> cameFrom)
        {
            var frontier = new PriorityQueue<Node>();
            frontier.Enqueue(startPoint, 0f);

            var costSoFar = new Dictionary<Node, float>();
            costSoFar.Add(startPoint, 0f);

            cameFrom.Add(startPoint, null);

            var neighbours = new List<Node>(10);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == endPoint)
                    break;

                graph.GetNeighbours(current, neighbours);
                foreach (var next in neighbours)
                {
                    var newCost = costSoFar[current] + graph.GetCost(current, next);

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        var priority = newCost + Heuristic(endPoint, next);
                        frontier.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }
        }

        static float Heuristic(Node from, Node to)
        {
            var fromTo = to.Position - from.Position;
            return (float)Math.Sqrt(fromTo.x * fromTo.x + fromTo.y * fromTo.y);
        }
    }
}