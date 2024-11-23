using BestoNet.Networking.Input;
using BestoNetSamples.BestoNet.Networking.Input;
using UnityEngine;

namespace BestoNetSamples
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private bool isLocal;
        [SerializeField] private float moveSpeed = 5;
        [SerializeField] private UnityNewInputProvider inputProvider;

        private ulong _lastProcessedInput;
        private Vector3 _currentPosition;
        
        private void Start()
        {
            _currentPosition = transform.position;
        }

        private void Update()
        {
            _lastProcessedInput = GetInput();
            Vector3 movement = Vector3.zero;

            if ((_lastProcessedInput & (ulong)InputFlag.Up) != 0)  movement.z += 1;
            if ((_lastProcessedInput & (ulong)InputFlag.Down) != 0) movement.z -= 1;
            if ((_lastProcessedInput & (ulong)InputFlag.Left) != 0) movement.x -= 1;
            if ((_lastProcessedInput & (ulong)InputFlag.Right) !=0) movement.x += 1;

            movement.Normalize();
            _currentPosition += movement * (moveSpeed * Time.deltaTime);  
            
            transform.position = _currentPosition;
        }

        public ulong GetInput()
        {
            return isLocal ?  inputProvider.GetInput() : 0;
        }
    }
}
