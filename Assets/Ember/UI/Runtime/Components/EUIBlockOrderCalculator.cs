// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections.Generic;

using Ember.Basic;

using UnityEngine;

namespace Ember.UI
{
    public struct EUIBlockOrderConfig
    {
        public EUIBlockDirection Direction;
        public int BlocksPerGroup;
        public EUIDiamondMode DiamondMode;
        public EUISpiralMode SpiralMode;
        public EUIBlockDirection SpiralCorner;
        public bool LinesHorizontal;
        public bool TeethHorizontal;

        public static EUIBlockOrderConfig Default => new()
        {
            Direction = EUIBlockDirection.Top,
            BlocksPerGroup = 1,
            DiamondMode = EUIDiamondMode.Outward,
            SpiralMode = EUISpiralMode.Clockwise,
            SpiralCorner = EUIBlockDirection.TopLeft,
            LinesHorizontal = true,
            TeethHorizontal = true,
        };
    }

    /// <summary>
    /// 方块排布顺序计算器。将网格位置按排布模式分组，返回有序的组序列。
    /// 算法移植自 TransitionBlocks 的 8 个 TransitionOrder 子类。
    /// </summary>
    public static class EUIBlockOrderCalculator
    {
        [HasGC]
        public static List<List<Vector2Int>> Calculate(
            int columns, int rows, EUIBlockOrderPattern pattern, EUIBlockOrderConfig config)
        {
            return pattern switch
            {
                EUIBlockOrderPattern.AllAtOnce => BuildAllAtOnce(columns, rows),
                EUIBlockOrderPattern.Random => BuildRandom(columns, rows, config),
                EUIBlockOrderPattern.Diamond => BuildDiamond(columns, rows, config),
                EUIBlockOrderPattern.CornerWipe => BuildCornerWipe(columns, rows, config),
                EUIBlockOrderPattern.SideWipe => BuildSideWipe(columns, rows, config),
                EUIBlockOrderPattern.Checkerboard => BuildCheckerboard(columns, rows),
                EUIBlockOrderPattern.Spiral => BuildSpiral(columns, rows, config),
                EUIBlockOrderPattern.Lines => BuildLines(columns, rows, config),
                EUIBlockOrderPattern.Teeth => BuildTeeth(columns, rows, config),
                _ => BuildAllAtOnce(columns, rows),
            };
        }

        private static List<List<Vector2Int>> BuildAllAtOnce(int columns, int rows)
        {
            var group = new List<Vector2Int>(columns * rows);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    group.Add(new Vector2Int(x, y));
            return new List<List<Vector2Int>> { group };
        }

        private static List<List<Vector2Int>> BuildRandom(int columns, int rows, EUIBlockOrderConfig config)
        {
            var all = new List<Vector2Int>(columns * rows);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    all.Add(new Vector2Int(x, y));
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            int perGroup = Mathf.Max(1, config.BlocksPerGroup);
            var result = new List<List<Vector2Int>>();
            for (int i = 0; i < all.Count; i += perGroup)
            {
                int count = Mathf.Min(perGroup, all.Count - i);
                var group = new List<Vector2Int>(count);
                for (int j = 0; j < count; j++) group.Add(all[i + j]);
                result.Add(group);
            }
            return result;
        }

        private static List<List<Vector2Int>> BuildDiamond(int columns, int rows, EUIBlockOrderConfig config)
        {
            var mode = ResolveDiamondMode(config.DiamondMode);
            int cx = columns / 2, cy = rows / 2, maxSide = Mathf.Max(columns, rows);
            var result = new List<List<Vector2Int>>();
            for (int layer = 0; layer < maxSide; layer++)
            {
                int s = mode == EUIDiamondMode.Outward ? layer : maxSide - layer - 1;
                var ring = MakeDiamondRing(s, cx, cy, columns, rows);
                if (ring.Count > 0) result.Add(ring);
            }
            return result;
        }

        private static List<Vector2Int> MakeDiamondRing(int size, int cx, int cy, int cols, int rows)
        {
            var g = new List<Vector2Int>();
            if (size == 0) { if (Ok(cx, cy, cols, rows)) g.Add(new(cx, cy)); return g; }
            int xo = size, yo = 0;
            while (xo >= 0)
            {
                TryAdd(g, cx + xo, cy + yo, cols, rows);
                TryAdd(g, cx - xo, cy + yo, cols, rows);
                TryAdd(g, cx - xo, cy - yo, cols, rows);
                TryAdd(g, cx + xo, cy - yo, cols, rows);
                xo--; yo++;
            }
            return g;
        }

