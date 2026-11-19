using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.Animations;
using UnityEngine;

namespace DoctorHelp.Editor
{
    public sealed class SpriteFlipbookWindow : EditorWindow
    {
        private static readonly string[] SourceModes = { "Thư mục sprite", "Sprite Sheet" };
        [SerializeField] private bool useSheet;
        [SerializeField] private DefaultAsset sourceFolder;
        [SerializeField] private Texture2D sourceSheet;
        [SerializeField] private int columns = 4, rows = 2;
        [SerializeField] private int frameRate = 16;

        private static readonly Comparer<string> FrameOrder = Comparer<string>.Create((left, right) =>
        {
            int naturalOrder = EditorUtility.NaturalCompare(left, right);
            return naturalOrder != 0 ? naturalOrder : string.CompareOrdinal(left, right);
        });

        private readonly struct Frame
        {
            internal readonly Sprite Sprite;
            internal readonly Texture2D Source;
            internal readonly Rect UV;
            internal readonly Rect Bounds;

            internal Frame(Sprite sprite, Texture2D source)
            {
                Sprite = sprite;
                Source = source;
                UV = TextureCoordinates(sprite.rect, source.width, source.height);
                Bounds = new Rect(-sprite.pivot / sprite.pixelsPerUnit, sprite.rect.size / sprite.pixelsPerUnit);
            }

            internal Frame(Texture2D source, Rect cell, int width, int height)
            {
                Sprite = null;
                Source = source;
                UV = TextureCoordinates(cell, width, height);
                Bounds = new Rect(-cell.size * .5f, cell.size);
            }
        }

        private Frame[] frames = Array.Empty<Frame>();
        private Rect[] sheetCells = Array.Empty<Rect>();
        private Rect previewBounds;
        private string sourcePath;
        private string message;
        private MessageType messageType;
        private bool playing = true;
        private double playbackStart;
        private int currentFrame;

