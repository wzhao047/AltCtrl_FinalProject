using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("系统引用")]
    public RecipeOrderController orderController;
    public GearTypeController gearTypeController;

    [Header("齿轮按键（ABC）")]
    public List<KeyCode> gearKeys = new List<KeyCode> { KeyCode.A, KeyCode.B, KeyCode.C };

    [Header("事件")]
    public UnityEvent<KeyCode> onLeftGearDetected;     // 记录“先离盒”的齿轮
    public UnityEvent<KeyCode> onRightGearDetected;    // 记录“后离盒”的齿轮
    public UnityEvent onLeftGearPlacedCorrect;
    public UnityEvent onRightGearPlacedCorrect;
    public UnityEvent onPlacedWrong;
    public UnityEvent onEnterMouseStage;
    public UnityEvent onRoundWin;

    [Header("胜利相关（放对齿轮后，要求持续移动鼠标）")]
    public bool requireMouseMoveAfterCorrect = true;
    public float requiredMoveTime = 1.5f;
    public float minMouseSpeed = 50f;
    public bool speedAffectsProgress = true;
    public float maxProgressMultiplier = 3f;
    public float nextRoundDelay = 0.8f;

    [Header("UI：鼠标进度条（0~1）")]
    public Slider mouseProgressSlider;
    
    [Header("UI：倒计时显示")]
    [Tooltip("倒计时文本（TextMeshProUGUI）")]
    public TextMeshProUGUI timerText;

    [Header("UI：胜利/失败面板")]
    [Tooltip("胜利面板（GameObject）")]
    public GameObject winPanel;
    
    [Tooltip("失败面板（GameObject）")]
    public GameObject failPanel;
    
    [Tooltip("超时面板（GameObject）")]
    public GameObject timeoutPanel;
    
    [Header("UI：计数面板")]
    [Tooltip("计数面板（GameObject）")]
    public GameObject countNumberPanel;
    
    [Tooltip("计数面板上的文本（TextMeshProUGUI）")]
    public TextMeshProUGUI countNumberText;

    [Header("回合结果设置")]
    [Tooltip("显示胜利/失败面板的时间（秒）")]
    public float resultPanelDisplayTime = 3f;
    
    [Header("倒计时设置")]
    [Tooltip("全局倒计时时间（秒）- 所有订单共享一个计时器")]
    public float globalTimeLimit = 30f;
    
    [Tooltip("计数面板显示时间（秒）")]
    public float countPanelDisplayTime = 5f;

    public enum GameState
    {
        WaitingForLeftGearPlacement,
        WaitingForRightGearPlacement,
        MouseStage,
        WinState,
        FailState,
        Transition
    }

    [Header("游戏状态")]
    public GameState currentState = GameState.WaitingForLeftGearPlacement;

    // 当前 recipe 需求
    private RecipeOrderController.RoundRecipe currentRecipe;

    // 盒子外的齿轮（按“松开”进入；按回去移除）
    private readonly List<KeyCode> currentOutOfBoxGears = new List<KeyCode>();
    private readonly Dictionary<KeyCode, bool> keyStates = new Dictionary<KeyCode, bool>();

    // 本回合玩家已放置的左右齿轮和轨道（用于检查）
    private KeyCode placedLeftGear = KeyCode.None;
    private KeyCode placedRightGear = KeyCode.None;
    private int placedLeftTrackNumber = 0;
    private int placedRightTrackNumber = 0;
    private KeyCode placedLeftTrackKey = KeyCode.None;
    private KeyCode placedRightTrackKey = KeyCode.None;

    // 鼠标阶段
    private bool isHandlingNextRound = false;
    private float moveAccumulated = 0f;
    private Vector3 lastMousePos;
    
    // 倒计时系统（全局计时器）
    private float currentGlobalTime = 0f;
    private bool isGamePaused = false;
    
    // 成功订单计数
    private int successfulOrderCount = 0;

    private void Start()
    {
        InitializeKeyStates();
        // 初始化全局计时器
        currentGlobalTime = globalTimeLimit;
        successfulOrderCount = 0;
        StartNewRound();
    }

    private void Update()
    {
        HandleGearOutOfBox();
        UpdateKeyStates();
        
        // 更新全局倒计时（只在游戏进行中且未暂停时）
        if (!isHandlingNextRound && !isGamePaused && 
            currentState != GameState.WinState && 
            currentState != GameState.FailState && 
            currentState != GameState.Transition)
        {
            UpdateGlobalTimer();
        }

        switch (currentState)
        {
            case GameState.WaitingForLeftGearPlacement:
                HandleLeftPlacement();
                break;

            case GameState.WaitingForRightGearPlacement:
                HandleRightPlacement();
                break;

            case GameState.MouseStage:
                if (requireMouseMoveAfterCorrect)
                    UpdateMouseMove();
                break;

            case GameState.WinState:
            case GameState.FailState:
            case GameState.Transition:
                // 这些状态不需要每帧处理
                break;
        }
    }

    private void StartNewRound()
    {
        if (orderController == null)
        {
            Debug.LogError("GameManager: orderController 未设置！");
            return;
        }

        // 清状态
        currentOutOfBoxGears.Clear();
        placedLeftGear = KeyCode.None;
        placedRightGear = KeyCode.None;
        placedLeftTrackNumber = 0;
        placedRightTrackNumber = 0;
        placedLeftTrackKey = KeyCode.None;
        placedRightTrackKey = KeyCode.None;

        // 生成新 recipe
        orderController.GenerateNewRecipe();
        currentRecipe = orderController.GetCurrentRecipe();

        // 回到放左齿轮阶段
        currentState = GameState.WaitingForLeftGearPlacement;

        // 重置鼠标阶段
        moveAccumulated = 0f;
        lastMousePos = Input.mousePosition;
        UpdateProgressUI(0f, false);

        // 注意：不再重置计时器，使用全局计时器
        
        // 更新倒计时显示
        UpdateTimerDisplay();

        // 隐藏所有结果面板
        HideResultPanels();

        Debug.Log($"[Round] New Recipe: {currentRecipe.GetDebugString()}");
    }

    private void InitializeKeyStates()
    {
        foreach (var key in gearKeys)
            keyStates[key] = Input.GetKey(key);
    }

    private void UpdateKeyStates()
    {
        foreach (var key in gearKeys)
            keyStates[key] = Input.GetKey(key);
    }

    /// <summary>
    /// 检测齿轮“离盒/回盒”：按键从按下->松开 视为离盒；松开->按下 视为回盒
    /// </summary>
    private void HandleGearOutOfBox()
    {
        foreach (var key in gearKeys)
        {
            bool wasPressed = keyStates.TryGetValue(key, out var v) ? v : false;
            bool isPressed = Input.GetKey(key);

            // 按下 -> 松开：离盒
            if (wasPressed && !isPressed)
            {
                if (!currentOutOfBoxGears.Contains(key))
                {
                    currentOutOfBoxGears.Add(key);
                    Debug.Log($"齿轮 {key} 离开齿轮盒（进入盒外队列）");
                }
            }
            // 松开 -> 按下：回盒
            else if (!wasPressed && isPressed)
            {
                if (currentOutOfBoxGears.Contains(key))
                {
                    currentOutOfBoxGears.Remove(key);
                    Debug.Log($"齿轮 {key} 放回齿轮盒（从盒外队列移除）");
                }
            }
        }
    }

    /// <summary>
    /// 左边：轨道用 1-5，对应 Alpha1~Alpha5
    /// 要求：玩家按下任意左轨道键(1-5)时，记录"盒外队列第一个齿轮"和轨道信息
    /// </summary>
    private void HandleLeftPlacement()
    {
        // 检查所有左轨道键 (1-5)
        for (int trackNum = 1; trackNum <= 5; trackNum++)
        {
            KeyCode trackKey = RecipeOrderController.TrackNumberToKeyCode(trackNum);
            
            if (Input.GetKeyDown(trackKey))
            {
                if (currentOutOfBoxGears.Count <= 0)
                {
                    Debug.LogWarning($"左轨道 {trackNum} 按下了，但盒外没有齿轮。");
                    return;
                }

                var gear = currentOutOfBoxGears[0];
                currentOutOfBoxGears.RemoveAt(0);

                // 记录玩家放置的信息（不管对错）
                placedLeftGear = gear;
                placedLeftTrackNumber = trackNum;
                placedLeftTrackKey = trackKey;
                
                onLeftGearDetected?.Invoke(gear);
                
                // 进入等待右齿轮阶段
                currentState = GameState.WaitingForRightGearPlacement;

                Debug.Log($"左齿轮已记录：{gear} 放到左轨道 {trackNum}（键 {trackKey}）");
                return; // 只处理一个按键
            }
        }
    }

    /// <summary>
    /// 右边：轨道用 6-10，对应 Alpha6~Alpha0（10 用 0）
    /// 要求：玩家按下任意右轨道键(6-10)时，记录"盒外队列第一个齿轮"和轨道信息，然后检查结果
    /// </summary>
    private void HandleRightPlacement()
    {
        // 检查所有右轨道键 (6-10)
        for (int trackNum = 6; trackNum <= 10; trackNum++)
        {
            KeyCode trackKey = RecipeOrderController.TrackNumberToKeyCode(trackNum);
            
            if (Input.GetKeyDown(trackKey))
            {
                if (currentOutOfBoxGears.Count <= 0)
                {
                    Debug.LogWarning($"右轨道 {trackNum} 按下了，但盒外没有齿轮。");
                    return;
                }

                var gear = currentOutOfBoxGears[0];
                currentOutOfBoxGears.RemoveAt(0);

                // 记录玩家放置的信息（不管对错）
                placedRightGear = gear;
                placedRightTrackNumber = trackNum;
                placedRightTrackKey = trackKey;
                
                onRightGearDetected?.Invoke(gear);

                Debug.Log($"右齿轮已记录：{gear} 放到右轨道 {trackNum}（键 {trackKey}）");

                // 两个齿轮都记录完了，现在进行检查
                if (requireMouseMoveAfterCorrect)
                {
                    // 需要鼠标阶段：先进入鼠标阶段，完成后再检查
                    currentState = GameState.MouseStage;
                    onEnterMouseStage?.Invoke();
                    moveAccumulated = 0f;
                    lastMousePos = Input.mousePosition;
                    UpdateProgressUI(0f, true);
                }
                else
                {
                    // 不需要鼠标阶段：直接检查结果
                    CheckRoundResult();
                }
                
                return; // 只处理一个按键
            }
        }
    }

    private void UpdateMouseMove()
    {
        if (isHandlingNextRound) return;

        Vector3 current = Input.mousePosition;
        float distance = Vector3.Distance(current, lastMousePos);
        float speed = (Time.deltaTime > 0f) ? distance / Time.deltaTime : 0f;
        lastMousePos = current;

        if (speed >= minMouseSpeed)
        {
            float multiplier = 1f;

            if (speedAffectsProgress)
            {
                float normalized = (speed - minMouseSpeed) / Mathf.Max(minMouseSpeed, 0.0001f);
                normalized = Mathf.Clamp01(normalized);
                multiplier = Mathf.Lerp(1f, maxProgressMultiplier, normalized);
            }

            moveAccumulated += Time.deltaTime * multiplier;
        }

        float t = Mathf.Clamp01(requiredMoveTime > 0f ? moveAccumulated / requiredMoveTime : 1f);
        UpdateProgressUI(t, true);

        if (moveAccumulated >= requiredMoveTime)
        {
            UpdateProgressUI(1f, false);
            CheckRoundResult();
        }
    }

    /// <summary>
    /// 检查回合结果：验证玩家输入是否与订单匹配
    /// 检查内容：左齿轮类型、左轨道号、右齿轮类型、右轨道号
    /// </summary>
    private void CheckRoundResult()
    {
        if (isHandlingNextRound) return;

        // 检查玩家放置的齿轮类型和轨道号是否与订单完全匹配
        bool leftGearCorrect = (placedLeftGear == currentRecipe.leftGearKey);
        bool leftTrackCorrect = (placedLeftTrackNumber == currentRecipe.leftTrackNumber);
        bool rightGearCorrect = (placedRightGear == currentRecipe.rightGearKey);
        bool rightTrackCorrect = (placedRightTrackNumber == currentRecipe.rightTrackNumber);

        bool isCorrect = leftGearCorrect && leftTrackCorrect && rightGearCorrect && rightTrackCorrect;

        // 输出详细的检查结果
        Debug.Log($"[Round Check] 左齿轮: {placedLeftGear} (期望: {currentRecipe.leftGearKey}) - {(leftGearCorrect ? "✓" : "✗")}");
        Debug.Log($"[Round Check] 左轨道: {placedLeftTrackNumber} (期望: {currentRecipe.leftTrackNumber}) - {(leftTrackCorrect ? "✓" : "✗")}");
        Debug.Log($"[Round Check] 右齿轮: {placedRightGear} (期望: {currentRecipe.rightGearKey}) - {(rightGearCorrect ? "✓" : "✗")}");
        Debug.Log($"[Round Check] 右轨道: {placedRightTrackNumber} (期望: {currentRecipe.rightTrackNumber}) - {(rightTrackCorrect ? "✓" : "✗")}");

        if (isCorrect)
        {
            // 胜利：增加成功计数
            successfulOrderCount++;
            Debug.Log($"[Game] 成功订单计数: {successfulOrderCount}");
            
            // 显示胜利面板
            ShowWinPanel();
            onRoundWin?.Invoke();
            StartCoroutine(ResultPanelRoutine(true));
        }
        else
        {
            // 失败：显示失败面板
            ShowFailPanel();
            onPlacedWrong?.Invoke();
            StartCoroutine(ResultPanelRoutine(false));
        }
    }

    /// <summary>
    /// 显示胜利面板
    /// </summary>
    private void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }
        currentState = GameState.WinState;
        Debug.Log("[Round] 回合胜利！玩家输入与订单匹配。");
    }

    /// <summary>
    /// 显示失败面板
    /// </summary>
    private void ShowFailPanel()
    {
        if (failPanel != null)
        {
            failPanel.SetActive(true);
        }
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        currentState = GameState.FailState;
        Debug.LogWarning($"[Round] 回合失败！期望: L{currentRecipe.leftTrackNumber}-{currentRecipe.leftGearKey}, R{currentRecipe.rightTrackNumber}-{currentRecipe.rightGearKey} | 实际: L{placedLeftTrackNumber}-{placedLeftGear}, R{placedRightTrackNumber}-{placedRightGear}");
    }

    /// <summary>
    /// 隐藏所有结果面板
    /// </summary>
    private void HideResultPanels()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }
        if (timeoutPanel != null)
        {
            timeoutPanel.SetActive(false);
        }
        if (countNumberPanel != null)
        {
            countNumberPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 更新全局倒计时
    /// </summary>
    private void UpdateGlobalTimer()
    {
        currentGlobalTime -= Time.deltaTime;
        
        // 更新倒计时显示
        UpdateTimerDisplay();
        
        if (currentGlobalTime <= 0f)
        {
            currentGlobalTime = 0f;
            UpdateTimerDisplay(); // 确保显示为0
            // 如果还没有触发检测逻辑（即不在WinState或FailState），则触发游戏结束
            if (currentState != GameState.WinState && currentState != GameState.FailState)
            {
                OnGameTimeEnd();
            }
        }
    }
    
    /// <summary>
    /// 更新倒计时文本显示
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        
        // 将秒数转换为整数（向上取整，显示剩余秒数）
        int seconds = Mathf.CeilToInt(currentGlobalTime);
        
        // 确保不小于0
        seconds = Mathf.Max(0, seconds);
        
        // 更新文本显示（格式：30、29、28...）
        timerText.text = seconds.ToString();
        
        // 可选：当时间少于10秒时改变颜色为红色
        if (seconds <= 10)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }
    
    /// <summary>
    /// 游戏时间结束处理
    /// </summary>
    private void OnGameTimeEnd()
    {
        if (isGamePaused) return;
        
        isGamePaused = true;
        Debug.Log("[Game] 游戏时间结束！");
        
        // 更新倒计时显示为0
        UpdateTimerDisplay();
        
        // 显示计数面板
        ShowCountNumberPanel();
        
        // 启动计数面板协程
        StartCoroutine(CountPanelRoutine());
    }
    
    /// <summary>
    /// 显示计数面板
    /// </summary>
    private void ShowCountNumberPanel()
    {
        // 暂停游戏
        Time.timeScale = 0f;
        
        if (countNumberPanel != null)
        {
            countNumberPanel.SetActive(true);
        }
        
        // 更新计数文本
        if (countNumberText != null)
        {
            countNumberText.text = successfulOrderCount.ToString();
        }
        
        // 隐藏其他面板
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }
        if (timeoutPanel != null)
        {
            timeoutPanel.SetActive(false);
        }
        
        currentState = GameState.Transition;
    }
    
    /// <summary>
    /// 处理计数面板显示后的逻辑
    /// </summary>
    private IEnumerator CountPanelRoutine()
    {
        isHandlingNextRound = true;
        
        // 使用真实时间等待（因为Time.timeScale = 0）
        float elapsed = 0f;
        while (elapsed < countPanelDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // 隐藏计数面板
        if (countNumberPanel != null)
        {
            countNumberPanel.SetActive(false);
        }
        
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        // 重置成功计数
        successfulOrderCount = 0;
        Debug.Log("[Game] 成功计数已清零，重新开始游戏");
        
        // 重置全局计时器
        currentGlobalTime = globalTimeLimit;
        isGamePaused = false;
        
        // 重新开始游戏
        StartNewRound();
        isHandlingNextRound = false;
    }

    /// <summary>
    /// 处理结果面板显示后的逻辑
    /// </summary>
    private IEnumerator ResultPanelRoutine(bool isWin)
    {
        isHandlingNextRound = true;
        currentState = isWin ? GameState.WinState : GameState.FailState;
        
        // 注意：不再停止倒计时，全局计时器继续运行
        
        // 更新倒计时显示（保持当前值，不再更新）
        if (timerText != null)
        {
            timerText.color = Color.white; // 重置颜色
        }

        // 显示面板3秒
        yield return new WaitForSeconds(resultPanelDisplayTime);

        // 隐藏面板
        HideResultPanels();

        // 进入过渡状态
        currentState = GameState.Transition;

        // 等待一小段时间后进入下一回合
        yield return new WaitForSeconds(nextRoundDelay);

        StartNewRound();
        isHandlingNextRound = false;
    }

    private void UpdateProgressUI(float normalizedValue, bool show)
    {
        if (mouseProgressSlider == null) return;

        mouseProgressSlider.minValue = 0f;
        mouseProgressSlider.maxValue = 1f;
        mouseProgressSlider.value = Mathf.Clamp01(normalizedValue);
        mouseProgressSlider.gameObject.SetActive(show);
    }
}
