using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Direction
{
    Up,
    Right,
    Down,
    Left
}

static class DirectionExt
{
    public static Vector2Int ToVector2Int(this Direction diretion)
    {
        switch (diretion)
        {
            case Direction.Up:
                return new Vector2Int(0, 1);
            case Direction.Right:
                return new Vector2Int(1, 0);
            case Direction.Down:
                return new Vector2Int(0, -1);
            case Direction.Left:
                return new Vector2Int(-1, 0);
            default:
                throw new Exception("not found");
        }
    }
}