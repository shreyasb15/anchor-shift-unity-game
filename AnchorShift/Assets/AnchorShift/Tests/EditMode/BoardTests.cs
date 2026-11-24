using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AnchorShift.Tests
{
    public class BoardTests
    {
        static LevelDefinition Level(string[] rows, int[] blocks, int[] targets) =>
            new LevelDefinition { walls = rows, blocks = blocks, targets = targets };
        static readonly string[] Empty = { "......", "......", "......", "......", "......", "......" };

        [Test]
        public void FrontBlockVacatesCellForTrailingBlock()
        {
            var board = new BoardModel(Level(Empty, new[] { 7, 8, 30 }, new[] { 0, 1, 2 }));
            Assert.IsTrue(board.Move(1, 0));
            CollectionAssert.AreEqual(new[] { 8, 9, 31 }, board.Blocks);
        }

        [Test]
        public void AnchorBlocksMovementAndUndoRestoresExactState()
        {
            var board = new BoardModel(Level(Empty, new[] { 7, 8, 30 }, new[] { 0, 1, 2 }));
            board.SelectAnchor(1);
            Assert.IsTrue(board.Move(1, 0));
            CollectionAssert.AreEqual(new[] { 7, 8, 31 }, board.Blocks);
            Assert.IsTrue(board.Undo());
            CollectionAssert.AreEqual(new[] { 7, 8, 30 }, board.Blocks);
            Assert.AreEqual(1, board.Anchor);
            Assert.IsTrue(board.Undo());
            Assert.AreEqual(-1, board.Anchor);
        }

        [Test]
        public void BoundaryAndWallStopMovement()
        {
            var rows = (string[])Empty.Clone();
            rows[1] = "..#...";
            var board = new BoardModel(Level(rows, new[] { 6, 7, 11 }, new[] { 0, 1, 2 }));
            Assert.IsFalse(board.Move(1, 0));
            CollectionAssert.AreEqual(new[] { 6, 7, 11 }, board.Blocks);
            Assert.IsFalse(board.CanUndo);
        }

        [Test]
        public void UndoAndRestartRecoverFromSolvedPosition()
        {
            var level = Level(Empty, new[] { 6, 12, 18 }, new[] { 7, 13, 19 });
            var board = new BoardModel(level);
            Assert.IsTrue(board.Move(1, 0));
            Assert.IsTrue(board.IsSolved);
            Assert.IsFalse(board.Move(1, 0));
            Assert.IsTrue(board.Undo());
            Assert.IsFalse(board.IsSolved);
            CollectionAssert.AreEqual(level.blocks, board.Blocks);
            board.Move(1, 0);
            var restarted = new BoardModel(level);
            CollectionAssert.AreEqual(level.blocks, restarted.Blocks);
            Assert.IsFalse(restarted.CanUndo);
        }

        [Test]
        public void AllEightLevelsAreSolvable()
        {
            var text = Resources.Load<TextAsset>("levels");
            Assert.IsNotNull(text);
            var levels = JsonUtility.FromJson<LevelCollection>(text.text).levels;
            Assert.AreEqual(8, levels.Length);
            foreach (var level in levels)
            {
                int moves = ShortestSolution(level);
                Debug.Log("Anchor Shift: " + level.name + " minimum moves " + moves);
                Assert.Greater(moves, 0, level.name);
            }
        }

        static int ShortestSolution(LevelDefinition level)
        {
            var initial = new BoardModel(level);
            var queue = new Queue<(int[], int)>();
            var visited = new HashSet<string>();
            queue.Enqueue(((int[])initial.Blocks.Clone(), 0));
            visited.Add(Key(initial.Blocks));
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                var blocks = item.Item1;
                if (blocks[0] == initial.Targets[0] && blocks[1] == initial.Targets[1] && blocks[2] == initial.Targets[2])
                    return item.Item2;
                for (int anchor = -1; anchor < 3; anchor++)
                for (int direction = 0; direction < 4; direction++)
                {
                    var model = new BoardModel(level);
                    // Recreate a reached state by assigning the plain data, then use the actual production resolver.
                    for (int i = 0; i < 3; i++) model.Blocks[i] = blocks[i];
                    if (anchor >= 0) model.SelectAnchor(anchor);
                    int dx = direction == 1 ? 1 : direction == 3 ? -1 : 0;
                    int dy = direction == 2 ? 1 : direction == 0 ? -1 : 0;
                    if (!model.Move(dx, dy)) continue;
                    var key = Key(model.Blocks);
                    if (visited.Add(key)) queue.Enqueue(((int[])model.Blocks.Clone(), item.Item2 + 1));
                }
            }
            return -1;
        }

        static string Key(int[] positions) => positions[0] + "," + positions[1] + "," + positions[2];
    }
}
