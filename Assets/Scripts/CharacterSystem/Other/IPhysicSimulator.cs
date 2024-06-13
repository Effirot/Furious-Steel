
using UnityEngine;

public interface IPhysicObject 
{
    public Vector3 velocity { get; set; }

    public float mass { get; set; }

    public float PhysicTimeScale { get; set; }

    public float GravityScale { get; set; }

    public void Push(Vector3 direction);
}