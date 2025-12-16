using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    public enum GameState
    {
        WaitingForLeftGearPlacement,
        WaitingForRightGearPlacement,
        MouseStage,
        Transition
    }

    [Header("游戏状态")]
    public GameState currentState = GameState.WaitingForLeftGearPlacement;

    // 当前 recipe 需求
    private RecipeOrderController.RoundRecipe currentRecipe;

    // 盒子外的齿轮（按“松开”进入；按回去移除）
    private readonly List<KeyCode> currentOutOfBoxGears = new List<KeyCode>();
    private readonly Dictionary<KeyCode, bool> keyStates = new Dictionary<KeyCode, bool>();

    // 本回合玩家已放置的左右齿轮（用于 debug）
    private KeyCode placedLeftGear = KeyCode.None;
    private KeyCode placedRightGear = KeyCode.None;

    // 鼠标阶段
    private bool isHandlingNextRound = false;
    private float moveAccumulated = 0f;
    private Vector3 lastMousePos;

    private void Start()
    {
        InitializeKeyStates();
        StartNewRound();
    }

    private void Update()
    {
        HandleGearOutOfBox();
        UpdateKeyStates();

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

        // 生成新 recipe
        orderController.GenerateNewRecipe();
        currentRecipe = orderController.GetCurrentRecipe();

        // 回到放左齿轮阶段
        currentState = GameState.WaitingForLeftGearPlacement;

        // 重置鼠标阶段
        moveAccumulated = 0f;
        lastMousePos = Input.mousePosition;
        UpdateProgressUI(0f, false);

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
    /// 要求：玩家按下“当前 recipe 的 leftTrackKey”时，把“盒外队列第一个齿轮”提交为左齿轮
    /// </summary>
    private void HandleLeftPlacement()
    {
        if (Input.GetKeyDown(currentRecipe.leftTrackKey))
        {
            if (currentOutOfBoxGears.Count <= 0)
            {
                Debug.LogWarning("左轨道按下了，但盒外没有齿轮。");
                return;
            }

            var gear = currentOutOfBoxGears[0];
            currentOutOfBoxGears.RemoveAt(0);

            if (gear == currentRecipe.leftGearKey)
            {
                placedLeftGear = gear;
                onLeftGearDetected?.Invoke(gear);
                onLeftGearPlacedCorrect?.Invoke();
                currentState = GameState.WaitingForRightGearPlacement;

                Debug.Log($"左齿轮正确：{gear} 放到左轨道 {currentRecipe.leftTrackNumber}（键 {currentRecipe.leftTrackKey}）");
            }
            else
            {
                Debug.LogWarning($"左齿轮错误：拿到 {gear}，但需要 {currentRecipe.leftGearKey}");
                onPlacedWrong?.Invoke();

                // 失败处理：直接重开本回合（也可改成扣分/提示）
                StartNewRound();
            }
        }
    }

    /// <summary>
    /// 右边：轨道用 6-10，对应 Alpha6~Alpha0（10 用 0）
    /// 要求：玩家按下“当前 recipe 的 rightTrackKey”时，把“盒外队列第一个齿轮”提交为右齿轮
    /// </summary>
    private void HandleRightPlacement()
    {
        if (Input.GetKeyDown(currentRecipe.rightTrackKey))
        {
            if (currentOutOfBoxGears.Count <= 0)
            {
                Debug.LogWarning("右轨道按下了，但盒外没有齿轮。");
                return;
            }

            var gear = currentOutOfBoxGears[0];
            currentOutOfBoxGears.RemoveAt(0);

            if (gear == currentRecipe.rightGearKey)
            {
                placedRightGear = gear;
                onRightGearDetected?.Invoke(gear);
                onRightGearPlacedCorrect?.Invoke();

                Debug.Log($"右齿轮正确：{gear} 放到右轨道 {currentRecipe.rightTrackNumber}（键 {currentRecipe.rightTrackKey}）");

                // 两边都放对，进入鼠标阶段
                if (requireMouseMoveAfterCorrect)
                {
                    currentState = GameState.MouseStage;
                    onEnterMouseStage?.Invoke();
                    moveAccumulated = 0f;
                    lastMousePos = Input.mousePosition;
                    UpdateProgressUI(0f, true);
                }
                else
                {
                    // 不需要鼠标阶段就直接下一回合
                    RoundWinAndNext();
                }
            }
            else
            {
                Debug.LogWarning($"右齿轮错误：拿到 {gear}，但需要 {currentRecipe.rightGearKey}");
                onPlacedWrong?.Invoke();
                StartNewRound();
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
            RoundWinAndNext();
        }
    }

    private void RoundWinAndNext()
    {
        if (isHandlingNextRound) return;

        onRoundWin?.Invoke();
        StartCoroutine(NextRoundRoutine());
    }

    private IEnumerator NextRoundRoutine()
    {
        isHandlingNextRound = true;
        currentState = GameState.Transition;

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
