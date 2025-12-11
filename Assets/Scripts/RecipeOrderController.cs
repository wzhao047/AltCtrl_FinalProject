using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理订单需求系统
/// 随机生成1-3个不同的recipe订单，并在UI上显示
/// </summary>
public class RecipeOrderController : MonoBehaviour
{
    [System.Serializable]
    public class Recipe
    {
        [Header("Recipe信息")]
        [Tooltip("Recipe编号（1-6）")]
        public int recipeNumber;

        [Tooltip("Recipe名称（用于显示）")]
        public string recipeName;

        [Header("所需齿轮（手动输入按键名称）")]
        [Tooltip("第一个齿轮型号，输入按键名称（如：Z、X、C）")]
        public string firstGearKey = "Z";

        [Tooltip("第二个齿轮型号，输入按键名称（如：Z、X、C）")]
        public string secondGearKey = "X";

        [Header("显示相关")]
        [Tooltip("Recipe的图片（可选）")]
        public Sprite recipeSprite;

        // 内部使用的KeyCode（从字符串转换）
        private KeyCode? _firstGearKeyCode = null;
        private KeyCode? _secondGearKeyCode = null;

        /// <summary>
        /// 获取第一个齿轮的KeyCode
        /// </summary>
        public KeyCode GetFirstGearKeyCode()
        {
            if (!_firstGearKeyCode.HasValue)
            {
                _firstGearKeyCode = ParseKeyCode(firstGearKey);
            }
            return _firstGearKeyCode.Value;
        }

        /// <summary>
        /// 获取第二个齿轮的KeyCode
        /// </summary>
        public KeyCode GetSecondGearKeyCode()
        {
            if (!_secondGearKeyCode.HasValue)
            {
                _secondGearKeyCode = ParseKeyCode(secondGearKey);
            }
            return _secondGearKeyCode.Value;
        }

        /// <summary>
        /// 解析字符串为KeyCode
        /// </summary>
        private KeyCode ParseKeyCode(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return KeyCode.None;

            // 尝试直接解析（支持大小写）
            keyName = keyName.Trim().ToUpper();

            // 常见按键映射
            if (System.Enum.TryParse<KeyCode>(keyName, true, out KeyCode result))
            {
                return result;
            }

            // 如果解析失败，返回None并输出警告
            Debug.LogWarning($"无法解析按键名称: {keyName}，使用KeyCode.None");
            return KeyCode.None;
        }

        /// <summary>
        /// 重置KeyCode缓存（当字符串改变时调用）
        /// </summary>
        public void ResetKeyCodeCache()
        {
            _firstGearKeyCode = null;
            _secondGearKeyCode = null;
        }
    }

    [Header("订单显示UI")]
    [Tooltip("显示订单需求的UI Image（背景）")]
    public Image orderDisplayImage;

    [Tooltip("显示订单文本的TextMeshPro组件")]
    public TextMeshProUGUI orderText;

    [Header("订单设置")]
    [Tooltip("每次生成的订单数量范围（最小）")]
    [Range(1, 3)]
    public int minOrderCount = 1;

    [Tooltip("每次生成的订单数量范围（最大）")]
    [Range(1, 3)]
    public int maxOrderCount = 3;

    [Tooltip("生成新订单的延迟时间（秒）")]
    public float generateOrderDelay = 2f;

    [Header("所有可用的Recipe列表")]
    [Tooltip("6个Recipe的配置")]
    public List<Recipe> allRecipes = new List<Recipe>();

    // 当前订单列表（存储Recipe编号）
    private List<int> currentOrders = new List<int>();

    // 是否正在生成订单
    private bool isGeneratingOrder = false;

    private void Start()
    {
        // 初始化所有Recipe（如果没有配置）
        if (allRecipes == null || allRecipes.Count == 0)
        {
            InitializeDefaultRecipes();
        }

        // 生成初始订单
        GenerateNewOrders();
    }

    /// <summary>
    /// 初始化默认的6个Recipe
    /// </summary>
    private void InitializeDefaultRecipes()
    {
        allRecipes = new List<Recipe>
        {
            new Recipe
            {
                recipeNumber = 1,
                recipeName = "Recipe 1",
                firstGearKey = "Z",
                secondGearKey = "X"
            },
            new Recipe
            {
                recipeNumber = 2,
                recipeName = "Recipe 2",
                firstGearKey = "Z",
                secondGearKey = "C"
            },
            new Recipe
            {
                recipeNumber = 3,
                recipeName = "Recipe 3",
                firstGearKey = "X",
                secondGearKey = "Z"
            },
            new Recipe
            {
                recipeNumber = 4,
                recipeName = "Recipe 4",
                firstGearKey = "X",
                secondGearKey = "C"
            },
            new Recipe
            {
                recipeNumber = 5,
                recipeName = "Recipe 5",
                firstGearKey = "C",
                secondGearKey = "Z"
            },
            new Recipe
            {
                recipeNumber = 6,
                recipeName = "Recipe 6",
                firstGearKey = "C",
                secondGearKey = "X"
            }
        };
    }

