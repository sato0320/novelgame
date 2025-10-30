using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
//TextMeshProを使う時に必要な機能
using TMPro;
using UnityEngine.UIElements;
using UnityEngine.Assertions.Must;
using Unity.VisualScripting;

public class csvReader : MonoBehaviour
{
    //読み込む元のCSVファイル
    private TextAsset _csvFile;
    //CSVの文章を入れるためのリスト
    private List<string[]> _csvDataList = new List<string[]>();

    //現在表示中の文字列
    private int _messageCount = 0;
    private int _rowsCount = 0;
    //経過時間を確認する
    private float _countTime = 0f;

    [Header("メッセージの文字送り時間を変更する変数")]
    //メッセージが停止するまでの時間
    [SerializeField]
    private float _messageStopTime = 0.1f;

    [Header("名前表示用のTextMeshProUGUIを入れる変数")]
    //キャラクターの名前を表示させるTextボックス
    [SerializeField]
    private TextMeshProUGUI _nameText;

    [Header("メッセージ表示用のTextMeshProUGUIを入れる変数")]
    //キャラクターのセリフを表示させるTextボックス
    [SerializeField]
    private TextMeshProUGUI _messageText;

    [Header("選択肢を表示させるPrefabを入れる変数")]
    //選択肢表示用のオブジェクトを入れる変数
    [SerializeField]
    private GameObject _selectCommandObject;

    //メッセージを止めるためのフラグ
    private bool _messageStopFlag = false;

    //コマンドチェックフラグ
    private bool _commandCheckFlag = false;

    //選択肢表示用の配列
    private List<string[]> _getSelectCommand = new List<string[]>();

    [Header("シナリオが終了したかの判定を行うフラグ")]
    //シナリオエンドフラッグ
    public bool ScenarioEndFlag = false;

    [SerializeField] UnityEngine.UI.Image[] _backgrounds;
    [SerializeField] UnityEngine.UI.Image[] _characters;

    private void Start()
    {
        //csvファイルをstring形式で読み込む

        //Resources : resourcesフォルダの中にあるデータを取得するための処理
        //Load : データを変数に格納すること
        //Resources.Load : resourcesフォルダの中にあるデータを取得して変数に格納すること

        //_csvFileという名前の変数にResourcesフォルダの中にある"testCSV"という名前のデータ(TextAsset型)を取得して格納する
        _csvFile = Resources.Load("ScenarioCSV") as TextAsset; // Resouces下のCSV読み込み

        //unity上で扱いやすい形に変形する
        StringReader reader = new StringReader(_csvFile.text);

        // , で分割しつつ一行ずつ読み込み
        // リストに追加していく
        while (reader.Peek() != -1) // reader.Peaekが-1になるまで
        {
            string line = reader.ReadLine(); // 一行ずつ読み込み
            _csvDataList.Add(line.Split(',')); // , 区切りでリストに追加
        }

        //csvDatas[行][列]を指定して値を自由に取り出せる
        Debug.Log(_csvDataList[0][0]);
        Debug.Log(_csvDataList[0][1]);
        Debug.Log(_csvDataList[0][4]);
    }

    //文字を一文字ずつ表示する
    //Updateとは
    private void Update()
    {
        //該当のシナリオが読み込み終わったらこれ以上処理を行わない
        if (ScenarioEndFlag)
        {
            return;
        }
        
        //行の頭にコマンドがあるか確認する
        if (_commandCheckFlag == false)
        {
            GetCommand();
        }

        //メッセージ表示が完全に終了しているかつクリックされたら次のテキストに変更する
        //表示テキスト以上になった場合は表示処理を止める
        if (_rowsCount >= _csvDataList.Count || _messageCount >= _csvDataList[_rowsCount][2].Length || _messageStopFlag)
        {
            return;
        }

        //名前の表示処理を呼び出す
        NameTextView();

        //メッセージ表示処理を呼び出す
        MessageTextView();

        //背景
        Background();

        //キャラ
        CharacterImage();
    }

    //名前表示用の処理
    private void NameTextView()
    {
        _nameText.text = _csvDataList[_rowsCount][1];
    }

    //メッセージ表示用の処理
    private void MessageTextView()
    {
        //一文字ずつ文字を表示させる
        if (_messageStopTime <= _countTime)
        {
            _countTime = 0f;
            _messageText.text += _csvDataList[_rowsCount][2][_messageCount];
            _messageCount++;
        }

        //文字送り用に秒数を数える
        _countTime += Time.deltaTime;
    }

