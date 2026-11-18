using System.Collections.Generic;
using UnityEngine;

namespace AgentTown
{
    // Half-unit grid, four-way A*: no diagonal corner cutting or physics dependency.
    public sealed class TownNavigation
    {
        public const float Cell = .5f;
        public const int Width = 96, Height = 64;
        readonly bool[,] blocked = new bool[Width, Height];
        public static Vector2 FromPixel(float x, float y) => new Vector2((x - 768) / 32, (512 - y) / 32);
        public static Vector2Int ToCell(Vector2 p) => new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x + 24) / Cell), 0, Width - 1), Mathf.Clamp(Mathf.FloorToInt((p.y + 16) / Cell), 0, Height - 1));
        public static Vector2 Center(Vector2Int c) => new Vector2(-24 + (c.x + .5f) * Cell, -16 + (c.y + .5f) * Cell);
        public void BlockPixels(Rect r)
        {
            for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++)
            {
                Vector2 p = Center(new Vector2Int(x, y));
                Vector2 pixel = new Vector2(p.x * 32 + 768, 512 - p.y * 32);
                if (r.Contains(pixel)) blocked[x, y] = true;
            }
        }
        public bool Walkable(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height && !blocked[c.x, c.y];
        public bool Walkable(Vector2 p) => p.x > -23 && p.x < 23 && p.y > -14 && p.y < 14 && Walkable(ToCell(p));
        public Vector2Int Nearest(Vector2 point)
        {
            var c = ToCell(point);
            if (Walkable(c)) return c;
            for (int radius = 1; radius < 96; radius++)
                for (int dx = -radius; dx <= radius; dx++) for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                    var candidate = c + new Vector2Int(dx, dy);
                    if (Walkable(candidate)) return candidate;
                }
            return ToCell(Vector2.zero);
        }
        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            var origin = Nearest(start); var goal = Nearest(end);
            var open = new List<Vector2Int> { origin };
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var cost = new Dictionary<Vector2Int, int> { [origin] = 0 };
            var visited = new HashSet<Vector2Int>();
            Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            int safety = Width * Height;
            while (open.Count > 0 && safety-- > 0)
            {
                int best = 0, score = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    var p = open[i]; int f = cost[p] + Mathf.Abs(p.x - goal.x) + Mathf.Abs(p.y - goal.y);
                    if (f < score) { best = i; score = f; }
                }
                var current = open[best]; open.RemoveAt(best);
                if (current == goal)
                {
                    var result = new List<Vector2>();
                    while (current != origin) { result.Add(Center(current)); current = previous[current]; }
                    result.Reverse(); return result;
                }
                if (!visited.Add(current)) continue;
                foreach (var n in neighbors)
                {
                    var next = current + n;
                    if (!Walkable(next) || visited.Contains(next)) continue;
                    int g = cost[current] + 1;
                    if (cost.TryGetValue(next, out int old) && old <= g) continue;
                    cost[next] = g; previous[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return new List<Vector2>();
        }
    }
}
