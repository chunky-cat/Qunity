using UnityEngine;
using UnityEditor;
using Qunity;

[CustomEditor(typeof(QuakeMapAssetImporter))]
public class QuakeMapImporterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        QuakeMapAssetImporter importer = (QuakeMapAssetImporter)target;
        if (importer.warnTexturesReimported)
        {
            EditorGUILayout.HelpBox("Textures where not readable, please reimport this map!", MessageType.Warning);
        }
        if (importer.warnTexturesMissing)
        {
            EditorGUILayout.HelpBox("Some textures are missing", MessageType.Warning);
        }
    }
}
