using UnityEngine;

namespace BestoNetSamples.Environment
{
    [RequireComponent(typeof(MeshRenderer))]
    public class GridAnimator : MonoBehaviour
    {
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float minEmission = 1f;
        [SerializeField] private float maxEmission = 3f;
    
        private Material _material;
        private static readonly int GridEmission = Shader.PropertyToID("_GridEmission");

        private void Awake()
        {
            // Get a local copy of the material to avoid affecting other objects using the same material
            _material = GetComponent<MeshRenderer>().material;
        }

        private void Update()
        {
            // Smoothly animate the grid emission
            float emission = Mathf.Lerp(minEmission, maxEmission,(Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            _material.SetFloat(GridEmission, emission);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}