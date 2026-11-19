using System;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    public sealed class NpcBatchGeneratorWindow : EditorWindow
    {
        [SerializeField] private GameObject template;
        [SerializeField] private NpcProfile profile;
        [SerializeField] private NpcCatalog catalog;
        [SerializeField] private int firstSeed = 6;
        [SerializeField] private int count = 10;
        [SerializeField] private int difficulty = 1;
        [SerializeField] private bool keepLockedSlots;
        private static readonly string[] DifficultyOptions = { "Theo xác suất cũ", "Diff 1", "Diff 2", "Diff 3", "Diff 4" };
        private string message;
        private MessageType messageType;

        [MenuItem("Tools/NPC/Tạo NPC hàng loạt")]
        public static void Open()
        {
            var window = GetWindow<NpcBatchGeneratorWindow>("Tạo NPC hàng loạt");
            window.minSize = new Vector2(460, 360);
            window.Show();
        }
        private void OnEnable()
        {
            if (template == null) template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2D_Assets/NPC/NPC_0.prefab");
            if (profile == null) profile = AssetDatabase.LoadAssetAtPath<NpcProfile>("Assets/Data/NPC/NpcProfile.asset");
            if (catalog == null) catalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>("Assets/Resources/NpcCatalog.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tạo NPC từ prefab mẫu", EditorStyles.boldLabel);
            template = (GameObject)EditorGUILayout.ObjectField("Prefab mẫu", template, typeof(GameObject), false);
            profile = (NpcProfile)EditorGUILayout.ObjectField("NPC Profile", profile, typeof(NpcProfile), false);
            catalog = (NpcCatalog)EditorGUILayout.ObjectField(new GUIContent("Danh sách spawn", "Thêm NPC mới để có thể xuất hiện khi khám. Để trống nếu chỉ tạo prefab/profile."), catalog, typeof(NpcCatalog), false);
            NpcProfileSynchronization.DrawButtons(profile, catalog, (text, type) => { message = text; messageType = type; });
            EditorGUILayout.Space();
            firstSeed = EditorGUILayout.IntField("Seed bắt đầu", firstSeed);
            count = EditorGUILayout.IntField("Số lượng", count);
            difficulty = EditorGUILayout.Popup("Diff", difficulty, DifficultyOptions);
            keepLockedSlots = EditorGUILayout.Toggle(new GUIContent("Giữ Lock Override", "Mặc định tắt để các slot ngoại hình đều được chọn theo seed mới."), keepLockedSlots);
            EditorGUILayout.Space();
            string range = count > 0 && firstSeed >= 0 && (long)firstSeed + count - 1 <= int.MaxValue
                ? $"NPC_{firstSeed} → NPC_{(long)firstSeed + count - 1}" : "Seed/số lượng chưa hợp lệ";
            string assignment = difficulty == 0
                ? "Số bệnh theo xác suất bệnh thứ hai trong NPC Profile."
                : $"Mỗi NPC mới có đúng {difficulty} bệnh tương thích.";
            EditorGUILayout.HelpBox($"{range}\nLưu tại: {(profile != null ? profile.NpcFolder : "chọn NPC Profile")}\n{assignment} Trùng tên sẽ báo lỗi trước khi tạo.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || template == null || profile == null || count < 1 || firstSeed < 0))
                if (GUILayout.Button("Tạo NPC", GUILayout.Height(32)))
                {
                    try
                    {
                        var generated = NpcBatchGenerator.Generate(template, profile, catalog, firstSeed, count, keepLockedSlots, difficulty);
                        message = $"Đã tạo {generated.Count} NPC và cập nhật profile" + (catalog != null ? ", danh sách spawn." : ".");
                        messageType = MessageType.Info;
                        if ((long)firstSeed + count <= int.MaxValue) firstSeed += count;
                    }
                    catch (OperationCanceledException error) { message = error.Message; messageType = MessageType.Warning; }
                    catch (Exception error) { message = error.Message; messageType = MessageType.Error; Debug.LogException(error); }
                }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
        }
    }
}


