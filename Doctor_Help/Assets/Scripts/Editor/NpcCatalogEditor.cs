using UnityEditor;
using UnityEngine;
namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(NpcCatalog))]
    public sealed class NpcCatalogEditor : UnityEditor.Editor
    {
        private NpcProfile profile;
        private string message;
        private MessageType messageType;
        private void OnEnable() => profile = AssetDatabase.LoadAssetAtPath<NpcProfile>("Assets/Data/NPC/NpcProfile.asset");
        public override void OnInspectorGUI()
        {
            profile = (NpcProfile)EditorGUILayout.ObjectField("NPC Profile", profile, typeof(NpcProfile), false);
            EditorGUILayout.HelpBox("Refresh lấy các prefab còn tồn tại trong profile làm danh sách spawn, loại bỏ mục null/trùng. Nút xóa bên dưới chỉ xóa bản ghi mất prefab trong profile; không xóa asset prefab.", MessageType.Info);
            NpcProfileSynchronization.DrawButtons(profile, (NpcCatalog)target, (text, type) => { message = text; messageType = type; });
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
            DrawDefaultInspector();
        }
    }
}
