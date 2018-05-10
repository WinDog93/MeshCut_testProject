using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingLine : MonoBehaviour
{
    private Vector3 startPoint, endPoint, position;
    Plane _plane;

    private void MouseBtnDwn()
    {
        Vector3 startMousePos = Input.mousePosition;
        startMousePos.z = 10.0f;
        startPoint = Camera.main.ScreenToWorldPoint(startMousePos);
    }

    private void MouseBtnUp()
    {
        Vector3 endMousePos = Input.mousePosition;
        endMousePos.z = 10.0f;
        endPoint = Camera.main.ScreenToWorldPoint(endMousePos);
        position = (startPoint + endPoint) / 2;
        _plane = GetPlane(position);
        FindCuttingMesh(_plane);
    }

    private Plane GetPlane(Vector3 pos)
    {
        Plane _plane;
        Vector3 normal;
        _plane = new Plane();
        Vector3 p1 = startPoint - pos;
        float deg = Mathf.Atan2(pos.y, pos.z) * Mathf.Rad2Deg - 90;
        normal = (Quaternion.Euler(0, 0f, 90f) * p1).normalized;
        DrawPlane(pos, normal);
        Debug.DrawLine(startPoint, endPoint, Color.green, 100);
        //print("Deg " + (Mathf.Atan2(pos.y, pos.z) * Mathf.Rad2Deg - 90));
        _plane.SetNormalAndPosition(normal, pos);

        return _plane;
    }

    private void FindCuttingMesh(Plane plane)
    {
        RaycastHit hit;
        var screenPos = Camera.main.WorldToScreenPoint(position);
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            //Debug.DrawLine(position, hit.transform.position, Color.red, 100);
            CuttingMesh cutMesh = hit.collider.gameObject.GetComponent<CuttingMesh>();
            if (cutMesh != null)
            {
                cutMesh.Cut(plane);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MouseBtnDwn();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            MouseBtnUp();
        }

    }

    void DrawPlane(Vector3 position, Vector3 normal)
    {

        Vector3 v3;

        if (normal.normalized != Vector3.forward)
            v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        else
            v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;

        var corner0 = position + v3;
        var corner2 = position - v3;
        var q = Quaternion.AngleAxis(90f, normal);
        v3 = q * v3;
        var corner1 = position + v3;
        var corner3 = position - v3;

        Debug.DrawLine(corner0, corner2, Color.green, 100);
        Debug.DrawLine(corner1, corner3, Color.green, 100);
        Debug.DrawLine(corner0, corner1, Color.green, 100);
        Debug.DrawLine(corner1, corner2, Color.green, 100);
        Debug.DrawLine(corner2, corner3, Color.green, 100);
        Debug.DrawLine(corner3, corner0, Color.green, 100);
        Debug.DrawRay(position, normal, Color.red, 100);
    }
}
