using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 游戏主管理器
/// 控制完整的游戏流程：订单生成 -> 检测齿轮输入 -> 验证提交
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("系统引用")]
    [Tooltip("订单控制器")]
    public RecipeOrderController orderController;

    [Tooltip("齿轮型号控制器")]
    public GearTypeController gearTypeController;

    [Header("轨道设置")]
    [Tooltip("第一个齿轮放置的轨道按键（Y键）")]
    public KeyCode firstGearTrackKey = KeyCode.Y;

    [Tooltip("第二个齿轮放置的轨道按键（E键）")]
    public KeyCode secondGearTrackKey = KeyCode.E;

    [Tooltip("确认提交按键（S键）")]
    public KeyCode submitKey = KeyCode.S;

    [Header("游戏状态")]
    [Tooltip("当前游戏状态")]
    public GameState currentState = GameState.WaitingForFirstGearPlacement;

    [Header("当前输入记录")]
    [Tooltip("玩家输入的Recipe列表")]
    public List<PlayerRecipeInput> playerInputs = new List<PlayerRecipeInput>();

    [Tooltip("当前正在输入的Recipe索引（0开始）")]
    public int currentRecipeIndex = 0;

    [Header("UI面板")]
    [Tooltip("成功面板（WinPanel）")]
    public GameObject winPanel;

    [Tooltip("失败面板（FailPanel）")]
    public GameObject failPanel;

    [Header("事件")]
    [Tooltip("当检测到first gear时触发")]
    public UnityEvent<KeyCode> onFirstGearDetected;

    [Tooltip("当检测到second gear时触发")]
    public UnityEvent<KeyCode> onSecondGearDetected;

    [Tooltip("当first gear放置在轨道上时触发")]
    public UnityEvent onFirstGearPlaced;

    [Tooltip("当second gear放置在轨道上时触发")]
    public UnityEvent onSecondGearPlaced;

    [Tooltip("当所有Recipe输入完成时触发")]
    public UnityEvent onAllRecipesInput;

    [Tooltip("当提交成功时触发")]
    public UnityEvent onSubmitSuccess;

    [Tooltip("当提交失败时触发")]
    public UnityEvent onSubmitFailed;

    // 当前在盒子外面的齿轮（已松开但未放置的齿轮）
    private List<KeyCode> currentOutOfBoxGears = new List<KeyCode>();

    // 跟踪按键状态（用于检测松开和按下）
    private Dictionary<KeyCode, bool> keyStates = new Dictionary<KeyCode, bool>();

    // 齿轮按键列表（Z、X、C等）
    private List<KeyCode> gearKeys = new List<KeyCode> { KeyCode.Z, KeyCode.X, KeyCode.C };

    public enum GameState
    {
        WaitingForFirstGearPlacement,  // 等待将第一个齿轮放在Y轨道上
        WaitingForSecondGearPlacement, // 等待将第二个齿轮放在E轨道上
        WaitingForGearsReturn,         // 等待齿轮放回盒子（准备下一个Recipe）
        WaitingForSubmit,              // 等待提交
        Processing,                    // 处理中（防止重复输入）
        ShowingPanel                   // 显示面板中
    }

    [System.Serializable]
    public class PlayerRecipeInput
    {
        public KeyCode firstGear;
        public KeyCode secondGear;
        public int recipeNumber = -1; // 对应的Recipe编号，-1表示未匹配
    }

    private void Start()
    {
        // 初始化按键状态跟踪
        InitializeKeyStates();

        // 重置游戏状态
        ResetGame();
    }

    private void Update()
    {
        // 检测S键（提交/关闭面板）
        if (Input.GetKeyDown(submitKey))
        {
            if (currentState == GameState.ShowingPanel)
            {
                // 如果正在显示面板，隐藏面板并刷新订单
                HideAllPanels();
                RefreshOrder();
            }
            else
            {
                // 如果不在显示面板状态，进行验证
                ValidateAndShowResult();
            }
            return;
        }

        // 如果正在显示面板，不处理其他输入
        if (currentState == GameState.ShowingPanel)
            return;

        // 检测齿轮的拿出和放回（所有状态都需要检测）
        HandleGearOutOfBox();

        // 根据当前状态处理输入
        switch (currentState)
        {
            case GameState.WaitingForFirstGearPlacement:
                HandleFirstGearPlacement();
                break;

            case GameState.WaitingForSecondGearPlacement:
                HandleSecondGearPlacement();
                break;

            case GameState.WaitingForGearsReturn:
                HandleGearsReturn();
                break;

            case GameState.WaitingForSubmit:
                // 这个状态现在由S键统一处理，不再需要单独处理
                break;
        }

        // 更新按键状态
        UpdateKeyStates();
    }

    /// <summary>
    /// 初始化按键状态跟踪
    /// </summary>
    private void InitializeKeyStates()
    {
        foreach (var key in gearKeys)
        {
            keyStates[key] = Input.GetKey(key);
        }
    }

    /// <summary>
    /// 更新按键状态
    /// </summary>
    private void UpdateKeyStates()
    {
        foreach (var key in gearKeys)
        {
            keyStates[key] = Input.GetKey(key);
        }
    }

    /// <summary>
    /// 检测齿轮的拿出和放回（持续检测）
    /// </summary>
    private void HandleGearOutOfBox()
    {
        foreach (var key in gearKeys)
        {
            bool wasPressed = keyStates.ContainsKey(key) ? keyStates[key] : false;
            bool isPressed = Input.GetKey(key);

            // 检测从按下到松开的变化（齿轮被拿出）
            if (wasPressed && !isPressed)
            {
                if (!currentOutOfBoxGears.Contains(key))
                {
                    currentOutOfBoxGears.Add(key);
                    Debug.Log($"齿轮 {key} 被拿出盒子");
                }
            }
            // 检测从松开到按下的变化（齿轮被放回）
            else if (!wasPressed && isPressed)
            {
                if (currentOutOfBoxGears.Contains(key))
                {
                    currentOutOfBoxGears.Remove(key);
                    Debug.Log($"齿轮 {key} 被放回盒子");
                }
            }
        }
    }

    /// <summary>
    /// 处理第一个齿轮放置（按下Y键时记录）
    /// </summary>
    private void HandleFirstGearPlacement()
    {
        if (Input.GetKeyDown(firstGearTrackKey))
        {
            // 检查是否有齿轮在盒子外面
            if (currentOutOfBoxGears.Count > 0)
            {
                // 记录第一个在盒子外面的齿轮为first gear
                KeyCode firstGear = currentOutOfBoxGears[0];
                
                // 确保有输入记录
                if (currentRecipeIndex >= playerInputs.Count)
                {
                    playerInputs.Add(new PlayerRecipeInput());
                }
                playerInputs[currentRecipeIndex].firstGear = firstGear;

                // 从列表中移除（齿轮已放置）
                currentOutOfBoxGears.RemoveAt(0);

                // 进入下一个状态
                currentState = GameState.WaitingForSecondGearPlacement;

                onFirstGearDetected?.Invoke(firstGear);
                onFirstGearPlaced?.Invoke();
                Debug.Log($"First Gear {firstGear} 已放置在轨道 {firstGearTrackKey} 上");
            }
            else
            {
                Debug.LogWarning("没有齿轮在盒子外面，无法放置first gear");
            }
        }
    }

    /// <summary>
    /// 处理第二个齿轮放置（按下E键时记录）
    /// </summary>
    private void HandleSecondGearPlacement()
    {
        if (Input.GetKeyDown(secondGearTrackKey))
        {
            // 检查是否有齿轮在盒子外面
            if (currentOutOfBoxGears.Count > 0)
            {
                // 记录第一个在盒子外面的齿轮为second gear
                KeyCode secondGear = currentOutOfBoxGears[0];
                
                // 确保有输入记录
                if (currentRecipeIndex < playerInputs.Count)
                {
                    playerInputs[currentRecipeIndex].secondGear = secondGear;
                }

                // 从列表中移除（齿轮已放置）
                currentOutOfBoxGears.RemoveAt(0);

                // 检查是否还有更多Recipe需要输入
                List<int> orders = orderController != null ? orderController.GetCurrentOrders() : new List<int>();
                
                if (currentRecipeIndex + 1 < orders.Count)
                {
                    // 还有更多Recipe，等待齿轮放回盒子
                    currentState = GameState.WaitingForGearsReturn;
                    Debug.Log($"第二个齿轮已放置，等待所有齿轮放回盒子后开始第 {currentRecipeIndex + 2} 个Recipe");
                }
                else
                {
                    // 所有Recipe输入完成，等待提交
                    currentState = GameState.WaitingForSubmit;
                    Debug.Log("所有Recipe输入完成，等待提交（按S键）");
                }

                onSecondGearDetected?.Invoke(secondGear);
                onSecondGearPlaced?.Invoke();
                Debug.Log($"Second Gear {secondGear} 已放置在轨道 {secondGearTrackKey} 上");
            }
            else
            {
                Debug.LogWarning("没有齿轮在盒子外面，无法放置second gear");
            }
        }
    }

    /// <summary>
    /// 处理齿轮放回盒子（检查是否所有齿轮都放回）
    /// </summary>
    private void HandleGearsReturn()
    {
        // 检查是否所有齿轮都放回盒子了
        if (currentOutOfBoxGears.Count == 0)
        {
            // 所有齿轮都放回了，开始下一个Recipe
            List<int> orders = orderController != null ? orderController.GetCurrentOrders() : new List<int>();
            
            if (currentRecipeIndex + 1 < orders.Count)
            {
                currentRecipeIndex++;
                currentState = GameState.WaitingForFirstGearPlacement;
                Debug.Log($"开始输入第 {currentRecipeIndex + 1} 个Recipe");
            }
            else
            {
                // 所有Recipe输入完成，等待提交
                currentState = GameState.WaitingForSubmit;
                Debug.Log("所有Recipe输入完成，等待提交（按S键）");
            }
        }
    }

    /// <summary>
    /// 验证并显示结果（不管当前处于什么阶段）
    /// </summary>
    private void ValidateAndShowResult()
    {
        if (orderController == null)
        {
            Debug.LogError("GameManager: orderController 未设置！");
            return;
        }

        currentState = GameState.Processing;

        List<int> orders = orderController.GetCurrentOrders();
        
        // 为每个玩家输入匹配Recipe编号
        foreach (var input in playerInputs)
        {
            if (input.recipeNumber == -1 && input.firstGear != KeyCode.None && input.secondGear != KeyCode.None)
            {
                input.recipeNumber = orderController.GetRecipeByGears(input.firstGear, input.secondGear);
            }
        }

        // 检查玩家输入的Recipe是否与订单匹配
        bool allMatch = ValidateRecipes(orders);

        // 显示对应的面板
        if (allMatch)
        {
            ShowWinPanel();
        }
        else
        {
            ShowFailPanel();
        }

        currentState = GameState.ShowingPanel;
    }

    /// <summary>
    /// 验证Recipe是否匹配
    /// </summary>
    private bool ValidateRecipes(List<int> orders)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.LogWarning("订单列表为空");
            return false;
        }

        // 检查玩家输入的Recipe是否与订单匹配
        bool allMatch = true;
        List<int> playerRecipeNumbers = new List<int>();
        
        foreach (var input in playerInputs)
        {
            if (input.recipeNumber > 0)
            {
                playerRecipeNumbers.Add(input.recipeNumber);
            }
        }

        // 检查数量是否匹配
        if (playerRecipeNumbers.Count != orders.Count)
        {
            allMatch = false;
            Debug.Log($"数量不匹配：订单有 {orders.Count} 个，玩家输入了 {playerRecipeNumbers.Count} 个");
        }
        else
        {
            // 检查每个Recipe是否都在订单中
            foreach (var recipeNumber in playerRecipeNumbers)
            {
                if (!orders.Contains(recipeNumber))
                {
                    allMatch = false;
                    Debug.Log($"Recipe {recipeNumber} 不在订单中");
                    break;
                }
            }

            // 检查订单中的每个Recipe是否都被输入了
            foreach (var orderNumber in orders)
            {
                if (!playerRecipeNumbers.Contains(orderNumber))
                {
                    allMatch = false;
                    Debug.Log($"订单中的 Recipe {orderNumber} 未被输入");
                    break;
                }
            }
        }

        return allMatch;
    }

    /// <summary>
    /// 显示成功面板
    /// </summary>
    private void ShowWinPanel()
    {
        HideAllPanels();
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("显示成功面板！");
        }
        else
        {
            Debug.LogWarning("WinPanel未设置！");
        }
        onSubmitSuccess?.Invoke();
    }

    /// <summary>
    /// 显示失败面板
    /// </summary>
    private void ShowFailPanel()
    {
        HideAllPanels();
        if (failPanel != null)
        {
            failPanel.SetActive(true);
            Debug.Log("显示失败面板！");
        }
        else
        {
            Debug.LogWarning("FailPanel未设置！");
        }
        onSubmitFailed?.Invoke();
    }

    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    private void HideAllPanels()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 刷新订单（完成当前订单并生成新订单）
    /// </summary>
    private void RefreshOrder()
    {
        if (orderController == null)
        {
            Debug.LogError("GameManager: orderController 未设置！");
            return;
        }

        // 如果验证成功，完成所有订单
        List<int> orders = orderController.GetCurrentOrders();
        bool allMatch = ValidateRecipes(orders);
        
        if (allMatch)
        {
            // 完成所有订单
            foreach (var orderNumber in orders)
            {
                orderController.CompleteOrder(orderNumber);
            }
        }

        // 重置游戏状态
        ResetGame();
        
        Debug.Log("订单已刷新，游戏已重置");
    }

    /// <summary>
    /// 重置游戏
    /// </summary>
    public void ResetGame()
    {
        currentState = GameState.WaitingForFirstGearPlacement;
        currentRecipeIndex = 0;
        playerInputs.Clear();
        currentOutOfBoxGears.Clear();
        
        // 确保面板已隐藏
        HideAllPanels();
        
        Debug.Log("游戏已重置，等待第一个齿轮放置");
    }

    /// <summary>
    /// 获取当前状态描述（用于调试）
    /// </summary>
    public string GetCurrentStateDescription()
    {
        switch (currentState)
        {
            case GameState.WaitingForFirstGearPlacement:
                return $"等待将第一个齿轮放在轨道上（按{firstGearTrackKey}键）";
            case GameState.WaitingForSecondGearPlacement:
                return $"等待将第二个齿轮放在轨道上（按{secondGearTrackKey}键）";
            case GameState.WaitingForGearsReturn:
                return "等待所有齿轮放回盒子";
            case GameState.WaitingForSubmit:
                return $"等待提交（按{submitKey}键）";
            case GameState.Processing:
                return "处理中...";
            case GameState.ShowingPanel:
                return "显示面板中";
            default:
                return "未知状态";
        }
    }
}

