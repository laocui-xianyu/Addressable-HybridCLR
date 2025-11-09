using System;
using UnityEngine;

namespace HotUpdate
{
    public class Move : MonoBehaviour
    {
        private Material _material;
        [SerializeField] private float moveSpeed = 5f;
        private Rigidbody _rb;
        

        private void Awake()
        {
            _material = GetComponent<MeshRenderer>().material;
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
        }
        private void FixedUpdate()
        {
            Vector3 direction = MoveInput();
            _rb.velocity = new Vector3(direction.x * moveSpeed, _rb.velocity.y, direction.z * moveSpeed);
        }

        private Vector3 MoveInput()
        {
            float directionX = Input.GetAxis("Horizontal");
            float directionZ = Input.GetAxis("Vertical");
            return new Vector3(directionX, 0f, directionZ).normalized;
        }
    }
}