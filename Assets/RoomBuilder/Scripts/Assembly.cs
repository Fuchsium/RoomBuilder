using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace RoomBuilder
{
    public sealed class Assembly : MonoBehaviour
    {
        [field: SerializeField] public bool Preview { get; set; } = true;
        [field: Space, SerializeField] public AssemblyParameters Parameters {get; set;}

        [HideInInspector, SerializeField] private bool _debug;
        [HideInInspector, SerializeField] internal GameObject _builtParent;

        public void Rebuild()
        {
            string newMeshName;
            if(_builtParent != null && _builtParent.TryGetComponent(out MeshFilter meshFilter))
            {
                newMeshName = meshFilter.sharedMesh.name;
            } else
            {
                newMeshName = "rbMesh_" + Guid.NewGuid().ToString("N");
            }

            Undo.IncrementCurrentGroup();
            var built = transform.Find("__AssemblyBuiltMeshes");
            if(built != null)
            {
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate(built.gameObject);
                #else
                Destroy(build.gameObject);
                #endif
            }
            built = new GameObject("__AssemblyBuiltMeshes").transform;
            built.parent = transform;
            #if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(built.gameObject, "Rebuilt Assembly Mesh");
            #endif

            var meshes = GetComponentsInChildren<Piece>().Where(x => x.enabled && x.gameObject.activeInHierarchy && x.Mesh != null).Select(x => (x.Mesh, x.Materials, Transform: x.transform.localToWorldMatrix));
            var materials = new HashSet<Material>(meshes.SelectMany(x => x.Materials)).ToArray();
            var meshesByMaterial = new List<(Mesh Mesh, int SubmeshIndex, Matrix4x4 Transform)>[materials.Length];
            DebugLog($"Materials: {string.Join(", ", materials.Select(x => x.name))}");
            foreach(var mesh in meshes)
            {
                for(var material = 0; material < mesh.Materials.Length; material++)
                {
                    var index = Array.IndexOf(materials, mesh.Materials[material]);
                    if(index == -1)
                    {
                        Debug.LogError("Array.IndexOf failed (this shouldn't happen)");
                        return;
                    }

                    if(meshesByMaterial[index] == null)
                    {
                        meshesByMaterial[index] = new List<(Mesh Mesh, int SubmeshIndex, Matrix4x4 Transform)>();
                    }
                    meshesByMaterial[index].Add((mesh.Mesh, material, mesh.Transform));
                }
            }

            DebugLog($"Found {meshesByMaterial.Length} submeshes:");
            for (int i = 0; i < meshesByMaterial.Length; i++)
            {
                var meshByMaterial = meshesByMaterial[i];
                DebugLog($" - {materials[i].name}");
                foreach(var submesh in meshByMaterial)
                {
                    var submeshInfo = submesh.Mesh.GetSubMesh(submesh.SubmeshIndex);
                    DebugLog($"    - Mesh: {submesh.Mesh.name}, Submesh Index: {submesh.SubmeshIndex}, Submesh Range: [{submeshInfo.indexStart}..{submeshInfo.indexStart + submeshInfo.indexCount - 1}] Transform:\n{submesh.Transform}");
                    DebugLog($"      Indices: {string.Join(", ", submesh.Mesh.triangles.Skip(submeshInfo.indexStart).Take(submeshInfo.indexCount))}");
                    DebugLog($"      Positions: {string.Join(", ", submesh.Mesh.vertices.Skip(submeshInfo.indexStart).Take(submeshInfo.indexCount))}");
                }
            }

            var pos = new List<Vector3>();
            var normal = new List<Vector3>();
            var color = new List<Color>();
            var uv1 = new List<Vector2>();

            var submeshes = new List<SubMeshDescriptor>();
            var indices = new List<int>();

            var lastSubmeshEndIndex = -1;
            var lastSubmeshSize = 0;
            for(var mat=0; mat<materials.Length; mat++)
            {
                var meshesForMaterial = meshesByMaterial[mat];
                var submeshIndexCount = 0;
                var submeshSize = 0;
                foreach(var mesh in meshesForMaterial)
                {
                    var submesh = mesh.Mesh.GetSubMesh(mesh.SubmeshIndex);
                    
                    var size = submesh.indexCount;
                    submeshIndexCount += size;

                    var meshIndices = mesh.Mesh.triangles.Skip(submesh.indexStart).Take(size).ToArray();
                    indices.AddRange(meshIndices.Select(x => x + lastSubmeshSize + submeshSize));

                    var meshPos = mesh.Mesh.vertices;
                    var meshNormal = mesh.Mesh.normals;
                    var meshColor = mesh.Mesh.colors;
                    var meshUV1 = mesh.Mesh.uv;

                    submeshSize += meshPos.Length;
                    var uniqueVerts = Enumerable.Range(0, meshPos.Length);
                    pos.AddRange(uniqueVerts.Select(x => mesh.Transform.MultiplyPoint(GetOrDefault(meshPos, x))));
                    normal.AddRange(uniqueVerts.Select(x => mesh.Transform.MultiplyVector(GetOrDefault(meshNormal, x))));
                    color.AddRange(uniqueVerts.Select(x => GetOrDefaultColor(meshColor, x)));
                    uv1.AddRange(uniqueVerts.Select(x => GetOrDefault(meshUV1, x)));
                }
                submeshes.Add(new SubMeshDescriptor() { indexStart = lastSubmeshEndIndex + 1, indexCount = submeshIndexCount });
                lastSubmeshEndIndex += submeshIndexCount;
                lastSubmeshSize += submeshSize;
            }

            DebugLog($"Indices: {string.Join(", ", indices)}");
            DebugLog($"Positions: {string.Join(", ", pos)}");

            var builtMesh = new Mesh()
            {
                name = newMeshName,
                vertices = pos.ToArray(),
                normals = normal.ToArray(),
                colors = color.ToArray(),
                uv = uv1.ToArray(),
                triangles = indices.ToArray(),
                subMeshCount = materials.Length
            };
            for(var i=0; i<materials.Length; i++) {
                builtMesh.SetSubMesh(i, submeshes[i]);
            }

            builtMesh.RecalculateTangents();

            built.gameObject.AddComponent<MeshFilter>().sharedMesh = builtMesh;
            built.gameObject.AddComponent<MeshRenderer>().materials = materials;
            _builtParent = built.gameObject;

            #if UNITY_EDITOR
            SaveMesh(builtMesh);
            EditorSceneManager.MarkSceneDirty(built.gameObject.scene);
            #endif
        }

        #if UNITY_EDITOR
        private void SaveMesh(Mesh mesh)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var resultPath = Parameters.BuildResultPath
                .Replace("{SceneName}", activeScene.name)
                .Replace("{SceneDir}", Path.GetDirectoryName(activeScene.path))
                .Replace('\\', '/');

            if(!resultPath.StartsWith("Assets/"))
            {
                resultPath = "Assets/" + resultPath;
            }

            var path = "";
            foreach(var segment in resultPath.Split(new char[] {'/', '\\'})) {
                path = Path.Combine(path, segment);
                if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            AssetDatabase.CreateAsset(mesh, Path.Combine(resultPath, mesh.name + ".asset"));
        }
        #endif

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugLog(string message)
        {
            if(_debug)
            {
                Debug.Log(message);
            }
        }

        private static T GetOrDefault<T>(T[] array, int index)
        {
            if(index >= array.Length)
            {
                return default;
            }
            return array[index];
        }
        private static Color GetOrDefaultColor(Color[] array, int index)
        {
            if(index >= array.Length)
            {
                return Color.white;
            }
            return array[index];
        }
    }
}
