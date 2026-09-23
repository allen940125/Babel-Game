#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CommandRunnerWindow : EditorWindow
{
    private GameCommandSO _selectedCommand;
    private Editor _cachedEditor;
    private Vector2 _scrollPos;

    // 搜尋與快取下拉選單
    private List<GameCommandSO> _allCommands = new List<GameCommandSO>();
    private string[] _commandNames = new string[0];
    private int _selectedIndex = -1;

    [MenuItem("Tools/Command Runner (SO 執行與編輯工具)")]
    public static void ShowWindow()
    {
        var window = GetWindow<CommandRunnerWindow>("Command Runner");
        window.minSize = new Vector2(350, 400);
    }

    private void OnEnable()
    {
        RefreshAssetList();
    }

    private void OnDisable()
    {
        // 釋放編輯器記憶體
        if (_cachedEditor != null)
        {
            DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }
    }

    private void RefreshAssetList()
    {
        _allCommands.Clear();
        string[] guids = AssetDatabase.FindAssets("t:GameCommandSO");
        List<string> names = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameCommandSO cmd = AssetDatabase.LoadAssetAtPath<GameCommandSO>(path);
            if (cmd != null)
            {
                _allCommands.Add(cmd);
                string label = string.IsNullOrEmpty(cmd.commandName) ? cmd.name : $"{cmd.name} ({cmd.commandName})";
                names.Add(label);
            }
        }

        _commandNames = names.ToArray();
        SyncSelectedIndex();
    }

    private void SyncSelectedIndex()
    {
        if (_selectedCommand != null)
        {
            _selectedIndex = _allCommands.IndexOf(_selectedCommand);
        }
        else
        {
            _selectedIndex = -1;
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(8);

        // ==========================================
        // 1. 選擇 SO 的區域 (支援 Object 拖曳與下拉選單)
        // ==========================================
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("選擇目標指令 (GameCommandSO)", EditorStyles.boldLabel);

        // 方式 A：拖曳物件欄位
        EditorGUI.BeginChangeCheck();
        var newTarget = (GameCommandSO)EditorGUILayout.ObjectField("指令資產", _selectedCommand, typeof(GameCommandSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            SelectCommand(newTarget);
        }

        // 方式 B：下拉選單選取
        if (_commandNames.Length > 0)
        {
            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUILayout.Popup("專案清單快選", _selectedIndex, _commandNames);
            if (EditorGUI.EndChangeCheck() && newIdx >= 0 && newIdx < _allCommands.Count)
            {
                SelectCommand(_allCommands[newIdx]);
            }
        }

        if (GUILayout.Button("重新掃描專案 SO 清單"))
        {
            RefreshAssetList();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // ==========================================
        // 2. 核心：即時繪製所選 SO 的內容與欄位填寫區
        // ==========================================
        if (_selectedCommand != null)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"正在編輯: {_selectedCommand.name}", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // 確保快取編輯器存在
            if (_cachedEditor == null || _cachedEditor.target != _selectedCommand)
            {
                if (_cachedEditor != null) DestroyImmediate(_cachedEditor);
                _cachedEditor = Editor.CreateEditor(_selectedCommand);
            }

            // 繪製目標 SO 的原生 Inspector 欄位 (包括 [SerializeReference] Action、itemId、count 等)
            _cachedEditor.OnInspectorGUI();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // ==========================================
            // 3. 執行指令按鈕
            // ==========================================
            GUI.backgroundColor = Application.isPlaying ? Color.green : Color.gray;
            GUI.enabled = Application.isPlaying;

            string btnText = Application.isPlaying ? $"▶ 執行 [{_selectedCommand.name}]" : "請先進入 Play Mode 才能執行";
            if (GUILayout.Button(btnText, GUILayout.Height(40)))
            {
                _selectedCommand.Execute();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("請先拖入或從下拉選單選擇一個 GameCommandSO。", MessageType.Info);
        }
    }

    private void SelectCommand(GameCommandSO cmd)
    {
        _selectedCommand = cmd;
        SyncSelectedIndex();

        if (_cachedEditor != null)
        {
            DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }

        Repaint();
    }
}
#endif