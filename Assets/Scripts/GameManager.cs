using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;




//左矢印キー: 左側へ一つ移動
//右矢印キー: 右側へ一つ移動
//下矢印キー: 下側へ一つ移動　自動で移動するのは、手動で移動した際にリセットした方がいのか？
//Zキー: 時計回りに回転
//Xキー: 反時計回りに回転


public class PuyoPuyo
{
    public Puyo Center; //中心のぷよ
    public Puyo Sub;  //まわりにあるぷよ
}

public class GameManager : MonoBehaviour
{
    public Vector2Int playerPosition;
    public Vector2Int subPosition;
    public Direction playerDir = Direction.Up;

    public PuyoGenerator generator;

    public PuyoPuyo current;
    public PuyoPuyo next;
    public PuyoPuyo nextNext;

    public Text finishText;


    //盤面の大きさは、6,12+1
    public int fieldWidth = 6 + 2; //+2はエリア外
    public int fieldHeight = 12 + 1 + 2; //+2はエリア外

    //fieldStatus[y,x]と　y,xの順 xは右方向が正、yは上方向が正
    public PuyoFieldStatus[,] fieldStatus;
    public Puyo[,] fieldPuyoRef;

    Coroutine puyoDrop;

    //ぷよが出てくる初期位置
    public Vector2Int initialPosition = new Vector2Int(2 + 1, 11 + 1);//0スタートで、エリア外があるのでx,yともに+1

    bool playerEnabled = true;
    bool isFinished = false;

    Coroutine finishCoroutine = null;

    #region Unityの関数
    // Start is called before the first frame update
    void Start()
    {
        this.finishText.gameObject.SetActive(false);

        CreateField();
        CreateNewPlayerPuyo();
        this.puyoDrop = StartCoroutine(FallPlayerPuyo());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        //ゲームオーバー時は
        if (this.isFinished)
        {
            if (this.finishCoroutine == null)
            {
                this.finishCoroutine = StartCoroutine(Finish());
            }
            return;
        }


        if (this.playerEnabled)
        {
            PlayerInput();
        }
        var subPuyoPos = this.subPosition;

        var statusUpdated = false;
        var centerCanMove = CanMovePuyo(this.playerPosition, Direction.Down);
        if (this.current.Center != null && !centerCanMove)
        {
            //フィールドの状態変更
            this.fieldStatus[this.playerPosition.y, this.playerPosition.x] = this.current.Center.puyoType;
            //puyoの参照変更
            this.fieldPuyoRef[this.playerPosition.y, this.playerPosition.x] = this.current.Center;
            this.current.Center = null;

            this.playerEnabled = false;

            statusUpdated = true;
        }
        if (this.current.Sub != null && !CanMovePuyo(subPuyoPos, Direction.Down))
        {

            this.fieldStatus[subPuyoPos.y, subPuyoPos.x] = this.current.Sub.puyoType;
            this.fieldPuyoRef[subPuyoPos.y, subPuyoPos.x] = this.current.Sub;
            this.current.Sub = null;

            this.playerEnabled = false;

            statusUpdated = true;

        }

        if (statusUpdated)
        {
            UpdatePuyoFieldStatus();
        }

        if (this.current.Center == null && this.current.Sub == null)
        {
            CreateNewPlayerPuyo();
            this.playerEnabled = true;
        }
        this.isFinished = CheckGameOver();

    }
    #endregion

    /// <summary>
    /// 終了のテキスト表示してシーン遷移する
    /// </summary>
    /// <returns></returns>
    IEnumerator Finish()
    {

        //終了のテキストを表示
        this.finishText.gameObject.SetActive(true);
        var transparent = finishText.color;
        transparent.a = 0;
        finishText.color = transparent;

        for (var alpha = 0f; alpha <= 1; alpha += 0.01f)
        {
            var c = finishText.color;
            c.a = alpha;
            finishText.color = c;
            yield return new WaitForSeconds(0.03f);
        }

        yield return new WaitForSeconds(3);

        //タイトルへ移動
        SceneManager.LoadScene("Title");
    }

    void CreateField()
    {
        this.fieldStatus = new PuyoFieldStatus[this.fieldHeight, this.fieldWidth];
        this.fieldPuyoRef = new Puyo[this.fieldHeight, this.fieldWidth];

        for (var y = 0; y < this.fieldStatus.GetLength(0); y++)
        {
            for (var x = 0; x < this.fieldStatus.GetLength(1); x++)
            {
                //フィールド外
                if (y == 0 || y == fieldStatus.GetLength(0) - 1 ||
                    x == 0 || x == fieldStatus.GetLength(1) - 1)
                {
                    this.fieldStatus[y, x] = PuyoFieldStatus.OutOfField;
                    continue;
                }
                //何もないところ
                this.fieldStatus[y, x] = PuyoFieldStatus.None;
            }
        }
    }

