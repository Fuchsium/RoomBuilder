using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoomBuilder.Editor
{
    [CustomEditor(typeof(Assembly))]
    public sealed class AssemblyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var self = (Assembly)target;

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Build Result", self._builtParent, typeof(GameObject), allowSceneObjects: true);
            GUI.enabled = true;

            if(GUILayout.Button(self._builtParent == null ? "Build" : "Rebuild"))
            {
                self.Rebuild();
            }
            if(self._builtParent != null && GUILayout.Button("Clean build result"))
            {
                EditorSceneManager.MarkSceneDirty(self._builtParent.scene);

                if(self._builtParent.TryGetComponent(out MeshFilter meshFilter))
                {
                    if(meshFilter.sharedMesh != null)
                    {
                        var path = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                        if(!string.IsNullOrEmpty(path))
                        {
                            AssetDatabase.DeleteAsset(path);
                        }
                    }
                }

                Undo.DestroyObjectImmediate(self._builtParent);
            }
        }
    }
}
