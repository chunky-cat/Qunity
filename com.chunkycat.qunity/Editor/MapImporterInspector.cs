using UnityEditor;
using Qunity;
using UnityEditor.AssetImporters;

[CustomEditor(typeof(QuakeMapAssetImporter))]
public class QuakeMapImporterInspector : ScriptedImporterEditor
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
