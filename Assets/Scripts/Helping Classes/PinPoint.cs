using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinPoint : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private Color _color;
    [SerializeField] private float _radius;
    [SerializeField] private bool showGizmos = true;

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = _color;
            Gizmos.DrawSphere(this.transform.position, _radius);
        }
    }
#endif
}