        private static List<List<Vector2Int>> BuildCornerWipe(int columns, int rows, EUIBlockOrderConfig config)
        {
            var result = new List<List<Vector2Int>>();
            var corner = ResolveCorner(config.Direction);
            int total = columns + rows;
            switch (corner)
            {
                case EUIBlockDirection.TopLeft:
                    for (int c = -total; c < total; c++) AddDiag(result, c, 1, 0, rows, 1, true, columns, rows); break;
                case EUIBlockDirection.TopRight:
                    for (int c = total; c >= -total; c--) AddDiag(result, c, -1, 0, rows, 1, false, columns, rows); break;
                case EUIBlockDirection.BottomLeft:
                    for (int c = 0; c < total; c++) AddDiag(result, c, 1, 0, rows, 1, false, columns, rows); break;
                default:
                    for (int c = total; c >= -total; c--) AddDiag(result, c, -1, 0, rows, 1, true, columns, rows); break;
            }
            return result;
        }

        private static void AddDiag(List<List<Vector2Int>> r, int col, int cd, int rs, int re, int rd, bool add, int cols, int rows)
        {
            var g = new List<Vector2Int>();
            for (int row = rs; row != re; row += rd) { int x = add ? col + row : col - row; if (Ok(x, row, cols, rows)) g.Add(new(x, row)); }
            if (g.Count > 0) r.Add(g);
        }

        private static List<List<Vector2Int>> BuildSideWipe(int columns, int rows, EUIBlockOrderConfig config)
        {
            var r = new List<List<Vector2Int>>();
            switch (ResolveSide(config.Direction))
            {
                case EUIBlockDirection.Top: for (int y = rows - 1; y >= 0; y--) r.Add(Row(y, columns)); break;
                case EUIBlockDirection.Bottom: for (int y = 0; y < rows; y++) r.Add(Row(y, columns)); break;
                case EUIBlockDirection.Left: for (int x = 0; x < columns; x++) r.Add(Col(x, rows)); break;
                default: for (int x = columns - 1; x >= 0; x--) r.Add(Col(x, rows)); break;
            }
            return r;
        }

        private static List<Vector2Int> Row(int y, int cols) { var g = new List<Vector2Int>(cols); for (int x = 0; x < cols; x++) g.Add(new(x, y)); return g; }
        private static List<Vector2Int> Col(int x, int rows) { var g = new List<Vector2Int>(rows); for (int y = 0; y < rows; y++) g.Add(new(x, y)); return g; }

        private static List<List<Vector2Int>> BuildCheckerboard(int columns, int rows)
        {
            var odds = new List<Vector2Int>(); var evens = new List<Vector2Int>();
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < rows; y++)
                    ((x % 2 == 0 ? y % 2 == 0 : y % 2 != 0) ? evens : odds).Add(new(x, y));
            var r = new List<List<Vector2Int>>();
            if (odds.Count > 0) r.Add(odds);
            if (evens.Count > 0) r.Add(evens);
            return r;
        }

        private static List<List<Vector2Int>> BuildSpiral(int columns, int rows, EUIBlockOrderConfig config)
        {
            var r = new List<List<Vector2Int>>();
            var mode = ResolveSpiralMode(config.SpiralMode);
            var corner = ResolveCorner(config.SpiralCorner);
            bool[,] v = new bool[rows, columns];
            var (xd, yd) = SpiralDir(corner, mode);
            var (cx, cy) = SpiralStart(corner, columns, rows);
            v[cy, cx] = true; r.Add(new List<Vector2Int> { new(cx, cy) });
            while (true)
            {
                if (!SpiralMove(ref cx, ref cy, xd, yd, columns, rows, v))
                {
                    (xd, yd) = SpiralTurn(xd, yd, mode);
                    if (!SpiralMove(ref cx, ref cy, xd, yd, columns, rows, v)) break;
                }
                r.Add(new List<Vector2Int> { new(cx, cy) });
                v[cy, cx] = true;
            }
            return r;
        }

