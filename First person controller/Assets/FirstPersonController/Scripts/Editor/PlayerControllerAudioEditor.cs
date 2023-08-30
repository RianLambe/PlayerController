using UnityEditor;

[CustomEditor(typeof(AudioSettings))]
public class PlayerControllerAudioEditor : Editor {
    string tagStr;

    public override void OnInspectorGUI() {
        //Base inspectotor 
        //base.OnInspectorGUI(); 

        tagStr = EditorGUILayout.TagField("Tag for Objects:", tagStr);

    }
}