    //Commandが書かれているか判断を行う
    private void GetCommand()
    {
        //もしもコマンドがあった場合、選択肢表示をする
        string commandCheck = _csvDataList[_rowsCount][0];

        //コマンドが入力されているか確認する
        switch (commandCheck)
        {
            //選択肢を2つ表示させる
            case "Select2":
                SelectCommand(2);
                break;
            
            //指定のシナリオまで飛ぶ
            case "JumpCommand":
                JumpMessageRow(_csvDataList[_rowsCount][1]);
                break;

            //シナリオが終わったら処理を止める
            case "Scenario_End":
                ScenarioEndFlag = true;
                break;
            
            //もしコマンドじゃなかったらリセットする
            default:
                //メッセージテキストの表示を空白に変更する
                _messageText.text = "";
                break;
        }
        //コマンドの確認を終えたのでFlagをtrueにする
        _commandCheckFlag = true;
    }

    //選択肢を表示させるコマンドの処理
    private void SelectCommand(int SelectCommandValue)
    {
        //SelectCommandObjectの子オブジェクトの選択肢を引数nつを取得して表示させる
        //メッセージがこれ以上流れないようにフラグをtrueに変更する
        _messageStopFlag = true;

        _getSelectCommand.Clear();

        //子オブジェクトを一度全て非表示にする
        foreach (Transform child in _selectCommandObject.transform)
        {
            child.gameObject.SetActive(false);
        }

        //指定の数の子オブジェクトを取得する
        for (var i = 0; i < SelectCommandValue; ++i)
        {
            //一つ下の段を見る
            _rowsCount++;
            var selectObject = _selectCommandObject.transform.GetChild(i);
            selectObject.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = _csvDataList[_rowsCount][1];
            _getSelectCommand.Add(_csvDataList[_rowsCount]);
            selectObject.gameObject.SetActive(true);
        }
        //表示させた
        _selectCommandObject.SetActive(true);
    }

    //選択肢の中から選択後のメッセージ先に飛ぶ
    private void JumpMessageRow(string SelectCommandValue)
    {
        //JumpCommandの飛び先を探したいので_Startを付けた文字列で検索する
        var jumpCommand = SelectCommandValue + "_Start";

        //JumpCommand_A
        for (var i = 0; i < _csvDataList.Count; ++i)
        {
            if (_csvDataList[i][0] == jumpCommand)
            {
                _rowsCount = i;
            }
        }
        _rowsCount += 1;
        Debug.Log(_rowsCount);
        _messageStopFlag = false;
        CommandClear();
    }
    
    //Commandをクリアする処理
    private void CommandClear()
    {
        _commandCheckFlag = false;
        _selectCommandObject.SetActive(false);
        _messageText.text = "";
    }

    //Unityデフォルトのボタンなどでクリックされた時にこの関数を呼べば次の行を表示できる
    public void NextMessageView()
    {
        //もし文字が表示と途中なら最後まで表示させる
        if(_messageCount < _csvDataList[_rowsCount][2].Length)
        {
            _messageText.text = _csvDataList[_rowsCount][2];
            _messageCount = _csvDataList[_rowsCount][2].Length;
            return;
        }

        //読みこんだメッセージの行以下ならカウントを追加する
        if (_rowsCount >= _csvDataList.Count || _messageStopFlag)
        {
            return;
        }
        //次の行に移る
        _rowsCount++;
        //Messageを先頭から出す
        _messageCount = 0;
        //次のフラグを確認する
        _commandCheckFlag = false;
    }

    //選択肢が押された時の挙動を登録する
    public void SelectCommandReturnValue(int SelectCommandValue)
    {
        Debug.Log(_getSelectCommand[SelectCommandValue][3]);
        //押されたボタンの位置を情報として表示する
        //押されたボタンの位置を情報として渡す
        JumpMessageRow(_getSelectCommand[SelectCommandValue][3]);
    }

    void Background()
    {
        string backgroundCheck = _csvDataList[_rowsCount][4];

        switch (backgroundCheck)
        {
            case "groundA":
                Backgroundfalse();
                _backgrounds[0].enabled = true;
                break;
            case "groundB":
            Backgroundfalse();
                _backgrounds[1].enabled = true;
                break;
        }

    }

    void Backgroundfalse()
    {
        _backgrounds[0].enabled = false;
        _backgrounds[1].enabled = false;
    }
    
    void CharacterImage()
    {
        string characterCheck = _csvDataList[_rowsCount][1];

        switch (characterCheck)
        {
            case "？？？":
                Characterfalse();
                _characters[0].color = Color.white;
                break;
            case "フィオーレ":
                Characterfalse();
                _characters[1].color = Color.white;
                break;
            case "ステラ":
                Characterfalse();
                _characters[0].color = Color.white;
                break;
            case "":
                Characterfalse();
                break;
        }
    }
    
    void Characterfalse()
    {
        _characters[0].color = Color.gray;
        _characters[1].color = Color.gray;
    }
}