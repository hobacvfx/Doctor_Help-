using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(HeartbeatExamination))]
    public sealed class HeartbeatExaminationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,"m_Script","rhythm");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Heartbeat rhythm",EditorStyles.boldLabel);
            var rhythm=serializedObject.FindProperty("rhythm");
            EditorGUILayout.PropertyField(rhythm.FindPropertyRelative("normalBpm"));
            var spawn=serializedObject.FindProperty("patients").objectReferenceValue as PatientSpawnPoint;
            var symptoms=spawn != null && spawn.NpcProfile != null && spawn.NpcProfile.AllSickness != null
                ? spawn.NpcProfile.AllSickness.SymptomProfile : null;
            DrawSymptom(rhythm.FindPropertyRelative("rapidHeartbeatSymptomId"),"Rapid heartbeat",symptoms);
            DrawSymptom(rhythm.FindPropertyRelative("slowHeartbeatSymptomId"),"Slow heartbeat",symptoms);
            DrawSymptom(rhythm.FindPropertyRelative("noHeartbeatSymptomId"),"No heartbeat",symptoms);
            EditorGUILayout.HelpBox("Normal / slow / fast: BPM, BPM × 0.5, BPM × 3. No Heartbeat shows a frozen heart without repeating VFX or audio. Distance affects sound VFX and audio, not the feedback VFX.",MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
        private static void DrawSymptom(SerializedProperty property,string label,SymptomProfile source)
        {
            if(source==null){EditorGUILayout.PropertyField(property,new GUIContent(label));return;}
            var entries=source.Symptoms;
            var names=new string[entries.Count+1];
            names[0]=string.IsNullOrEmpty(property.stringValue)?"None":"Missing / unassigned: "+property.stringValue;
            int current=0;
            for(int i=0;i<entries.Count;i++)
            {
                names[i+1]=entries[i].DisplayName;
                if(entries[i].Id==property.stringValue) current=i+1;
            }
            int selected=EditorGUILayout.Popup(label,current,names);
            if(selected!=current) property.stringValue=selected==0?"":entries[selected-1].Id;
        }
    }
}


