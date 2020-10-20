using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Field : MonoBehaviour
{
    public GameObject tile;

    // Start is called before the first frame update
    void Start()
    {
        var height = 13 + 2;
        var width = 6 + 2;
        for (var w = 0; w < width; w++)
        {
            for (var h = 0; h < height; h++)
            {
                var obj = Instantiate(tile, this.transform);
                obj.transform.position = new Vector3(w, h, 0);

                var renderer = obj.GetComponent<Renderer>();

                //エリア外の色変更
                if (h == 0 || h == height - 1 ||
                    w == 0 || w == width - 1)
                {
                    renderer.material.color = Color.gray;
                    continue;
                }

                if (h % 2 == 0)
                {
                    var c = new Color(170 / 255f, 170 / 255f, 255 / 255f, 1);

                    renderer.material.color = c;
                }
            }
        }
    }
}