        [MenuItem("Tools/Animation/Sprite Flipbook")]
        private static void Open()
        {
            var window = GetWindow<SpriteFlipbookWindow>("Sprite Flipbook");
            window.minSize = new Vector2(360, 440);
            if (Selection.activeObject is DefaultAsset folder &&
                AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder)))
            {
                window.sourceFolder = folder;
                window.useSheet = false;
                window.playing = true;
                window.RefreshFrames();
            }
            else if (Selection.activeObject is Texture2D sheet)
            {
                window.sourceSheet = sheet;
                window.useSheet = true;
                window.playing = true;
                window.RefreshFrames();
            }
        }

        private void OnEnable() => RefreshFrames();
        private void OnProjectChange() => RefreshFrames();

        private void Update()
        {
            if (!playing || frames.Length < 2) return;
            double elapsed = EditorApplication.timeSinceStartup - playbackStart;
            int nextFrame = (int)((long)(elapsed * frameRate) % frames.Length);
            if (currentFrame == nextFrame) return;
            currentFrame = nextFrame;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            useSheet = GUILayout.Toolbar(useSheet ? 1 : 0, SourceModes) == 1;
            if (useSheet)
            {
                sourceSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", sourceSheet, typeof(Texture2D), false);
                columns = Mathf.Clamp(EditorGUILayout.DelayedIntField("Cột", columns), 1, 32);
                rows = Mathf.Clamp(EditorGUILayout.DelayedIntField("Hàng", rows), 1, 32);
            }
            else
            {
                sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Thư mục sprite", sourceFolder, typeof(DefaultAsset), false);
            }
            if (EditorGUI.EndChangeCheck())
            {
                message = null;
                playing = true;
                RefreshFrames();
            }

            EditorGUI.BeginChangeCheck();
            frameRate = EditorGUILayout.IntSlider("FPS", frameRate, 1, 120);
            if (EditorGUI.EndChangeCheck()) RestartPlaybackClock();

            if (frames.Length == 0)
            {
                EditorGUILayout.HelpBox(useSheet
                    ? "Chọn một ảnh trong Assets và nhập số cột, số hàng phù hợp với kích thước ảnh."
                    : "Chọn thư mục trong Assets chứa ảnh đã import dạng Sprite (Single hoặc Multiple).", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{frames.Length} frame · {(float)frames.Length / frameRate:0.###} giây/vòng");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(playing ? "Tạm dừng" : "Phát lặp"))
                {
                    playing = !playing;
                    RestartPlaybackClock();
                }

                if (GUILayout.Button("Đọc lại")) RefreshFrames();
            }

            EditorGUI.BeginChangeCheck();
            int chosenFrame = EditorGUILayout.IntSlider("Frame", currentFrame + 1, 1, frames.Length) - 1;
            if (EditorGUI.EndChangeCheck())
            {
                playing = false;
                currentFrame = chosenFrame;
            }

            EditorGUILayout.LabelField(useSheet ? $"Ô {currentFrame + 1}" : frames[currentFrame].Sprite.name, EditorStyles.miniLabel);
            var previewRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint) DrawPreview(previewRect, frames[currentFrame]);

            string exportLabel = useSheet ? "Cắt lưới và tạo Clip + Controller" : "Tạo Animation Clip + Animator Controller";
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button(exportLabel, GUILayout.Height(28)))
                    CreateAssets();

            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
        }

        private void RefreshFrames()
        {
            UnityEngine.Object source = useSheet ? sourceSheet : sourceFolder;
            sourcePath = source == null ? "" : AssetDatabase.GetAssetPath(source);
            frames = Array.Empty<Frame>();
            sheetCells = Array.Empty<Rect>();
            if (sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
                frames = useSheet ? ReadSheetFrames() : ReadFolderFrames();

            previewBounds = default;
            foreach (var frame in frames)
                previewBounds = Rect.MinMaxRect(
                    Mathf.Min(previewBounds.xMin, frame.Bounds.xMin), Mathf.Min(previewBounds.yMin, frame.Bounds.yMin),
                    Mathf.Max(previewBounds.xMax, frame.Bounds.xMax), Mathf.Max(previewBounds.yMax, frame.Bounds.yMax));
            currentFrame = 0;
            RestartPlaybackClock();
            Repaint();
        }

        private Frame[] ReadFolderFrames()
        {
            if (!AssetDatabase.IsValidFolder(sourcePath)) return Array.Empty<Frame>();
            var found = new List<Frame>();
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { sourcePath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith(sourcePath + "/", StringComparison.Ordinal)
                    && path.LastIndexOf('/') == sourcePath.Length)
                .OrderBy(Path.GetFileName, FrameOrder);
            foreach (string path in paths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name, FrameOrder))
                    found.Add(new Frame(sprite, texture));
            }

            return found.ToArray();
        }

        private Frame[] ReadSheetFrames()
        {
            if (AssetImporter.GetAtPath(sourcePath) is not TextureImporter importer) return Array.Empty<Frame>();
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (columns > width || rows > height) return Array.Empty<Frame>();

            var found = new Frame[columns * rows];
            sheetCells = new Rect[found.Length];
            for (int index = 0; index < found.Length; index++)
            {
                int column = index % columns;
                int row = index / columns;
                // Unity uses a bottom-left origin, while animation reads from the top row.
                var cell = Rect.MinMaxRect(width * column / columns, height - height * (row + 1) / rows,
                    width * (column + 1) / columns, height - height * row / rows);
                sheetCells[index] = cell;
                found[index] = new Frame(sourceSheet, cell, width, height);
            }

            return found;
        }

        private void RestartPlaybackClock() =>
            playbackStart = EditorApplication.timeSinceStartup - (double)currentFrame / frameRate;

        private static Rect TextureCoordinates(Rect pixels, int width, int height) =>
            new Rect(pixels.x / width, pixels.y / height, pixels.width / width, pixels.height / height);

        private void DrawPreview(Rect area, Frame frame)
        {
            EditorGUI.DrawRect(area, new Color(.19f, .19f, .19f));
            if (area.width <= 16 || area.height <= 16 || frame.Source == null) return;

            float scale = Mathf.Min((area.width - 16) / previewBounds.width, (area.height - 16) / previewBounds.height);
            Rect bounds = frame.Bounds;
            Vector2 fromCenter = new Vector2(bounds.xMin - previewBounds.center.x, previewBounds.center.y - bounds.yMax);
            GUI.DrawTextureWithTexCoords(new Rect(area.center + fromCenter * scale, bounds.size * scale), frame.Source, frame.UV);
        }

        private static Sprite[] GetSheetSprites(string path, Rect[] cells)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories();
            factories.Init();

            ISpriteEditorDataProvider ReadProvider()
            {
                var data = factories.GetSpriteEditorDataProviderFromObject(importer);
                if (data == null) throw new InvalidOperationException("Unity không đọc được cấu hình sprite của ảnh.");
                data.InitSpriteEditorDataProvider();
                return data;
            }

            var existing = importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Multiple
                ? ReadProvider().GetSpriteRects()
                : Array.Empty<SpriteRect>();
            var ordered = cells.Select(cell => existing.FirstOrDefault(slice => slice.rect == cell)).ToArray();
            if (existing.Length == 0)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                for (int index = 0; index < cells.Length; index++)
                    ordered[index] = new SpriteRect
                    {
                        name = $"{name}_{index + 1:00}",
                        rect = cells[index],
                        alignment = SpriteAlignment.Center,
                        pivot = Vector2.one * .5f,
                        spriteID = GUID.Generate()
                    };

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                var provider = ReadProvider();
                provider.SetSpriteRects(ordered);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                    .SetNameFileIdPairs(ordered.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)));
                provider.Apply();
                importer.SaveAndReimport();
            }
            else if (ordered.Any(slice => slice == null))
            {
                throw new InvalidOperationException("Lưới này khác các slice hiện có. Chọn lưới khớp hoặc dùng chế độ Thư mục sprite để xuất các slice cũ.");
            }

            var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToLookup(sprite => sprite.name);
            return ordered.Select(slice => imported[slice.name].Single()).ToArray();
        }

        private void CreateAssets()
        {
            string folderPath = useSheet ? sourcePath[..sourcePath.LastIndexOf('/')] : sourcePath;
            string name = (useSheet ? Path.GetFileNameWithoutExtension(sourcePath) : Path.GetFileName(folderPath)) + "_Loop";
            string clipPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{name}.anim");
            string controllerPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{name}.controller");
            var clip = new AnimationClip { frameRate = frameRate };
            try
            {
                var sprites = useSheet ? GetSheetSprites(sourcePath, sheetCells) : frames.Select(frame => frame.Sprite).ToArray();
                var keys = new ObjectReferenceKeyframe[sprites.Length];
                for (int index = 0; index < sprites.Length; index++)
                    keys[index] = new ObjectReferenceKeyframe { time = (float)index / frameRate, value = sprites[index] };

                AnimationUtility.SetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                settings.stopTime = (float)sprites.Length / frameRate;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, clipPath);
                var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
                if (controller == null) throw new InvalidOperationException("Unity không tạo được Animator Controller.");
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(controller);
                message = $"Đã tạo {Path.GetFileName(clipPath)} và {Path.GetFileName(controllerPath)}. Gán controller lên GameObject có SpriteRenderer.";
                messageType = MessageType.Info;
            }
            catch (Exception error)
            {
                if (AssetDatabase.LoadMainAssetAtPath(controllerPath) != null) AssetDatabase.DeleteAsset(controllerPath);
                if (AssetDatabase.Contains(clip)) AssetDatabase.DeleteAsset(clipPath);
                else DestroyImmediate(clip);
                message = error.Message;
                messageType = MessageType.Error;
                Debug.LogException(error);
            }
        }
    }
}
