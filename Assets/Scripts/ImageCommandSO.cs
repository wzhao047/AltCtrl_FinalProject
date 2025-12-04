using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewImageCommand",
    menuName = "Input/Image Command"
)]
public class ImageCommandSO : ScriptableObject
{
    [Header("这个指令的名字（只是方便你在 Inspector 里辨认）")]
    public string commandName;

    [Header("屏幕上显示的图片")]
    public Sprite imageSprite;

    [Header("玩家需要按的键（顺序无所谓）")]
    public List<KeyCode> requiredKeys = new List<KeyCode>();
}
