using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Rock : MonoBehaviour
{
    void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Obstacle");
    }
}