    /// <summary>
    /// プレイヤーのぷよを自動で落下させる
    /// </summary>
    /// <returns></returns>
    IEnumerator FallPlayerPuyo()
    {
        var delta = Direction.Down.ToVector2Int();

        while (true)
        {
            var subPos = this.subPosition;

            var canMoveCenterPuyo = this.current.Center != null && CanMovePuyo(this.playerPosition, Direction.Down);
            var canMoveSubPuyo = this.current.Sub != null && CanMovePuyo(subPos, Direction.Down);

            if (canMoveCenterPuyo)
            {
                var dst = this.playerPosition + delta;
                this.playerPosition = dst;
                this.current.Center.MoveTo(dst);
            }

            if (canMoveSubPuyo)
            {
                var dst = subPos + delta;
                this.subPosition = dst;
                this.current.Sub.MoveTo(dst);
            }

            if (!canMoveCenterPuyo && !canMoveSubPuyo)
            {
                this.playerEnabled = true;
            }

            yield return new WaitForSeconds(2);

        }
    }

    /// <summary>
    /// プレイヤーのぷよを生成する。
    /// </summary>
    void CreateNewPlayerPuyo()
    {
        this.playerPosition = this.initialPosition;

        if (this.next == null)
        {
            UpdateNextPuyoPuyo();
        }

        this.current = this.next;
        UpdateNextPuyoPuyo();

        var subPos = this.playerPosition + Direction.Up.ToVector2Int();
        this.subPosition = subPos;

        this.current.Center.MoveTo(this.playerPosition);
        this.current.Sub.MoveTo(subPos);

        playerDir = Direction.Up;
    }


