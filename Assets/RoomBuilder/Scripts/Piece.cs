using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RoomBuilder
{
    public sealed class Piece : MonoBehaviour
    {
        [field: SerializeField] public Mesh Mesh {get; set;}

        [SerializeField] private Material[] _materials;
        public Material[] Materials { get => _materials; set => _materials = value; }

        private Assembly _assembly;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(Mesh == null)
            {
                return;
            }

            if(Materials.Length != Mesh.subMeshCount)
            {
                Array.Resize(ref _materials, Mesh.subMeshCount);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.25f, 0f, 0.3f);
            Gizmos.DrawMesh(Mesh);
        }

        private void OnDrawGizmos()
        {
            if(_assembly == null)
            {
                _assembly = GetComponentInParent<Assembly>();
            }
            if(_assembly != null && !_assembly.Preview)
            {
                return;
            }

            if(Mesh == null || Materials.Length == 0 || !enabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;

            // Draw the mesh as an invisible gizmo so that it can be picked
            Gizmos.color = new Color(0f, 0f, 0f, 0f);
            Gizmos.DrawMesh(Mesh);

            for(var i=0; i<Materials.Length; i++) {
                Materials[i].SetPass(i);
                //Graphics.DrawMeshNow(Mesh, Gizmos.matrix, i);
                Gizmos.DrawMesh(Mesh, i);
            }
        }
        #endif
    }
}