using UnityEngine;

namespace RoomBuilder
{
    [CreateAssetMenu(menuName = "RoomBuilder/Assembly Parameters")]
    public sealed class AssemblyParameters : ScriptableObject
    {
        [field: SerializeField] public string BuildResultPath {get; set;} = "RoomBuilderMeshes";
    }    
}
