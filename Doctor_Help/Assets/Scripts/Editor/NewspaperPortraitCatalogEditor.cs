using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(NewspaperPortraitCatalog))]
    public sealed class NewspaperPortraitCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Sepia portraits are captured directly from the saved NPC prefabs. Refresh after editing their appearance or adding NPCs.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Capture missing NPC portraits"))
                    try { CaptureMissing((NewspaperPortraitCatalog)target); }
                    catch (OperationCanceledException) { }
                    catch (Exception error) { Debug.LogException(error); }
                if (GUILayout.Button("Capture / refresh NPC portraits"))
                    try { Capture((NewspaperPortraitCatalog)target); }
                    catch (OperationCanceledException) { }
                    catch (Exception error) { Debug.LogException(error); }
            }
        }

        public static void Capture(NewspaperPortraitCatalog catalog) => Capture(catalog, false);

        public static int CaptureMissing(NewspaperPortraitCatalog catalog) => Capture(catalog, true);

        private static int Capture(NewspaperPortraitCatalog catalog, bool missingOnly)
        {
            if (catalog.Source == null) throw new InvalidOperationException("Assign the NPC profile first.");
            var targets = catalog.Source.Npcs.Where(npc =>
                npc?.Prefab != null && (!missingOnly || catalog.Find(npc.Id) == null)).ToArray();
            if (targets.Length == 0 && missingOnly) return 0;

            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(catalog)).Replace('\\', '/') + "/Portraits";
            Directory.CreateDirectory(folder);
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("portraits");
            if (!missingOnly) entries.arraySize = 0;
            var entryIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < entries.arraySize; index++)
                entryIndexes.TryAdd(entries.GetArrayElementAtIndex(index).FindPropertyRelative("npcId").stringValue, index);

            try
            {
                for (int index = 0; index < targets.Length; index++)
                {
                    var npc = targets[index];
                    if (EditorUtility.DisplayCancelableProgressBar("Newspaper portraits", npc.NpcName,
                        (float)index / targets.Length)) throw new OperationCanceledException();
                    string path = folder + "/" + npc.Id + ".png";
                    var sprite = missingOnly ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
                    if (sprite == null)
                    {
                        CapturePrefab(npc.Prefab, path);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        // Mobile builds compress portraits (ASTC 6x6); the editor/PC keeps the uncompressed source look.
                        foreach (string platform in new[] { "Android", "iOS" })
                        {
                            var settings = importer.GetPlatformTextureSettings(platform);
                            settings.overridden = true;
                            settings.format = TextureImporterFormat.ASTC_6x6;
                            settings.compressionQuality = 50;
                            importer.SetPlatformTextureSettings(settings);
                        }
                        importer.SaveAndReimport();
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                    if (sprite == null) throw new InvalidOperationException("Portrait did not import as a sprite: " + path);
                    if (!entryIndexes.TryGetValue(npc.Id, out int entryIndex))
                    {
                        entryIndex = entries.arraySize;
                        entries.InsertArrayElementAtIndex(entryIndex);
                        entryIndexes.Add(npc.Id, entryIndex);
                    }
                    var entry = entries.GetArrayElementAtIndex(entryIndex);
                    entry.FindPropertyRelative("npcId").stringValue = npc.Id;
                    entry.FindPropertyRelative("image").objectReferenceValue = sprite;
                }
                Undo.RecordObject(catalog, "Refresh newspaper portraits");
                data.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
                return targets.Length;
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void CapturePrefab(GameObject prefab, string path)
        {
            var preview = new PreviewRenderUtility();
            Texture2D texture = null;
            try
            {
                var instance = Instantiate(prefab);
                preview.AddSingleGO(instance);
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                foreach (var animator in instance.GetComponentsInChildren<Animator>()) animator.enabled = false;
                var dialogue = instance.GetComponent<NpcDialogue>();
                if (dialogue != null)
                {
                    var box = (GameObject)new SerializedObject(dialogue).FindProperty("chatBox").objectReferenceValue;
                    if (box != null) box.SetActive(false);
                }
                SpriteRenderer head = null;
                foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.name == "Head") { head = renderer; break; }
                if (head == null) throw new InvalidOperationException("NPC portrait requires a Head sprite: " + prefab.name);
                var bounds = head.bounds;
                if (bounds.size.y <= Mathf.Epsilon)
                    throw new InvalidOperationException("NPC portrait requires a visible Head sprite: " + prefab.name);
                float height = bounds.size.y * 1.9f;
                var center = bounds.center + Vector3.down * bounds.size.y * .23f;
                preview.camera.orthographic = true;
                preview.camera.orthographicSize = height / 2;
                preview.camera.nearClipPlane = .01f;
                preview.camera.farClipPlane = 100;
                preview.camera.transform.position = new Vector3(center.x, center.y, -30);
                preview.camera.transform.rotation = Quaternion.identity;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(.78f, .70f, .56f, 1);
                preview.BeginStaticPreview(new Rect(0, 0, 240, 280));
                preview.Render(true);
                texture = preview.EndStaticPreview();
                var pixels = texture.GetPixels();
                float darkest = 1f;
                float lightest = 0f;
                for (int i = 0; i < pixels.Length; i++)
                {
                    float grey = pixels[i].grayscale;
                    darkest = Mathf.Min(darkest, grey);
                    lightest = Mathf.Max(lightest, grey);
                    pixels[i] = new Color(grey * .95f, grey * .84f, grey * .67f, 1);
                }
                if (lightest - darkest < .05f)
                    throw new InvalidOperationException("NPC portrait rendered without visible detail: " + prefab.name);
                texture.SetPixels(pixels); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { if (texture != null) DestroyImmediate(texture); preview.Cleanup(); }
        }
    }
}
