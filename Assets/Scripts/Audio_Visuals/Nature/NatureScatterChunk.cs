
using UnityEngine;

namespace Nature
{
    [AddComponentMenu("")]            // nascosto dal menu Add Component
    [DisallowMultipleComponent]
    public sealed class NatureScatterChunk : MonoBehaviour
    {
        [HideInInspector] public int cellX;
        [HideInInspector] public int cellY;
    }
}