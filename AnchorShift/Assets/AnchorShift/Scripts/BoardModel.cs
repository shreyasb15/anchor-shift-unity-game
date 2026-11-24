using System;
using System.Collections.Generic;

namespace AnchorShift
{
    // Cell indices run left to right, top to bottom. This class has no Unity dependencies.
    public sealed class BoardModel
    {
        public const int Size = 6;
        public readonly bool[] Walls = new bool[36];
        public readonly int[] Targets = new int[3];
        public readonly int[] Blocks = new int[3];
        public int Anchor { get; private set; } = -1;
        readonly Stack<Snapshot> history = new Stack<Snapshot>();

        struct Snapshot
        {
            public int A, B, C, Anchor;
            public Snapshot(BoardModel board)
            {
                A = board.Blocks[0]; B = board.Blocks[1]; C = board.Blocks[2]; Anchor = board.Anchor;
            }
        }

        public BoardModel(LevelDefinition level)
        {
            if (level == null || level.walls == null || level.walls.Length != Size ||
                level.blocks == null || level.blocks.Length != 3 ||
                level.targets == null || level.targets.Length != 3)
                throw new ArgumentException("Invalid level dimensions");
            for (int y = 0; y < Size; y++)
            {
                if (level.walls[y] == null || level.walls[y].Length != Size)
                    throw new ArgumentException("Invalid wall row");
                for (int x = 0; x < Size; x++) Walls[y * Size + x] = level.walls[y][x] == '#';
            }
            Array.Copy(level.blocks, Blocks, 3);
            Array.Copy(level.targets, Targets, 3);
            var occupied = new HashSet<int>();
            var goals = new HashSet<int>();
            for (int i = 0; i < 3; i++)
            {
                if (!Valid(Blocks[i]) || !Valid(Targets[i]) || !occupied.Add(Blocks[i]) || !goals.Add(Targets[i]))
                    throw new ArgumentException("Invalid or duplicate block/target cell");
            }
        }

        bool Valid(int cell) => cell >= 0 && cell < 36 && !Walls[cell];
        public bool IsSolved => Blocks[0] == Targets[0] && Blocks[1] == Targets[1] && Blocks[2] == Targets[2];
        public bool CanUndo => history.Count > 0;

        // Clicking the active block clears the anchor. An anchor change is undoable.
        public bool SelectAnchor(int block)
        {
            if (block < 0 || block > 2) return false;
            history.Push(new Snapshot(this));
            Anchor = Anchor == block ? -1 : block;
            return true;
        }

        public bool Move(int dx, int dy)
        {
            if (Math.Abs(dx) + Math.Abs(dy) != 1 || IsSolved) return false;
            var order = new[] { 0, 1, 2 };
            // Frontmost first: a trailing block can enter a cell vacated in this turn.
            Array.Sort(order, (a, b) =>
            {
                int pa = Blocks[a] % Size * dx + Blocks[a] / Size * dy;
                int pb = Blocks[b] % Size * dx + Blocks[b] / Size * dy;
                int compare = pb.CompareTo(pa);
                return compare != 0 ? compare : a.CompareTo(b);
            });
            var before = new Snapshot(this);
            bool changed = false;
            foreach (int i in order)
            {
                if (i == Anchor) continue;
                int x = Blocks[i] % Size + dx, y = Blocks[i] / Size + dy;
                if (x < 0 || x >= Size || y < 0 || y >= Size) continue;
                int next = y * Size + x;
                if (Walls[next] || Array.IndexOf(Blocks, next) != -1) continue;
                Blocks[i] = next;
                changed = true;
            }
            if (changed) history.Push(before);
            return changed;
        }

        public bool Undo()
        {
            if (history.Count == 0) return false;
            var snapshot = history.Pop();
            Blocks[0] = snapshot.A; Blocks[1] = snapshot.B; Blocks[2] = snapshot.C;
            Anchor = snapshot.Anchor;
            return true;
        }
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public string name;
        public string[] walls;
        public int[] blocks;
        public int[] targets;
    }

    [Serializable]
    public sealed class LevelCollection
    {
        public LevelDefinition[] levels;
    }
}
