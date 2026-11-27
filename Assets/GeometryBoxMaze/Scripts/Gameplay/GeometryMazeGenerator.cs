using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Procedural Recursive-Backtracker labyrinth generator for Geometry Box Tilt Maze.
    /// Produces genuine multi-path winding childhood geometry-box puzzles with merged plastic
    /// walls, BFS-verified solvable Switch Gates, fair corner/dead-end pit holes, collectible
    /// Gold Pins, Eraser Bumpers, and optional dynamic obstacles.
    /// </summary>
    public static class GeometryMazeGenerator
    {
        public struct GenerationConfig
        {
            public int levelNumber;
            public string levelName;
            public string difficultyLabel;
            public string modeLabel;
            public int cols;
            public int rows;
            public int seed;
            public int holeCount;
            public int pinCount;
            public bool includeEraserBumper;
            public bool includeSwitchGate;
            public bool includeMovingSlider;
            public bool includeSpinner;
            public float threeStarTime;
            public float twoStarTime;
            public float timeTrialLimit;
            public Color trayColor;
            public Color outerFrameColor;
            public Color wallColor;
        }

        public static GeometryMazeLevelData CreateRuntimeLevel(GenerationConfig cfg)
        {
            GeometryMazeLevelData data = ScriptableObject.CreateInstance<GeometryMazeLevelData>();
            PopulateLevelData(data, cfg);
            return data;
        }

        public static void PopulateLevelData(GeometryMazeLevelData asset, GenerationConfig cfg)
        {
            if (asset == null) return;

            int cols = Mathf.Clamp(cfg.cols, 4, 8);
            int rows = Mathf.Clamp(cfg.rows, 5, 12);
            Vector2 traySize = new Vector2(7.6f, 11.6f);
            float cellW = traySize.x / cols;
            float cellH = traySize.y / rows;
            float wallThick = cols >= 7 ? 0.16f : 0.20f;
            float ballRadius = cols >= 7 ? 0.23f : (cols >= 6 ? 0.25f : 0.28f);

            asset.levelNumber = cfg.levelNumber;
            asset.levelName = string.IsNullOrEmpty(cfg.levelName) ? $"Labyrinth #{cfg.seed % 1000}" : cfg.levelName;
            asset.difficultyLabel = cfg.difficultyLabel;
            asset.modeLabel = cfg.modeLabel;
            asset.trayInnerSize = traySize;
            asset.trayColor = cfg.trayColor;
            asset.outerFrameColor = cfg.outerFrameColor;
            asset.wallColor = cfg.wallColor;
            asset.threeStarTime = cfg.threeStarTime;
            asset.twoStarTime = cfg.twoStarTime;
            asset.timeTrialLimit = cfg.timeTrialLimit;
            asset.ballRadius = ballRadius;
            asset.walls = new List<MazeWallDefinition>();
            asset.holes = new List<MazeHoleDefinition>();
            asset.obstacles = new List<MazeObstacleDefinition>();
            asset.bonusPins = new List<Vector2>();
            asset.eraserBumpers = new List<MazeBumperDefinition>();
            asset.switchGates = new List<MazeSwitchGateDefinition>();

            System.Random rng = new System.Random(cfg.seed);

            bool[,] vWalls = new bool[Mathf.Max(1, cols - 1), rows];
            bool[,] hWalls = new bool[cols, Mathf.Max(1, rows - 1)];
            for (int c = 0; c < cols - 1; c++)
                for (int r = 0; r < rows; r++)
                    vWalls[c, r] = true;
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows - 1; r++)
                    hWalls[c, r] = true;

            // 1. Recursive Backtracker DFS carving from bottom-left cell
            Vector2Int startCell = new Vector2Int(0, 0);
            CarvePerfectMaze(startCell, cols, rows, vWalls, hWalls, rng);

            // Optional 2x2 central pocket for spinner obstacle (before BFS so connectivity remains valid)
            Vector2Int spinnerCorner = new Vector2Int(-1, -1);
            if (cfg.includeSpinner && cols >= 6 && rows >= 8)
            {
                int sc = cols / 2 - 1;
                int sr = rows / 2 - 1;
                vWalls[sc, sr] = false;
                vWalls[sc, sr + 1] = false;
                hWalls[sc, sr] = false;
                hWalls[sc + 1, sr] = false;
                spinnerCorner = new Vector2Int(sc, sr);
            }

            // 2. Compute BFS distances & choose deep Goal cell in upper rows
            Dictionary<Vector2Int, int> distMap = ComputeBfsDistances(startCell, cols, rows, vWalls, hWalls, out Dictionary<Vector2Int, Vector2Int> parents);
            Vector2Int goalCell = ChooseFarGoalCell(startCell, cols, rows, distMap);

            asset.startPosition = CellCenter(startCell, cols, rows, traySize);
            asset.goalPosition = CellCenter(goalCell, cols, rows, traySize);

            // Track reserved cells so pins/holes/bumpers never collide unfairly
            HashSet<Vector2Int> reservedCells = new HashSet<Vector2Int> { startCell, goalCell };
            if (spinnerCorner.x >= 0)
            {
                reservedCells.Add(spinnerCorner);
                reservedCells.Add(spinnerCorner + Vector2Int.right);
                reservedCells.Add(spinnerCorner + Vector2Int.up);
                reservedCells.Add(spinnerCorner + Vector2Int.one);
            }

            // 3. Optional Switch Gate on the final edge entering goalCell
            if (cfg.includeSwitchGate && parents.TryGetValue(goalCell, out Vector2Int gatePrev))
            {
                // Find all cells still reachable from startCell if edge (gatePrev <-> goalCell) is blocked
                Dictionary<Vector2Int, int> distWithoutGoalEdge = ComputeBfsDistances(
                    startCell, cols, rows, vWalls, hWalls, out _, blockedA: gatePrev, blockedB: goalCell);

                Vector2Int bestSwitchCell = new Vector2Int(-1, -1);
                int bestSwitchDist = -1;
                foreach (var kv in distWithoutGoalEdge)
                {
                    if (reservedCells.Contains(kv.Key)) continue;
                    int deg = CountOpenNeighbors(kv.Key, cols, rows, vWalls, hWalls);
                    int score = kv.Value + (deg == 1 ? 4 : 0); // Prefer rewarding dead ends
                    if (score > bestSwitchDist)
                    {
                        bestSwitchDist = score;
                        bestSwitchCell = kv.Key;
                    }
                }

                if (bestSwitchCell.x >= 0)
                {
                    reservedCells.Add(bestSwitchCell);
                    Vector2 gateMid = (CellCenter(gatePrev, cols, rows, traySize) + CellCenter(goalCell, cols, rows, traySize)) * 0.5f;
                    bool verticalPassage = Mathf.Abs(goalCell.y - gatePrev.y) > 0;
                    Vector2 gateSize = verticalPassage
                        ? new Vector2(Mathf.Max(0.45f, cellW - wallThick * 0.5f), wallThick * 1.15f)
                        : new Vector2(wallThick * 1.15f, Mathf.Max(0.45f, cellH - wallThick * 0.5f));
                    float gateRot = verticalPassage ? 0f : 90f;

                    asset.switchGates.Add(new MazeSwitchGateDefinition(
                        CellCenter(bestSwitchCell, cols, rows, traySize),
                        gateMid,
                        gateSize,
                        gateRot));
                }
            }

            // 4. Optional Rotating Spinner inside 2x2 pocket
            if (spinnerCorner.x >= 0)
            {
                Vector2 arenaCenter = (CellCenter(spinnerCorner, cols, rows, traySize) + CellCenter(spinnerCorner + Vector2Int.one, cols, rows, traySize)) * 0.5f;
                float barLen = Mathf.Min(cellW, cellH) * 1.55f;
                asset.obstacles.Add(MazeObstacleDefinition.CreateSpinner(arenaCenter, new Vector2(barLen, wallThick * 1.15f), 62f));
            }

            // 5. Optional Moving Slider Obstacle across an unused straight open neighbor edge
            if (cfg.includeMovingSlider)
            {
                List<Vector2Int> candidates = new List<Vector2Int>();
                for (int c = 1; c < cols - 1; c++)
                {
                    for (int r = 1; r < rows - 1; r++)
                    {
                        Vector2Int cell = new Vector2Int(c, r);
                        if (reservedCells.Contains(cell)) continue;
                        if (c < cols - 1 && !vWalls[c, r])
                        {
                            candidates.Add(cell);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    Vector2Int chosen = candidates[rng.Next(candidates.Count)];
                    reservedCells.Add(chosen);
                    reservedCells.Add(chosen + Vector2Int.right);
                    Vector2 leftPos = CellCenter(chosen, cols, rows, traySize) + new Vector2(-cellW * 0.12f, 0f);
                    Vector2 rightPos = CellCenter(chosen + Vector2Int.right, cols, rows, traySize) + new Vector2(cellW * 0.12f, 0f);
                    Vector2 mid = (leftPos + rightPos) * 0.5f;
                    asset.obstacles.Add(MazeObstacleDefinition.CreateSlider(
                        new Vector2(leftPos.x, mid.y),
                        new Vector2(rightPos.x, mid.y),
                        new Vector2(cellW * 0.58f, wallThick * 1.05f),
                        1.55f));
                }
            }

            // 6. Optional Eraser Bumper tucked gently in open dead-end pocket edges
            if (cfg.includeEraserBumper)
            {
                List<Vector2Int> deadEnds = CollectDeadEnds(cols, rows, vWalls, hWalls, reservedCells);
                if (deadEnds.Count > 0)
                {
                    Vector2Int eCell = deadEnds[rng.Next(deadEnds.Count)];
                    reservedCells.Add(eCell);
                    Vector2 openDir = FindSingleOpenExitDirection(eCell, cols, rows, vWalls, hWalls);
                    Vector2 cPos = CellCenter(eCell, cols, rows, traySize) - openDir * (Mathf.Min(cellW, cellH) * 0.14f);
                    asset.eraserBumpers.Add(new MazeBumperDefinition(
                        cPos,
                        new Vector2(Mathf.Min(cellW, cellH) * 0.62f, wallThick * 1.45f),
                        rng.Next(2) == 0 ? 28f : -28f,
                        8.2f));
                }
            }

            // 7. Merge & Emit Continuous Molded Plastic Walls
            EmitMergedWalls(cols, rows, traySize, wallThick, vWalls, hWalls, asset.walls);

            // 8. Place Fair, Well-Spaced Pit Holes (mostly tucked into dead-ends / off-center side bends)
            int maxHoles = Mathf.Clamp(cfg.holeCount, 0, 5);
            if (maxHoles > 0)
            {
                float holeR = Mathf.Clamp(Mathf.Min(cellW, cellH) * 0.25f, 0.24f, 0.34f);
                List<Vector2Int> holePool = BuildOrderedHoleCandidates(cols, rows, vWalls, hWalls, distMap, reservedCells);
                for (int i = 0; i < holePool.Count && asset.holes.Count < maxHoles; i++)
                {
                    Vector2Int hCell = holePool[i];
                    if (HasNeighborHole(hCell, asset.holes, traySize, cols, rows)) continue;

                    reservedCells.Add(hCell);
                    Vector2 openDir = FindSingleOpenExitDirection(hCell, cols, rows, vWalls, hWalls);
                    // Tuck slightly away from exit when in a dead-end so ball only drops if steered in
                    Vector2 holeCenter = CellCenter(hCell, cols, rows, traySize) - openDir * (Mathf.Min(cellW, cellH) * 0.16f);
                    asset.holes.Add(new MazeHoleDefinition(holeCenter, holeR));
                }
            }

            // 9. Place Collectible Brass Pins (reward exploring off-branch corridors)
            int targetPins = Mathf.Clamp(cfg.pinCount, 0, 5);
            if (targetPins > 0)
            {
                List<Vector2Int> pinPool = new List<Vector2Int>();
                foreach (var kv in distMap)
                {
                    if (!reservedCells.Contains(kv.Key) && kv.Value >= 2)
                    {
                        pinPool.Add(kv.Key);
                    }
                }
                pinPool.Sort((a, b) =>
                {
                    int da = CountOpenNeighbors(a, cols, rows, vWalls, hWalls);
                    int db = CountOpenNeighbors(b, cols, rows, vWalls, hWalls);
                    int cmp = da.CompareTo(db);
                    return cmp != 0 ? cmp : distMap[b].CompareTo(distMap[a]);
                });

                for (int i = 0; i < pinPool.Count && asset.bonusPins.Count < targetPins; i++)
                {
                    Vector2 pos = CellCenter(pinPool[i], cols, rows, traySize);
                    bool tooClose = false;
                    for (int p = 0; p < asset.bonusPins.Count; p++)
                    {
                        if (Vector2.Distance(pos, asset.bonusPins[p]) < 2.2f)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;
                    reservedCells.Add(pinPool[i]);
                    asset.bonusPins.Add(pos);
                }
            }
        }

        private static void CarvePerfectMaze(Vector2Int start, int cols, int rows, bool[,] vWalls, bool[,] hWalls, System.Random rng)
        {
            bool[,] visited = new bool[cols, rows];
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            visited[start.x, start.y] = true;
            stack.Push(start);

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            List<int> order = new List<int> { 0, 1, 2, 3 };

            while (stack.Count > 0)
            {
                Vector2Int cur = stack.Peek();
                // Fisher-Yates shuffle
                for (int i = order.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                bool carved = false;
                for (int i = 0; i < order.Count; i++)
                {
                    Vector2Int next = cur + dirs[order[i]];
                    if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows || visited[next.x, next.y]) continue;

                    RemoveWallBetween(cur, next, vWalls, hWalls);
                    visited[next.x, next.y] = true;
                    stack.Push(next);
                    carved = true;
                    break;
                }

                if (!carved) stack.Pop();
            }
        }

        private static void RemoveWallBetween(Vector2Int a, Vector2Int b, bool[,] vWalls, bool[,] hWalls)
        {
            if (b.x == a.x + 1) vWalls[a.x, a.y] = false;
            else if (b.x == a.x - 1) vWalls[b.x, b.y] = false;
            else if (b.y == a.y + 1) hWalls[a.x, a.y] = false;
            else if (b.y == a.y - 1) hWalls[b.x, b.y] = false;
        }

        private static bool IsPassageOpen(Vector2Int a, Vector2Int b, int cols, int rows, bool[,] vWalls, bool[,] hWalls)
        {
            if (b.x < 0 || b.x >= cols || b.y < 0 || b.y >= rows) return false;
            if (b.x == a.x + 1) return !vWalls[a.x, a.y];
            if (b.x == a.x - 1) return !vWalls[b.x, b.y];
            if (b.y == a.y + 1) return !hWalls[a.x, a.y];
            if (b.y == a.y - 1) return !hWalls[b.x, b.y];
            return false;
        }

        private static Dictionary<Vector2Int, int> ComputeBfsDistances(
            Vector2Int start,
            int cols,
            int rows,
            bool[,] vWalls,
            bool[,] hWalls,
            out Dictionary<Vector2Int, Vector2Int> parents,
            Vector2Int? blockedA = null,
            Vector2Int? blockedB = null)
        {
            Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();
            parents = new Dictionary<Vector2Int, Vector2Int>();
            Queue<Vector2Int> q = new Queue<Vector2Int>();

            dist[start] = 0;
            q.Enqueue(start);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            while (q.Count > 0)
            {
                Vector2Int cur = q.Dequeue();
                int curDist = dist[cur];

                for (int i = 0; i < dirs.Length; i++)
                {
                    Vector2Int nxt = cur + dirs[i];
                    if (!IsPassageOpen(cur, nxt, cols, rows, vWalls, hWalls)) continue;
                    if (blockedA.HasValue && blockedB.HasValue &&
                        ((cur == blockedA.Value && nxt == blockedB.Value) || (cur == blockedB.Value && nxt == blockedA.Value)))
                    {
                        continue;
                    }
                    if (dist.ContainsKey(nxt)) continue;

                    dist[nxt] = curDist + 1;
                    parents[nxt] = cur;
                    q.Enqueue(nxt);
                }
            }
            return dist;
        }

        private static Vector2Int ChooseFarGoalCell(Vector2Int start, int cols, int rows, Dictionary<Vector2Int, int> distMap)
        {
            Vector2Int bestUpper = new Vector2Int(cols - 1, rows - 1);
            int bestUpperDist = -1;
            Vector2Int bestAny = bestUpper;
            int bestAnyDist = -1;

            foreach (var kv in distMap)
            {
                if (kv.Key == start) continue;
                if (kv.Value > bestAnyDist)
                {
                    bestAnyDist = kv.Value;
                    bestAny = kv.Key;
                }
                if (kv.Key.y >= rows - 2 && kv.Value > bestUpperDist)
                {
                    bestUpperDist = kv.Value;
                    bestUpper = kv.Key;
                }
            }
            return bestUpperDist > 0 ? bestUpper : bestAny;
        }

        private static int CountOpenNeighbors(Vector2Int c, int cols, int rows, bool[,] vWalls, bool[,] hWalls)
        {
            int count = 0;
            if (IsPassageOpen(c, c + Vector2Int.up, cols, rows, vWalls, hWalls)) count++;
            if (IsPassageOpen(c, c + Vector2Int.right, cols, rows, vWalls, hWalls)) count++;
            if (IsPassageOpen(c, c + Vector2Int.down, cols, rows, vWalls, hWalls)) count++;
            if (IsPassageOpen(c, c + Vector2Int.left, cols, rows, vWalls, hWalls)) count++;
            return count;
        }

        private static Vector2 FindSingleOpenExitDirection(Vector2Int c, int cols, int rows, bool[,] vWalls, bool[,] hWalls)
        {
            if (IsPassageOpen(c, c + Vector2Int.up, cols, rows, vWalls, hWalls)) return Vector2.up;
            if (IsPassageOpen(c, c + Vector2Int.right, cols, rows, vWalls, hWalls)) return Vector2.right;
            if (IsPassageOpen(c, c + Vector2Int.down, cols, rows, vWalls, hWalls)) return Vector2.down;
            if (IsPassageOpen(c, c + Vector2Int.left, cols, rows, vWalls, hWalls)) return Vector2.left;
            return Vector2.zero;
        }

        private static List<Vector2Int> CollectDeadEnds(int cols, int rows, bool[,] vWalls, bool[,] hWalls, HashSet<Vector2Int> reserved)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    Vector2Int cell = new Vector2Int(c, r);
                    if (reserved.Contains(cell)) continue;
                    if (CountOpenNeighbors(cell, cols, rows, vWalls, hWalls) == 1)
                    {
                        result.Add(cell);
                    }
                }
            }
            return result;
        }

        private static List<Vector2Int> BuildOrderedHoleCandidates(
            int cols, int rows, bool[,] vWalls, bool[,] hWalls,
            Dictionary<Vector2Int, int> distMap, HashSet<Vector2Int> reserved)
        {
            List<Vector2Int> list = CollectDeadEnds(cols, rows, vWalls, hWalls, reserved);
            // Fallback to T-junctions if fewer dead ends than desired holes
            if (list.Count < 5)
            {
                for (int c = 0; c < cols; c++)
                {
                    for (int r = 0; r < rows; r++)
                    {
                        Vector2Int cell = new Vector2Int(c, r);
                        if (reserved.Contains(cell) || list.Contains(cell)) continue;
                        if (CountOpenNeighbors(cell, cols, rows, vWalls, hWalls) == 3 && distMap.TryGetValue(cell, out int d) && d >= 3)
                        {
                            list.Add(cell);
                        }
                    }
                }
            }
            return list;
        }

        private static bool HasNeighborHole(Vector2Int candidate, List<MazeHoleDefinition> existing, Vector2 traySize, int cols, int rows)
        {
            Vector2 pos = CellCenter(candidate, cols, rows, traySize);
            float minGap = Mathf.Max(traySize.x / cols, traySize.y / rows) * 1.35f;
            for (int i = 0; i < existing.Count; i++)
            {
                if (Vector2.Distance(pos, existing[i].position) < minGap) return true;
            }
            return false;
        }

        private static Vector2 CellCenter(Vector2Int cell, int cols, int rows, Vector2 traySize)
        {
            float cellW = traySize.x / cols;
            float cellH = traySize.y / rows;
            float x = -traySize.x * 0.5f + (cell.x + 0.5f) * cellW;
            float y = -traySize.y * 0.5f + (cell.y + 0.5f) * cellH;
            return new Vector2(x, y);
        }

        private static void EmitMergedWalls(int cols, int rows, Vector2 traySize, float thick, bool[,] vWalls, bool[,] hWalls, List<MazeWallDefinition> outWalls)
        {
            float cellW = traySize.x / cols;
            float cellH = traySize.y / rows;
            float left = -traySize.x * 0.5f;
            float bottom = -traySize.y * 0.5f;

            // Merge consecutive vertical internal segments along each column seam
            for (int c = 0; c < cols - 1; c++)
            {
                float x = left + (c + 1) * cellW;
                int runStart = -1;
                for (int r = 0; r <= rows; r++)
                {
                    bool active = r < rows && vWalls[c, r];
                    if (active && runStart < 0)
                    {
                        runStart = r;
                    }
                    else if (!active && runStart >= 0)
                    {
                        int span = r - runStart;
                        float centerY = bottom + (runStart + span * 0.5f) * cellH;
                        float height = span * cellH + thick;
                        outWalls.Add(new MazeWallDefinition(new Vector2(x, centerY), new Vector2(thick, height)));
                        runStart = -1;
                    }
                }
            }

            // Merge consecutive horizontal internal segments along each row seam
            for (int r = 0; r < rows - 1; r++)
            {
                float y = bottom + (r + 1) * cellH;
                int runStart = -1;
                for (int c = 0; c <= cols; c++)
                {
                    bool active = c < cols && hWalls[c, r];
                    if (active && runStart < 0)
                    {
                        runStart = c;
                    }
                    else if (!active && runStart >= 0)
                    {
                        int span = c - runStart;
                        float centerX = left + (runStart + span * 0.5f) * cellW;
                        float width = span * cellW + thick;
                        outWalls.Add(new MazeWallDefinition(new Vector2(centerX, y), new Vector2(width, thick)));
                        runStart = -1;
                    }
                }
            }
        }
    }
}
