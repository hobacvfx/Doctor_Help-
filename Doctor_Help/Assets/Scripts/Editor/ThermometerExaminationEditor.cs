using UnityEditor;
namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(ThermometerExamination))] public sealed class ThermometerExaminationEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();DrawPropertiesExcluding(serializedObject,"m_Script","temperatureRules");
            var spawn=serializedObject.FindProperty("patients").objectReferenceValue as PatientSpawnPoint;
            var source=spawn!=null && spawn.NpcProfile!=null && spawn.NpcProfile.AllSickness!=null?spawn.NpcProfile.AllSickness.SymptomProfile:null;
            var rules=serializedObject.FindProperty("temperatureRules");
            DrawSymptom(rules.FindPropertyRelative("highTemperatureSymptomId"),"High temperature",source);
            DrawSymptom(rules.FindPropertyRelative("lowTemperatureSymptomId"),"Low temperature",source);
            EditorGUILayout.HelpBox("Normal 37 C; high 38.6-42 C; low 33-34.9 C. One temperature per consultation; repeated holds reuse it. Readings depend only on the assigned sickness symptoms.",MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
        private static void DrawSymptom(SerializedProperty property,string label,SymptomProfile source)
        {
            if(source==null){EditorGUILayout.PropertyField(property);return;}
            var entries=source.Symptoms;var names=new string[entries.Count+1];names[0]="Missing / unassigned: "+property.stringValue;int current=0;
            for(int i=0;i<entries.Count;i++){names[i+1]=entries[i].DisplayName;if(entries[i].Id==property.stringValue)current=i+1;}
            int selected=EditorGUILayout.Popup(label,current,names);
            if(selected!=current)property.stringValue=selected==0?"":entries[selected-1].Id;
        }
    }
}


