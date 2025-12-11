using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 管理齿轮型号系统
/// 齿轮在盒子里时按键是长按状态，松开按键代表拿出齿轮并使用
/// </summary>
public class GearTypeController : MonoBehaviour
{
    [System.Serializable]
    public class GearType
    {
        [Header("齿轮信息")]
        [Tooltip("齿轮型号名称（用于调试）")]
        public string gearTypeName;

        [Tooltip("对应的按键")]
        public KeyCode keyCode;

        [Header("容器盒子UI")]
        [Tooltip("在容器盒子中显示的齿轮UI（UI Image或GameObject），取出齿轮时隐藏")]
        public UnityEngine.UI.Image gearUI;

        [Tooltip("齿轮的GameObject（可选，如果需要3D模型或GameObject显示）")]
        public GameObject gearObject;

        [Tooltip("是否正在使用（松开按键时）")]
        public bool isInUse = false;

        [Header("Recipe相关")]
        [Tooltip("当使用此齿轮型号时显示的Recipe图片（UI Image）")]
        public UnityEngine.UI.Image recipeImage;

        [Header("事件")]
        [Tooltip("当齿轮被拿出使用时触发")]
        public UnityEvent onGearTakenOut;

        [Tooltip("当齿轮被放回盒子时触发")]
        public UnityEvent onGearPutBack;
    }

    [Header("齿轮型号列表")]
    [Tooltip("所有可用的齿轮型号（Z、X、C等）")]
    public List<GearType> gearTypes = new List<GearType>();

    [Header("设置")]
    [Tooltip("游戏开始时，所有齿轮默认在盒子里（按键默认按下状态）")]
    public bool startWithGearsInBox = true;

    [Header("容器盒子UI设置")]
    [Tooltip("齿轮容器盒子的UI GameObject（可选，用于控制整个盒子的显示）")]
    public GameObject containerBoxUI;

    [Header("Recipe显示设置")]
    [Tooltip("用来显示Recipe图画的UI Image")]
    public UnityEngine.UI.Image recipeDisplayImage;

    [Tooltip("如果没有使用任何齿轮，是否显示默认图片")]
    public bool showDefaultWhenNoGear = true;

    [Tooltip("默认Recipe图片（当没有使用任何齿轮时显示）")]
    public Sprite defaultRecipeSprite;

    // 当前正在使用的齿轮类型
    private GearType currentActiveGear = null;

    // 跟踪每个齿轮的按键状态
    private Dictionary<GearType, bool> keyPressedStates = new Dictionary<GearType, bool>();

    private void Start()
    {
        // 初始化默认齿轮型号（如果没有配置）
        if (gearTypes == null || gearTypes.Count == 0)
        {
            InitializeDefaultGearTypes();
        }

        // 初始化按键状态
        InitializeKeyStates();

        // 初始化容器盒子UI
        InitializeContainerBoxUI();

        // 更新初始显示
        UpdateRecipeDisplay();
        UpdateAllGearVisuals();
    }

    private void Update()
    {
        // 检测所有齿轮型号的按键状态
        foreach (var gearType in gearTypes)
        {
            if (gearType == null) continue;

            bool isPressed = Input.GetKey(gearType.keyCode);
            bool wasPressed = keyPressedStates.ContainsKey(gearType) ? keyPressedStates[gearType] : startWithGearsInBox;

            // 检测按键状态变化
            if (isPressed != wasPressed)
            {
                if (!isPressed && wasPressed)
                {
                    // 按键从按下变为松开 = 拿出齿轮使用
                    TakeOutGear(gearType);
                }
                else if (isPressed && !wasPressed)
                {
                    // 按键从松开变为按下 = 放回齿轮到盒子
                    PutBackGear(gearType);
                }

                keyPressedStates[gearType] = isPressed;
            }
        }
    }

    /// <summary>
    /// 初始化默认齿轮型号（Z、X、C）
    /// </summary>
    private void InitializeDefaultGearTypes()
    {
        gearTypes = new List<GearType>
        {
            new GearType { gearTypeName = "齿轮Z", keyCode = KeyCode.Z },
            new GearType { gearTypeName = "齿轮X", keyCode = KeyCode.X },
            new GearType { gearTypeName = "齿轮C", keyCode = KeyCode.C }
        };
    }

    /// <summary>
    /// 初始化按键状态
    /// </summary>
    private void InitializeKeyStates()
    {
        foreach (var gearType in gearTypes)
        {
            if (gearType != null)
            {
                // 如果游戏开始时齿轮在盒子里，模拟按键是按下状态
                keyPressedStates[gearType] = startWithGearsInBox;
                gearType.isInUse = false;
            }
        }
    }