        private static (int, int) SpiralStart(EUIBlockDirection c, int cols, int rows) => c switch
        {
            EUIBlockDirection.TopLeft => (0, rows - 1), EUIBlockDirection.TopRight => (cols - 1, rows - 1),
            EUIBlockDirection.BottomLeft => (0, 0), _ => (cols - 1, 0),
        };
        private static (int, int) SpiralDir(EUIBlockDirection c, EUISpiralMode m) { bool cw = m == EUISpiralMode.Clockwise; return c switch { EUIBlockDirection.TopLeft => (cw ? 1 : 0, cw ? 0 : -1), EUIBlockDirection.TopRight => (cw ? 0 : -1, cw ? -1 : 0), EUIBlockDirection.BottomLeft => (cw ? 0 : 1, cw ? 1 : 0), _ => (cw ? -1 : 0, cw ? 0 : 1), }; }
        private static bool SpiralMove(ref int cx, ref int cy, int xd, int yd, int cols, int rows, bool[,] v) { int nx = cx + xd, ny = cy + yd; if (Ok(nx, ny, cols, rows) && !v[ny, nx]) { cx = nx; cy = ny; return true; } return false; }
        private static (int, int) SpiralTurn(int xd, int yd, EUISpiralMode m) { bool cw = m == EUISpiralMode.Clockwise; if (xd == 0 && yd == 1) return (cw ? 1 : -1, 0); if (xd == 0 && yd == -1) return (cw ? -1 : 1, 0); if (xd == 1 && yd == 0) return (0, cw ? -1 : 1); return (0, cw ? 1 : -1); }

        private static List<List<Vector2Int>> BuildLines(int columns, int rows, EUIBlockOrderConfig config)
        {
            var r = new List<List<Vector2Int>>();
            var corner = ResolveCorner(config.Direction);

            // 交错扫描：先奇数行/列，后偶数行/列；扫入方向由基准角决定。
            if (config.LinesHorizontal)
            {
                // 水平行：Top 角自上而下，Bottom 角自下而上
                bool topDown = corner == EUIBlockDirection.TopLeft || corner == EUIBlockDirection.TopRight;
                int rs = topDown ? rows - 1 : 0, re = topDown ? -1 : rows, rd = topDown ? -1 : 1;
                for (int parity = 1; parity >= 0; parity--)
                    for (int y = rs; y != re; y += rd)
                        if ((y & 1) == parity)
                        {
                            var g = new List<Vector2Int>(columns);
                            for (int x = 0; x < columns; x++) g.Add(new(x, y));
                            r.Add(g);
                        }
            }
            else
            {
                // 垂直列：Left 角自左而右，Right 角自右而左
                bool leftRight = corner == EUIBlockDirection.TopLeft || corner == EUIBlockDirection.BottomLeft;
                int cs = leftRight ? 0 : columns - 1, ce = leftRight ? columns : -1, cd = leftRight ? 1 : -1;
                for (int parity = 1; parity >= 0; parity--)
                    for (int x = cs; x != ce; x += cd)
                        if ((x & 1) == parity)
                        {
                            var g = new List<Vector2Int>(rows);
                            for (int y = 0; y < rows; y++) g.Add(new(x, y));
                            r.Add(g);
                        }
            }
            return r;
        }

        private static List<List<Vector2Int>> BuildTeeth(int columns, int rows, EUIBlockOrderConfig config)
        {
            var r = new List<List<Vector2Int>>();
            // TeethHorizontal = true 时锯齿线沿水平方向排布（逐行，上下跳动），与 LinesHorizontal 语义一致。
            if (config.TeethHorizontal)
            { for (int y = 0; y < rows; y++) { var g = new List<Vector2Int>(columns); for (int x = 0; x < columns; x++) g.Add(new(x, x % 2 == 0 ? y : rows - 1 - y)); r.Add(g); } }
            else
            { for (int x = 0; x < columns; x++) { var g = new List<Vector2Int>(rows); for (int y = 0; y < rows; y++) g.Add(new(y % 2 == 0 ? x : columns - 1 - x, y)); r.Add(g); } }
            return r;
        }

        [NoGC] private static bool Ok(int x, int y, int c, int r) => (uint)x < (uint)c && (uint)y < (uint)r;
        [NoGC] private static void TryAdd(List<Vector2Int> g, int x, int y, int c, int r) { if (Ok(x, y, c, r)) g.Add(new(x, y)); }

        private static EUIDiamondMode ResolveDiamondMode(EUIDiamondMode m) => m == EUIDiamondMode.Random ? (Random.value < 0.5f ? EUIDiamondMode.Outward : EUIDiamondMode.Inward) : m;
        private static EUISpiralMode ResolveSpiralMode(EUISpiralMode m) => m == EUISpiralMode.Random ? (Random.value < 0.5f ? EUISpiralMode.Clockwise : EUISpiralMode.CounterClockwise) : m;
        private static EUIBlockDirection ResolveSide(EUIBlockDirection d) => d != EUIBlockDirection.Random ? d : (EUIBlockDirection)Random.Range(0, 4);
        private static EUIBlockDirection ResolveCorner(EUIBlockDirection d) => d != EUIBlockDirection.Random ? d : (EUIBlockDirection)(4 + Random.Range(0, 4));
    }
}
