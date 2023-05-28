using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ViewDistance : MonoBehaviour {

    public int segments = 50;
    public float xradius = 5f;
    public float yradius = 5f;
    LineRenderer line;

    void Start () {
        line = gameObject.GetComponent<LineRenderer>();

        line.SetVertexCount (segments + 1);
        line.useWorldSpace = false;

        float x;
        float y;
        float z;

        float angle = 20f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin (Mathf.Deg2Rad * angle) * xradius;
            y = Mathf.Cos (Mathf.Deg2Rad * angle) * yradius;

            line.SetPosition (i,new Vector3(x,y,0) );

            angle += (360f / segments);
        }
    }
    
}