    /// <summary>
    /// 生成新的订单（随机1-3个不同的Recipe）
    /// </summary>
    public void GenerateNewOrders()
    {
        if (isGeneratingOrder)
            return;

        isGeneratingOrder = true;

        // 清空当前订单
        currentOrders.Clear();

        // 随机生成订单数量
        int orderCount = Random.Range(minOrderCount, maxOrderCount + 1);

        // 确保不超过可用Recipe数量
        orderCount = Mathf.Min(orderCount, allRecipes.Count);

        // 创建可用Recipe编号列表（1-6）
        List<int> availableRecipeNumbers = new List<int>();
        for (int i = 0; i < allRecipes.Count; i++)
        {
            availableRecipeNumbers.Add(allRecipes[i].recipeNumber);
        }

        // 随机选择不重复的Recipe
        for (int i = 0; i < orderCount; i++)
        {
            if (availableRecipeNumbers.Count == 0)
                break;

            int randomIndex = Random.Range(0, availableRecipeNumbers.Count);
            int selectedRecipeNumber = availableRecipeNumbers[randomIndex];
            currentOrders.Add(selectedRecipeNumber);
            availableRecipeNumbers.RemoveAt(randomIndex);
        }

        // 更新UI显示
        UpdateOrderDisplay();

        isGeneratingOrder = false;

        Debug.Log($"生成了 {currentOrders.Count} 个新订单");
    }

    /// <summary>
    /// 更新订单显示UI
    /// </summary>
    private void UpdateOrderDisplay()
    {
        if (orderText == null)
        {
            Debug.LogWarning("RecipeOrderController: orderText 未设置！");
            return;
        }

        // 构建订单文本
        string orderTextContent = "";

        for (int i = 0; i < currentOrders.Count; i++)
        {
            int recipeNumber = currentOrders[i];
            Recipe recipe = GetRecipeByNumber(recipeNumber);

            if (recipe != null)
            {
                // 格式：1. Recipe 1
                orderTextContent += $"{i + 1}. {recipe.recipeName}";

                // 如果不是最后一行，添加换行
                if (i < currentOrders.Count - 1)
                {
                    orderTextContent += "\n";
                }
            }
        }

        // 更新文本显示
        orderText.text = orderTextContent;

        // 显示订单UI
        if (orderDisplayImage != null)
        {
            orderDisplayImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 通过Recipe编号获取Recipe
    /// </summary>
    private Recipe GetRecipeByNumber(int recipeNumber)
    {
        return allRecipes.Find(r => r.recipeNumber == recipeNumber);
    }

    /// <summary>
    /// 获取当前订单列表
    /// </summary>
    public List<int> GetCurrentOrders()
    {
        return new List<int>(currentOrders);
    }

    /// <summary>
    /// 获取当前订单数量
    /// </summary>
    public int GetCurrentOrderCount()
    {
        return currentOrders.Count;
    }

    /// <summary>
    /// 检查指定的Recipe是否在当前订单中
    /// </summary>
    public bool IsRecipeInOrders(int recipeNumber)
    {
        return currentOrders.Contains(recipeNumber);
    }

    /// <summary>
    /// 通过齿轮组合获取Recipe编号
    /// </summary>
    public int GetRecipeByGears(KeyCode firstGear, KeyCode secondGear)
    {
        Recipe recipe = allRecipes.Find(r => 
            (r.GetFirstGearKeyCode() == firstGear && r.GetSecondGearKeyCode() == secondGear));
        
        return recipe != null ? recipe.recipeNumber : -1;
    }

    /// <summary>
    /// 通过齿轮组合获取Recipe名称
    /// </summary>
    public string GetRecipeNameByGears(KeyCode firstGear, KeyCode secondGear)
    {
        Recipe recipe = allRecipes.Find(r => 
            (r.GetFirstGearKeyCode() == firstGear && r.GetSecondGearKeyCode() == secondGear));
        
        return recipe != null ? recipe.recipeName : "";
    }

    /// <summary>
    /// 完成一个订单（从列表中移除）
    /// </summary>
    public bool CompleteOrder(int recipeNumber)
    {
        if (currentOrders.Contains(recipeNumber))
        {
            currentOrders.Remove(recipeNumber);
            UpdateOrderDisplay();

            // 如果所有订单都完成了，生成新订单
            if (currentOrders.Count == 0)
            {
                StartCoroutine(GenerateNewOrdersWithDelay());
            }

            return true;
        }
        return false;
    }

    /// <summary>
    /// 延迟生成新订单的协程
    /// </summary>
    private System.Collections.IEnumerator GenerateNewOrdersWithDelay()
    {
        yield return new WaitForSeconds(generateOrderDelay);
        GenerateNewOrders();
    }

    /// <summary>
    /// 清空当前所有订单
    /// </summary>
    public void ClearOrders()
    {
        currentOrders.Clear();
        UpdateOrderDisplay();
    }

    /// <summary>
    /// 隐藏订单显示
    /// </summary>
    public void HideOrderDisplay()
    {
        if (orderDisplayImage != null)
        {
            orderDisplayImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示订单显示
    /// </summary>
    public void ShowOrderDisplay()
    {
        if (orderDisplayImage != null)
        {
            orderDisplayImage.gameObject.SetActive(true);
        }
        UpdateOrderDisplay();
    }
}

