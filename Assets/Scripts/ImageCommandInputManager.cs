using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ImageCommandInputManager : MonoBehaviour
{
    [Header("所有的图片指令（ScriptableObject）")]
    public List<ImageCommandSO> commands;

    [Header("当前使用的是第几个指令（仅调试用，可选）")]
    public int currentIndex = 0;

    [Header("用来显示图片的 UI Image")]
    public Image targetImage;

    [Header("事件")]
    public UnityEvent onAllKeysPressed;
    public UnityEvent onWrongKey;

    private HashSet<KeyCode> requiredKeys;
    private HashSet<KeyCode> pressedKeys;

    private int lastIndex = -1;   // 记录上一次用的是哪个索引，用来避免连抽同一张

    private void Start()
    {
        LoadRandomCommand();
    }

    private void Update()
    {
        DetectKeyInput();
    }

    /// <summary>
    /// 从列表中随机选择一个指令（尽量避免和上一次一样）
    /// </summary>
    public void LoadRandomCommand()
    {
        if (commands == null || commands.Count == 0)
        {
            Debug.LogError("ImageCommandInputManager: commands 列表为空！");
            return;
        }

        int index;

        if (commands.Count == 1)
        {
            index = 0;
        }
        else
        {
            // 随机选一个 index，如果等于 lastIndex 就再抽一次
            do
            {
                index = Random.Range(0, commands.Count);
            } while (index == lastIndex);
        }

        lastIndex = index;
        LoadCommand(index);
    }

    /// <summary>
    /// 载入某个具体 index 的指令
    /// </summary>
    public void LoadCommand(int index)
    {
        if (index < 0 || index >= commands.Count)
        {
            Debug.LogError("ImageCommandInputManager: index 超出范围！");
            return;
        }

        currentIndex = index;
        var cmd = commands[currentIndex];

        requiredKeys = new HashSet<KeyCode>(cmd.requiredKeys);
        pressedKeys = new HashSet<KeyCode>();

        if (targetImage != null)
        {
            targetImage.sprite = cmd.imageSprite;
        }

        Debug.Log($"Commandloaded{cmd.commandName},Key Needed:{requiredKeys.Count}");
    }

    /// <summary>
    /// 检测键盘输入（顺序无所谓）
    /// </summary>
    private void DetectKeyInput()
    {
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key))
                continue;

            if (requiredKeys != null && requiredKeys.Contains(key))
            {
                if (!pressedKeys.Contains(key))
                {
                    pressedKeys.Add(key);
                    Debug.Log($"Correct Key Clicked{key}");

                    if (pressedKeys.SetEquals(requiredKeys))
                    {
                        Debug.Log("All required key clicked");
                        onAllKeysPressed?.Invoke();
                    }
                }
            }
            else
            {
                Debug.Log($"Wrong key clicked{key}");
                onWrongKey?.Invoke();
            }
        }
    }

    /// <summary>
    /// 如果你想手动重置当前进度（清空已按的正确键）
    /// </summary>
    public void ResetCurrentCommandProgress()
    {
        if (requiredKeys != null)
            pressedKeys = new HashSet<KeyCode>();
    }
}
