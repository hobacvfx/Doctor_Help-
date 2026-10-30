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
        [SerializeField] private bool keepLockedSlots;
        private string message;
        private MessageType messageType;

        [MenuItem("Tools/NPC/Tạo NPC hàng loạt")]
        public static void Open()
        {
            var window = GetWindow<NpcBatchGeneratorWindow>("Tạo NPC hàng loạt");
            window.minSize = new Vector2(460, 330);
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
            firstSeed = EditorGUILayout.IntField("Seed bắt đầu", firstSeed);
            count = EditorGUILayout.IntField("Số lượng", count);
            keepLockedSlots = EditorGUILayout.Toggle(new GUIContent("Giữ Lock Override", "Mặc định tắt để các slot ngoại hình đều được chọn theo seed mới."), keepLockedSlots);
            EditorGUILayout.Space();
            string range = count > 0 && firstSeed >= 0 && (long)firstSeed + count - 1 <= int.MaxValue
                ? $"NPC_{firstSeed} → NPC_{(long)firstSeed + count - 1}" : "Seed/số lượng chưa hợp lệ";
            EditorGUILayout.HelpBox($"{range}\nLưu tại: {(profile != null ? profile.NpcFolder : "chọn NPC Profile")}\nMỗi NPC mới nhận ngẫu nhiên một bệnh từ AllSickness. NPC cũ được giữ nguyên. Trùng tên sẽ báo lỗi trước khi tạo.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || template == null || profile == null || count < 1 || firstSeed < 0))
                if (GUILayout.Button("Tạo NPC", GUILayout.Height(32)))
                {
                    try
                    {
                        var generated = NpcBatchGenerator.Generate(template, profile, catalog, firstSeed, count, keepLockedSlots);
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
