using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Puyo : MonoBehaviour
{
    //public PuyoType type;


    public Vector2Int fieldPosition;
    public PuyoFieldStatus puyoType;

    private void Start()
    {
        var color = Color.white;
        switch (puyoType)
        {
            case PuyoFieldStatus.RedPuyo:
                color = Color.red;
                break;
            case PuyoFieldStatus.BluePuyo:
                color = Color.blue;
                break;
            case PuyoFieldStatus.GreenPuyo:
                color = Color.green;
                break;
            case PuyoFieldStatus.YellowPuyo:
                color = Color.yellow;
                break;
            default:
                throw new NotImplementedException();
        }

        var renderer = GetComponentInChildren<Renderer>();
        renderer.material.color = color;

    }

    public void MoveTo(int x, int y)
    {
        MoveTo(new Vector2Int(x, y));
    }
    public void MoveTo(Vector2Int dst)
    {
        this.fieldPosition = dst;
        var pos = new Vector3(dst.x, dst.y, -0.1f);
        transform.position = pos;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
