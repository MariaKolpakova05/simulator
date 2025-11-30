using System;
using UnityEngine;
using System.Collections.Generic;

public class ForceVisualizer : MonoBehaviour
{
    [Header("Настройки визуализации")]
    [SerializeField] private bool _showForces = true;
    [SerializeField] private float _arrowScale = 0.1f;

    public List<Force> Forces = new List<Force>();

    public void AddForce(Vector3 force, Color color, string name)
    {
        Forces.Add(new Force(force, color, name));
    }

    //public void ClearForces() => _forces.Clear();

    public void OnDrawGizmos()
    {
        if (!_showForces || Forces == null) return;

        foreach (Force f in Forces)
        {
            Gizmos.color = f.Color;
            Vector3 start = transform.position;
            Vector3 end = start + f.Vector * _arrowScale;

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(end, Vector3.one * 0.1f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(end + Vector3.up * 0.1f, f.Name);
#endif
        }
    }
}

[Serializable]
public class Force
{
    public Vector3 Vector;
        public Color Color;
        public string Name;

        public Force(Vector3 v, Color c, string n)
        {
            Vector = v;
            Color = c;
            Name = n;
        }
}
