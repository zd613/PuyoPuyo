using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuyoGenerator : MonoBehaviour
{
    public GameObject puyoPrefab;


    // Start is called before the first frame update
    void Start()
    {

    }

    public Puyo GenerateRedPuyo(Vector2Int position)
    {
        var type = PuyoFieldStatus.RedPuyo;
        return Generate(type, position);
    }

    public Puyo GenerateRandomPuyo(Vector2Int position)
    {
        var type = RandomPuyoType();
        return Generate(type, position);
    }

    public Puyo Generate(PuyoFieldStatus puyoType, Vector2Int position)
    {
        var obj = Instantiate(puyoPrefab);
        var puyo = obj.GetComponent<Puyo>();
        puyo.puyoType = puyoType;
        puyo.MoveTo(position);

        return puyo;
    }

    PuyoFieldStatus RandomPuyoType()
    {
        var value = UnityEngine.Random.Range(0, 4);//ぷよ　4種類
        switch (value)
        {
            case 0:
                return PuyoFieldStatus.RedPuyo;
            case 1:
                return PuyoFieldStatus.BluePuyo;
            case 2:
                return PuyoFieldStatus.GreenPuyo;
            case 3:
                return PuyoFieldStatus.YellowPuyo;
        }
        throw new NotImplementedException();
    }

}