    /// <summary>
    /// プレイヤーのぷよが置かれている位置をチェックして、ゲームオーバーかを判定する
    /// </summary>
    /// <returns>ゲームオーバーかどうか</returns>
    bool CheckGameOver()
    {
        var x = initialPosition.x;
        var y = initialPosition.y;
        if (ExistsPuyo(x, y))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// フィールド上の(x,y)にぷよが置かれているかどうか取得するメソッド
    /// </summary>
    /// <param name="x">x座標</param>
    /// <param name="y">y座標</param>
    /// <returns>(x,y)にぷよが存在するかどうか</returns>
    bool ExistsPuyo(int x, int y)
    {
        if (this.fieldStatus[y, x] == PuyoFieldStatus.OutOfField ||
            this.fieldStatus[y, x] == PuyoFieldStatus.None)
        {
            return false;
        }
        return true;
    }


    void UpdateNextPuyoPuyo()
    {
        if (this.nextNext == null)
        {
            UpdateNextNextPuyoPuyo();
        }
        this.next = this.nextNext;

        this.next.Center.MoveTo(new Vector2Int(9, 10));
        this.next.Sub.MoveTo(new Vector2Int(9, 11));

        UpdateNextNextPuyoPuyo();
    }
    void UpdateNextNextPuyoPuyo()
    {
        this.nextNext = new PuyoPuyo()
        {
            Center = this.generator.GenerateRandomPuyo(new Vector2Int(10, 7)),
            Sub = this.generator.GenerateRandomPuyo(new Vector2Int(10, 8))
        };
    }

    /// <summary>
    /// プレイヤーの入力をうけつけて、ぷよを移動させる
    /// </summary>
    void PlayerInput()
    {

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (CanMovePlayer(this.playerPosition, Direction.Left))
            {
                Move(Direction.Left);
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (CanMovePlayer(this.playerPosition, Direction.Right))
            {
                Move(Direction.Right);
            }

        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (CanMovePlayer(this.playerPosition, Direction.Down))
            {
                Move(Direction.Down);
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (CanRotate(this.playerDir, clockwise: true))
            {
                RotatePlayerPuyo(clockwise: true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            if (CanRotate(this.playerDir, clockwise: false))
            {
                RotatePlayerPuyo(clockwise: false);
            }
        }

    }

    /// <summary>
    /// centerとsub両方移動させる
    /// </summary>
    /// <param name="dir"></param>
    void Move(Direction dir)
    {
        var delta = dir.ToVector2Int();
        this.playerPosition = this.playerPosition + delta;
        this.current.Center.MoveTo(playerPosition);

        var subPos = this.playerPosition + this.playerDir.ToVector2Int();
        this.subPosition = subPos;
        this.current.Sub.MoveTo(subPos);
    }

    
    bool CanMovePuyo(Vector2Int currentPosition, Direction direction)
    {
        var delta = direction.ToVector2Int();
        var dstPosition = currentPosition + delta;
        if (this.fieldStatus[dstPosition.y, dstPosition.x] == PuyoFieldStatus.None)
        {
            return true;
        }
        return false;
    }

    //center,sub両方移動できるか
    bool CanMovePlayer(Vector2Int playerPosition, Direction dir)
    {
        var centerPos = playerPosition;
        var subPos = playerPosition + this.playerDir.ToVector2Int();

        var delta = dir.ToVector2Int();

        //移動後の中心ぷよと周りのぷよの位置
        var targetCenterPos = centerPos + delta;
        var targetSubPos = subPos + delta;


        if (fieldStatus[targetCenterPos.y, targetCenterPos.x] == PuyoFieldStatus.None &&
            fieldStatus[targetSubPos.y, targetSubPos.x] == PuyoFieldStatus.None
            )
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// プレイヤーのぷよを回転させる。centerPuyoを中心にしてsubPuyoを回転させる。
    /// </summary>
    /// <param name="clockwise">時計回りの時はtrue、反時計回りはfalse。</param>
    void RotatePlayerPuyo(bool clockwise)
    {
        this.playerDir = GetRotatedDir(this.playerDir, clockwise);
        var pos = this.playerPosition + this.playerDir.ToVector2Int();
        this.current.Sub.MoveTo(pos);
        this.subPosition = pos;
    }

    //上から右への回転のように45度回転下方向を取得する関数
    Direction GetRotatedDir(Direction current, bool clockwise)
    {
        if (clockwise)
        {
            switch (this.playerDir)
            {
                case Direction.Up:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Left;
                case Direction.Left:
                    return Direction.Up;
                default:
                    break;
            }

        }
        else
        {
            switch (this.playerDir)
            {
                case Direction.Up:
                    return Direction.Left;
                case Direction.Right:
                    return Direction.Up;
                case Direction.Down:
                    return Direction.Right;
                case Direction.Left:
                    return Direction.Down;
                default:
                    break;
            }
        }
        throw new NotImplementedException();
    }

    bool CanRotate(Direction currentDir, bool clockwise)
    {
        var positionAfterRotation = GetPositionAfterRotation(clockwise);

        if (this.fieldStatus[positionAfterRotation.y, positionAfterRotation.x] == PuyoFieldStatus.None ||
            this.fieldStatus[positionAfterRotation.y, positionAfterRotation.x] == PuyoFieldStatus.None)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 回転後の方向を取得
    /// </summary>
    /// <param name="clockwise"></param>
    /// <returns></returns>
    Vector2Int GetPositionAfterRotation(bool clockwise)
    {
        //回転後の方向
        var newDir = GetRotatedDir(this.playerDir, clockwise);
        var delta = newDir.ToVector2Int();
        var center = this.playerPosition;
        var newPos = center + delta;

        return newPos;
    }

    /// <summary>
    /// 連結しているぷよを削除する
    /// </summary>
    /// <returns>削除したかどうか</returns>
    bool RemoveConnectedPuyoPuyo()
    {
        //探索済みかどうか 初期値false
        var searched = new bool[this.fieldStatus.GetLength(0), this.fieldStatus.GetLength(1)];

        var removed = false;
        for (var y = 0; y < this.fieldStatus.GetLength(0); y++)
        {
            for (var x = 0; x < this.fieldStatus.GetLength(1); x++)
            {
                //探索済みならとばす
                if (searched[y, x])
                {
                    continue;
                }

                var stat = this.fieldStatus[y, x];
                //エリア外、何もなしの時は終了
                if (stat == PuyoFieldStatus.OutOfField ||
                    stat == PuyoFieldStatus.None)
                {
                    searched[y, x] = true;
                    continue;
                }

                if (stat == PuyoFieldStatus.RedPuyo ||
                   stat == PuyoFieldStatus.BluePuyo ||
                   stat == PuyoFieldStatus.GreenPuyo ||
                   stat == PuyoFieldStatus.YellowPuyo)
                {
                    var posList = new List<Vector2Int>();
                    var num = CountNumConnectedPuyo(x, y, stat, searched, posList);
                    if (num >= 4)
                    {

                        RemovePuyo(posList);
                        removed = true;

                    }


                }
            }
        }
        return removed;
    }

    //ぷよぷよを消した後、空中に浮いたぷよを落とす処理
    
    void DropPuyoPuyo()
    {

        //縦一列ずつ　下側から処理していく 左端はエリア外なので飛ばす
        for (var x = 1; x < this.fieldStatus.GetLength(1) - 1; x++)
        {
            var freeY = -1;//一番下の空いている位置。初期値-1は全部詰まっていて下におろすぷよがない状態
                           // y=1からスタート　y=0はエリア外なので
            for (var y = 1; y < this.fieldStatus.GetLength(0); y++)
            {
                var stat = this.fieldStatus[y, x];
                //上側のエリア外に当たると終了
                //左右のエリア外の壁も終了する
                if (stat == PuyoFieldStatus.OutOfField)
                {
                    break;
                }
                else if (stat == PuyoFieldStatus.None)
                {
                    if (freeY == -1)
                    {
                        freeY = y;
                    }

                    continue;
                }
                else if (stat == PuyoFieldStatus.RedPuyo ||
                   stat == PuyoFieldStatus.BluePuyo ||
                   stat == PuyoFieldStatus.GreenPuyo ||
                   stat == PuyoFieldStatus.YellowPuyo)
                {
                    if (freeY == -1)
                    {
                        continue;
                    }

                    var puyo = this.fieldPuyoRef[y, x];
                    this.fieldStatus[y, x] = PuyoFieldStatus.None;
                    this.fieldStatus[freeY, x] = stat;
                    //object移動
                    puyo.MoveTo(new Vector2Int(x, freeY));

                    this.fieldPuyoRef[y, x] = null;
                    this.fieldPuyoRef[freeY, x] = puyo;

                    freeY++;
                    continue;
                }


            }
        }
    }

    /// <summary>
    /// ぷよ消したり、消した後のぷよ落としたりの処理
    /// </summary>
    void UpdatePuyoFieldStatus()
    {
        while (true)
        {
            var removed = RemoveConnectedPuyoPuyo();
            if (!removed)
            {
                break;
            }
            DropPuyoPuyo();
        }
    }

    /// <summary>
    /// ある位置のぷよの連結しているぷよの数を数える
    /// </summary>
    /// <param name="x">初めのぷよのx座標</param>
    /// <param name="y">初めのぷよのy座標</param>
    /// <param name="status">連結もとのぷよの状態</param>
    /// <param name="searched">探索済みかどうかの2次元配列</param>
    /// <param name="positionList">連結しているぷよの座標を格納するリスト</param>
    /// <returns></returns>
    int CountNumConnectedPuyo(int x, int y, PuyoFieldStatus status, bool[,] searched, List<Vector2Int> positionList)
    {
        if (searched[y, x])
        {
            return 0;
        }

        var stat = this.fieldStatus[y, x];
        if (stat == PuyoFieldStatus.None ||
            stat == PuyoFieldStatus.OutOfField)
        {
            searched[y, x] = true;
            return 0;
        }
        if (stat == PuyoFieldStatus.RedPuyo ||
           stat == PuyoFieldStatus.BluePuyo ||
           stat == PuyoFieldStatus.GreenPuyo ||
           stat == PuyoFieldStatus.YellowPuyo)
        {
            if (stat != status)
            {
                return 0;
            }

            searched[y, x] = true;
            positionList.Add(new Vector2Int(x, y));

            //上下左右のマスで再帰
            var right = CountNumConnectedPuyo(x + 1, y, status, searched, positionList);
            var up = CountNumConnectedPuyo(x, y + 1, status, searched, positionList);
            var left = CountNumConnectedPuyo(x - 1, y, status, searched, positionList);
            var down = CountNumConnectedPuyo(x, y - 1, status, searched, positionList);

            return right + up + left + 1;
        }

        throw new Exception("");
    }

    void RemovePuyo(List<Vector2Int> positionList)
    {
        foreach (var pos in positionList)
        {
            this.fieldStatus[pos.y, pos.x] = PuyoFieldStatus.None;
            var puyo = this.fieldPuyoRef[pos.y, pos.x];
            puyo.gameObject.SetActive(false);
            this.fieldPuyoRef[pos.y, pos.x] = null;
        }
    }
}
