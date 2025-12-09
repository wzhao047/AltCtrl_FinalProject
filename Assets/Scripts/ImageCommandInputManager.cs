using System.Collections;
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
    public UnityEvent onAllKeysPressed;  // 所有按键输入正确时触发（用来显示“继续转动”的提示）
    public UnityEvent onWrongKey;        // 错误按键时触发

    [Header("胜利相关（全部输入正确后，还需要摇鼠标）")]
    [Tooltip("是否在按对全部按键后，要求玩家继续转动鼠标一段时间才算胜利")]
    public bool requireMouseMoveAfterCorrect = true;

    [Tooltip("鼠标需要被持续移动的时间（秒），只在速度超过阈值时才会累积")]
    public float requiredMoveTime = 1.5f;

    [Tooltip("判定为有效移动的最小鼠标速度（像素/秒）")]
    public float minMouseSpeed = 50f;

    [Tooltip("判定胜利后，等待多久再进入下一回合并随机抽取新的指令")]
    public float nextRoundDelay = 0.8f;

    [Header("全部完成后的事件（可选）")]
    [Tooltip("鼠标移动时间累积完成、玩家真正胜利时触发")]
    public UnityEvent onWin;

    [Header("UI：鼠标进度条（0~1）")]
    [Tooltip("用来展示鼠标移动累积进度的 Slider，最小值设为 0，最大值设为 1")]
    public Slider mouseProgressSlider;

    [Header("鼠标速度影响进度的设置")]
    [Tooltip("勾上后，鼠标越快，进度条涨得越快")]
    public bool speedAffectsProgress = true;

    [Tooltip("速度转成进度的系数。值越大，快鼠标带来的加速越明显")]
    public float speedToProgressFactor = 0.01f;

    [Tooltip("进度条因速度加成产生的最大倍率，例如 3 表示最多 3 倍速涨")]
    public float maxProgressMultiplier = 3f;


    private HashSet<KeyCode> requiredKeys;
    private HashSet<KeyCode> pressedKeys;

    private int lastIndex = -1;   // 记录上一次用的是哪个索引，用来避免连抽同一张

    // 鼠标判定相关
    private bool allKeysCorrect = false;   // 是否已经进入“鼠标阶段”
    private float moveAccumulated = 0f;    // 累积的有效鼠标移动时间
    private Vector3 lastMousePos;
    private bool isHandlingNextRound = false;

    private void Start()
    {
        LoadRandomCommand();
        lastMousePos = Input.mousePosition;
        UpdateProgressUI(0f, false);
    }

    private void Update()
    {
        DetectKeyInput();

        if (requireMouseMoveAfterCorrect && allKeysCorrect)
        {
            UpdateMouseMove();
        }
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

        allKeysCorrect = false;
        moveAccumulated = 0f;
        UpdateProgressUI(0f, false);

        if (targetImage != null)
        {
            targetImage.sprite = cmd.imageSprite;
        }

        Debug.Log($"Command loaded {cmd.commandName}, Key Needed: {requiredKeys.Count}");
    }

    /// <summary>
    /// 检测键盘输入（顺序无所谓）
    /// </summary>
    private void DetectKeyInput()
    {
        // 如果已经进入“鼠标移动阶段”，就不再处理键盘输入
        if (requireMouseMoveAfterCorrect && allKeysCorrect)
            return;

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key))
                continue;

            if (requiredKeys != null && requiredKeys.Contains(key))
            {
                if (!pressedKeys.Contains(key))
                {
                    pressedKeys.Add(key);
                    Debug.Log($"Correct Key Clicked {key}");

                    if (pressedKeys.SetEquals(requiredKeys))
                    {
                        Debug.Log("All required keys clicked");
                        onAllKeysPressed?.Invoke();

                        if (requireMouseMoveAfterCorrect)
                        {
                            // 进入鼠标移动阶段
                            allKeysCorrect = true;
                            moveAccumulated = 0f;
                            lastMousePos = Input.mousePosition;
                            UpdateProgressUI(0f, true);  // 显示进度条，从 0 开始
                        }
                        else
                        {
                            // 不需要鼠标移动的话，直接进入下一回合
                            if (!isHandlingNextRound)
                                StartCoroutine(NextRoundRoutine());
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"Wrong key clicked {key}");
                onWrongKey?.Invoke();
            }
        }
    }

    /// <summary>
    /// 处理鼠标移动累积时间
    /// </summary>
    private void UpdateMouseMove()
    {
        if (isHandlingNextRound)
            return;

        Vector3 current = Input.mousePosition;
        float distance = Vector3.Distance(current, lastMousePos);
        float speed = 0f;

        if (Time.deltaTime > 0f)
        {
            speed = distance / Time.deltaTime;  // 像素/秒
        }

        lastMousePos = current;

        // 只有在鼠标移动速度超过阈值时才算作“有效移动”
        
        if (speed >= minMouseSpeed)
        {
            float multiplier = 1f;

            if (speedAffectsProgress)
            {
                // 把“超过阈值的程度”归一化到 0~1：
                // 速度 = 阈值      → normalized = 0  → 1 倍速
                // 速度 = 2*阈值    → normalized = 1  → maxProgressMultiplier 倍
                float normalizedSpeed = (speed - minMouseSpeed) / Mathf.Max(minMouseSpeed, 0.0001f);
                normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

                // 在线性插值 1 ~ maxProgressMultiplier 之间
                multiplier = Mathf.Lerp(1f, maxProgressMultiplier, normalizedSpeed);
            }

            moveAccumulated += Time.deltaTime * multiplier;

            //（可选）调试一下你现在的速度和倍率
            //Debug.Log($"speed={speed:F1}, multiplier={multiplier:F2}, moveAccumulated={moveAccumulated:F2}");
        }

        float t = Mathf.Clamp01(requiredMoveTime > 0f ? moveAccumulated / requiredMoveTime : 1f);
        UpdateProgressUI(t, true);

        if (moveAccumulated >= requiredMoveTime)
        {
            Debug.Log("Mouse moved enough time, player wins!");
            allKeysCorrect = false;
            moveAccumulated = 0f;
            UpdateProgressUI(1f, false);

            onWin?.Invoke();

            if (!isHandlingNextRound)
            {
                StartCoroutine(NextRoundRoutine());
            }
        }
    }


    /// <summary>
    /// 胜利后，等待一小段时间再进入下一回合
    /// </summary>
    private IEnumerator NextRoundRoutine()
    {
        isHandlingNextRound = true;

        yield return new WaitForSeconds(nextRoundDelay);

        LoadRandomCommand();
        lastMousePos = Input.mousePosition;

        isHandlingNextRound = false;
    }

    /// <summary>
    /// 如果你想手动重置当前进度（清空已按的正确键）
    /// </summary>
    public void ResetCurrentCommandProgress()
    {
        if (requiredKeys != null)
            pressedKeys = new HashSet<KeyCode>();

        allKeysCorrect = false;
        moveAccumulated = 0f;
        UpdateProgressUI(0f, false);
    }

    /// <summary>
    /// 更新 UI 进度条
    /// </summary>
    private void UpdateProgressUI(float normalizedValue, bool show)
    {
        if (mouseProgressSlider == null)
            return;

        mouseProgressSlider.minValue = 0f;
        mouseProgressSlider.maxValue = 1f;
        mouseProgressSlider.value = Mathf.Clamp01(normalizedValue);
        mouseProgressSlider.gameObject.SetActive(show);
    }
}