    /// <summary>
    /// 初始化容器盒子UI
    /// </summary>
    private void InitializeContainerBoxUI()
    {
        // 确保容器盒子UI在游戏开始时是显示的
        if (containerBoxUI != null)
        {
            containerBoxUI.SetActive(true);
        }

        // 初始化所有齿轮UI的显示状态
        foreach (var gearType in gearTypes)
        {
            if (gearType != null && gearType.gearUI != null)
            {
                // 游戏开始时，所有齿轮都在盒子里，所以UI应该显示
                gearType.gearUI.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 拿出齿轮使用（松开按键）
    /// </summary>
    private void TakeOutGear(GearType gearType)
    {
        if (gearType == null) return;

        // 如果之前有其他齿轮在使用，先放回去
        if (currentActiveGear != null && currentActiveGear != gearType)
        {
            PutBackGear(currentActiveGear, false);
        }

        // 设置当前齿轮为使用状态
        gearType.isInUse = true;
        currentActiveGear = gearType;

        // 更新视觉和Recipe显示
        UpdateGearVisual(gearType);
        UpdateRecipeDisplay();

        // 触发事件
        gearType.onGearTakenOut?.Invoke();

        Debug.Log($"{gearType.gearTypeName} ({gearType.keyCode}) - 齿轮被拿出使用");
    }

    /// <summary>
    /// 放回齿轮到盒子（按下按键）
    /// </summary>
    private void PutBackGear(GearType gearType, bool updateRecipe = true)
    {
        if (gearType == null) return;

        gearType.isInUse = false;

        // 如果这是当前使用的齿轮，清除当前激活齿轮
        if (currentActiveGear == gearType)
        {
            currentActiveGear = null;
        }

        // 更新视觉和Recipe显示
        UpdateGearVisual(gearType);
        if (updateRecipe)
        {
            UpdateRecipeDisplay();
        }

        // 触发事件
        gearType.onGearPutBack?.Invoke();

        Debug.Log($"{gearType.gearTypeName} ({gearType.keyCode}) - 齿轮被放回盒子");
    }

    /// <summary>
    /// 更新单个齿轮的视觉状态
    /// </summary>
    private void UpdateGearVisual(GearType gearType)
    {
        // 更新容器盒子中的齿轮UI
        if (gearType.gearUI != null)
        {
            // 如果齿轮在使用中（已拿出），隐藏盒子里的齿轮UI
            // 如果齿轮在盒子里（未使用），显示盒子里的齿轮UI
            gearType.gearUI.gameObject.SetActive(!gearType.isInUse);
        }

        // 更新3D GameObject（如果存在）
        if (gearType.gearObject != null)
        {
            gearType.gearObject.SetActive(!gearType.isInUse);
        }
    }

    /// <summary>
    /// 更新所有齿轮的视觉状态
    /// </summary>
    private void UpdateAllGearVisuals()
    {
        foreach (var gearType in gearTypes)
        {
            if (gearType != null)
            {
                UpdateGearVisual(gearType);
            }
        }
    }

    /// <summary>
    /// 更新Recipe显示
    /// </summary>
    private void UpdateRecipeDisplay()
    {
        if (recipeDisplayImage == null)
            return;

        if (currentActiveGear != null && currentActiveGear.recipeImage != null)
        {
            // 显示当前使用齿轮对应的Recipe图片
            recipeDisplayImage.sprite = currentActiveGear.recipeImage.sprite;
            recipeDisplayImage.gameObject.SetActive(true);
        }
        else if (showDefaultWhenNoGear && defaultRecipeSprite != null)
        {
            // 显示默认Recipe图片
            recipeDisplayImage.sprite = defaultRecipeSprite;
            recipeDisplayImage.gameObject.SetActive(true);
        }
        else if (currentActiveGear == null)
        {
            // 没有使用任何齿轮，隐藏Recipe
            recipeDisplayImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 获取当前正在使用的齿轮型号
    /// </summary>
    public GearType GetCurrentActiveGear()
    {
        return currentActiveGear;
    }

    /// <summary>
    /// 获取当前正在使用的齿轮名称
    /// </summary>
    public string GetCurrentActiveGearName()
    {
        return currentActiveGear != null ? currentActiveGear.gearTypeName : null;
    }

    /// <summary>
    /// 检查指定齿轮是否正在使用
    /// </summary>
    public bool IsGearInUse(KeyCode keyCode)
    {
        var gearType = gearTypes.Find(g => g.keyCode == keyCode);
        return gearType != null && gearType.isInUse;
    }

    /// <summary>
    /// 通过按键代码获取齿轮型号
    /// </summary>
    public GearType GetGearTypeByKey(KeyCode keyCode)
    {
        return gearTypes.Find(g => g.keyCode == keyCode);
    }
}

