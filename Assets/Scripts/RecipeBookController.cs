using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制Recipe书的打开和关闭
/// 使用按键A来切换显示状态
/// </summary>
public class RecipeBookController : MonoBehaviour
{
    [Header("Recipe书的UI Image")]
    [Tooltip("要控制显示/隐藏的Recipe书UI Image")]
    public Image recipeBookImage;

    [Header("按键设置")]
    [Tooltip("控制打开/关闭的按键（默认是A键）")]
    public KeyCode toggleKey = KeyCode.A;

    [Header("初始状态")]
    [Tooltip("游戏开始时Recipe书是否显示")]
    public bool startVisible = false;

    private bool isVisible = false;

    private void Start()
    {
        // 设置初始状态
        isVisible = startVisible;
        UpdateVisibility();
    }

    private void Update()
    {
        // 检测按键A（或指定的按键）
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleRecipeBook();
        }
    }

    /// <summary>
    /// 切换Recipe书的显示状态
    /// </summary>
    public void ToggleRecipeBook()
    {
        isVisible = !isVisible;
        UpdateVisibility();
        Debug.Log($"Recipe书 {(isVisible ? "打开" : "关闭")}");
    }

    /// <summary>
    /// 打开Recipe书
    /// </summary>
    public void OpenRecipeBook()
    {
        isVisible = true;
        UpdateVisibility();
    }

    /// <summary>
    /// 关闭Recipe书
    /// </summary>
    public void CloseRecipeBook()
    {
        isVisible = false;
        UpdateVisibility();
    }

    /// <summary>
    /// 更新UI显示状态
    /// </summary>
    private void UpdateVisibility()
    {
        if (recipeBookImage != null)
        {
            recipeBookImage.gameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogWarning("RecipeBookController: recipeBookImage 未设置！");
        }
    }

    /// <summary>
    /// 获取当前Recipe书是否可见
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
}

