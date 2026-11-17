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

        private void Awake()
        {
            #if !UNITY_EDITOR
            if(TryGetComponent(out MeshRenderer renderer))
            {
                Destroy(renderer);
            }
            if(TryGetComponent(out MeshFilter filter))
            {
                Destroy(filter);
            }
            #else
            var assembly = GetComponentInParent<Assembly>();
            if(TryGetComponent(out MeshRenderer renderer) && assembly != null)
            {
                renderer.enabled = assembly.Preview;
            }
            #endif
        }

#if UNITY_EDITOR
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

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

            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            if(_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if(_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();

            _meshRenderer.materials = Materials;
            _meshFilter.mesh = Mesh;
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
            if(_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
            if(_assembly != null && !_assembly.Preview)
            {
                _meshRenderer.enabled = false;
                return;
            }

            if(Mesh == null || Materials.Length == 0 || !enabled || !gameObject.activeInHierarchy)
            {
                _meshRenderer.enabled = false;
                return;
            }

            _meshRenderer.enabled = true;
        }
        #endif
    }
}