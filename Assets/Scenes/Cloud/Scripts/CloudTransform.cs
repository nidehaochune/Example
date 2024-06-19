using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CloudTransform : MonoBehaviour
{
    public Image Image;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green * new Color(0.1f,0.1f,0.1f,0.1f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }


}
