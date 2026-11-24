using UnityEngine;
using UnityEngine.InputSystem;

namespace AnchorShift
{
    // A single, resolution-independent IMGUI surface. All puzzle state lives in BoardModel.
    public sealed class AnchorShiftGame : MonoBehaviour
    {
        static readonly Color Background = Hex("11191B");
        static readonly Color Grid = Hex("223033");
        static readonly Color Floor = Hex("182225");
        static readonly Color Wall = Hex("394648");
        static readonly Color Text = Hex("D7D9D1");
        static readonly Color Muted = Hex("899794");
        static readonly Color[] Colors = { Hex("C77D68"), Hex("91A98C"), Hex("D1B766") };

        LevelCollection collection;
        BoardModel board;
        Texture2D anchorDot;
        int levelIndex;
        GUIStyle heading, label, small, centered, button;
        float scale, offsetX, offsetY;
        const float Cell = 70f;
        const float BoardX = 190f, BoardY = 174f;

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var result);
            // IMGUI colors are composited in the project's Linear color space.
            return result.linear;
        }

        void Awake()
        {
            anchorDot = CreateCircleTexture();
            var text = Resources.Load<TextAsset>("levels");
            if (text == null) { Debug.LogError("Anchor Shift: missing Resources/levels.json"); enabled = false; return; }
            collection = JsonUtility.FromJson<LevelCollection>(text.text);
            if (collection == null || collection.levels == null || collection.levels.Length == 0)
            { Debug.LogError("Anchor Shift: no levels found"); enabled = false; return; }
            LoadLevel(0);
        }

        void OnDestroy()
        {
            if (anchorDot != null) Destroy(anchorDot);
        }

        static Texture2D CreateCircleTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f));
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(13f - distance));
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        void LoadLevel(int index)
        {
            levelIndex = index;
            board = new BoardModel(collection.levels[index]);
        }

        void Update()
        {
            SelectBlockUnderMouse();
            var k = Keyboard.current;
            if (k == null || board == null) return;
            if (k.zKey.wasPressedThisFrame) board.Undo();
            if (k.rKey.wasPressedThisFrame) LoadLevel(levelIndex);
            if (board.IsSolved && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
            {
                if (levelIndex + 1 < collection.levels.Length) LoadLevel(levelIndex + 1);
                return;
            }
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) board.Move(0, -1);
            else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) board.Move(1, 0);
            else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) board.Move(0, 1);
            else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) board.Move(-1, 0);
        }

        void SelectBlockUnderMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || board == null || board.IsSolved) return;
            float currentScale = Mathf.Min(Screen.width / 800f, Screen.height / 800f);
            float left = (Screen.width - 800f * currentScale) * .5f;
            float top = (Screen.height - 800f * currentScale) * .5f;
            Vector2 pointer = mouse.position.ReadValue();
            float x = (pointer.x - left) / currentScale;
            float y = (Screen.height - pointer.y - top) / currentScale;
            int column = Mathf.FloorToInt((x - BoardX) / Cell);
            int row = Mathf.FloorToInt((y - BoardY) / Cell);
            if (column < 0 || column >= 6 || row < 0 || row >= 6) return;
            for (int i = 0; i < 3; i++)
                if (board.Blocks[i] == row * 6 + column) { board.SelectAnchor(i); break; }
        }

        void InitializeStyles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Text } };
            label = new GUIStyle(GUI.skin.label) { fontSize = 17, normal = { textColor = Text } };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Muted } };
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        }

        static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Outline(Rect rect, Color color, float width)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, width), color);
            Fill(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            Fill(new Rect(rect.x, rect.y, width, rect.height), color);
            Fill(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        void Circle(Vector2 center, float diameter, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - diameter / 2f, center.y - diameter / 2f, diameter, diameter), anchorDot);
            GUI.color = previousColor;
        }

        void OnGUI()
        {
            if (board == null) return;
            InitializeStyles();
            Fill(new Rect(0, 0, Screen.width, Screen.height), Background);
            scale = Mathf.Min(Screen.width / 800f, Screen.height / 800f);
            offsetX = (Screen.width - 800f * scale) * .5f;
            offsetY = (Screen.height - 800f * scale) * .5f;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY), Quaternion.identity, new Vector3(scale, scale, 1));

            GUI.Label(new Rect(BoardX, 86, 430, 38), "ANCHOR SHIFT", heading);
            GUI.Label(new Rect(BoardX, 123, 320, 26), collection.levels[levelIndex].name, small);
            GUI.Label(new Rect(510, 100, 100, 27), string.Format("{0:00} / {1:00}", levelIndex + 1, collection.levels.Length), label);

            for (int y = 0; y < 6; y++)
            for (int x = 0; x < 6; x++)
            {
                int cell = y * 6 + x;
                var tile = new Rect(BoardX + x * Cell, BoardY + y * Cell, Cell - 3, Cell - 3);
                Fill(tile, board.Walls[cell] ? Wall : Floor);
                if (!board.Walls[cell]) Outline(tile, Grid, 1);
                for (int i = 0; i < 3; i++)
                {
                    if (board.Targets[i] == cell)
                        Outline(new Rect(tile.x + 12, tile.y + 12, tile.width - 24, tile.height - 24), Colors[i], 2);
                    if (board.Blocks[i] != cell) continue;
                    var block = new Rect(tile.x + 8, tile.y + 8, tile.width - 16, tile.height - 16);
                    Fill(block, Colors[i]);
                    if (board.Anchor == i)
                    {
                        Outline(new Rect(block.x - 4, block.y - 4, block.width + 8, block.height + 8), Text, 2);
                        Circle(block.center, 16, Text);
                        Circle(block.center, 11, Background);
                    }
                }
            }

            if (board.IsSolved)
                GUI.Label(new Rect(BoardX, 609, 420, 28),
                    levelIndex + 1 == collection.levels.Length ? "All eight levels complete" : "Solved  ·  Enter for next level", centered);
            else
                GUI.Label(new Rect(BoardX, 609, 420, 28), "Click a block to anchor it", centered);

            GUI.Label(new Rect(BoardX, 669, 420, 25), "ARROWS / WASD  move     Z  undo     R  restart", small);
            DrawButton(new Rect(BoardX, 712, 94, 35), "Undo", board.CanUndo, () => board.Undo());
            DrawButton(new Rect(BoardX + 105, 712, 94, 35), "Restart", true, () => LoadLevel(levelIndex));

            GUI.matrix = previous;
        }

        void DrawButton(Rect rect, string caption, bool active, System.Action action)
        {
            Outline(rect, active ? Muted : Grid, 1);
            if (!active) return;
            if (GUI.Button(rect, caption, button)) action();
        }
    }
}
