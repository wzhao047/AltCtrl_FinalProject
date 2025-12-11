using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 控制五个齿轮轨道
/// QWERT五个键分别对应五个轨道上的齿轮
/// </summary>
public class GearTrackController : MonoBehaviour
{
    [System.Serializable]
    public class GearTrack
    {
        [Header("轨道信息")]
        [Tooltip("轨道名称（用于调试）")]
        public string trackName;

        [Tooltip("对应的按键")]
        public KeyCode keyCode;

        [Tooltip("该轨道上的齿轮GameObject（可以是任何需要控制的物体）")]
        public GameObject gearObject;

        [Header("工作台UI提示")]
        [Tooltip("在工作台UI上对应轨道位置显示的提示图片（UI Image）")]
        public Image trackHintImage;

        [Header("齿轮状态")]
        [Tooltip("齿轮是否已安装")]
        public bool isGearInstalled = false;

        [Header("事件")]
        [Tooltip("当齿轮被安装时触发")]
        public UnityEvent onGearInstalled;

        [Tooltip("当齿轮被移除时触发")]
        public UnityEvent onGearRemoved;

        [Tooltip("当轨道被触发时触发（按键按下时）")]
        public UnityEvent onTrackTriggered;
    }

    [Header("五个齿轮轨道配置")]
    [Tooltip("五个轨道：Q、W、E、R、T")]
    public List<GearTrack> gearTracks = new List<GearTrack>();

    [Header("操作模式")]
    [Tooltip("如果为true，长按按键激活轨道，松开按键移除轨道；如果为false，使用切换模式")]
    public bool holdToActivate = true;

    [Header("工作台UI提示设置")]
    [Tooltip("是否在按键按下时显示提示图片")]
    public bool showHintOnKeyPress = true;

    private void Start()
    {
        // 如果没有配置轨道，自动创建默认的QWERT五个轨道
        if (gearTracks == null || gearTracks.Count == 0)
        {
            InitializeDefaultTracks();
        }

        // 初始化所有轨道的状态
        UpdateAllGearVisuals();
    }

    private void Update()
    {
        // 检测所有轨道的按键输入
        foreach (var track in gearTracks)
        {
            if (track == null) continue;

            if (holdToActivate)
            {
                // 按住/松开模式：按下时激活，松开时移除
                if (Input.GetKeyDown(track.keyCode))
                {
                    HandleKeyDown(track);
                }
                else if (Input.GetKeyUp(track.keyCode))
                {
                    HandleKeyUp(track);
                }
            }
            else
            {
                // 切换模式：按一次切换状态
                if (Input.GetKeyDown(track.keyCode))
                {
                    HandleTrackInput(track);
                }
            }
        }
    }

    /// <summary>
    /// 初始化默认的QWERT五个轨道
    /// </summary>
    private void InitializeDefaultTracks()
    {
        gearTracks = new List<GearTrack>
        {
            new GearTrack { trackName = "轨道Q", keyCode = KeyCode.Q },
            new GearTrack { trackName = "轨道W", keyCode = KeyCode.W },
            new GearTrack { trackName = "轨道E", keyCode = KeyCode.E },
            new GearTrack { trackName = "轨道R", keyCode = KeyCode.R },
            new GearTrack { trackName = "轨道T", keyCode = KeyCode.T }
        };
    }

    /// <summary>
    /// 处理按键按下（按住/松开模式）
    /// </summary>
    private void HandleKeyDown(GearTrack track)
    {
        // 如果已经激活，不重复激活
        if (track.isGearInstalled)
            return;

        // 安装齿轮（激活轨道）
        InstallGear(track);

        // 显示工作台UI提示
        if (showHintOnKeyPress && track.trackHintImage != null)
        {
            track.trackHintImage.gameObject.SetActive(true);
        }

        // 触发轨道事件
        track.onTrackTriggered?.Invoke();

        Debug.Log($"{track.trackName} ({track.keyCode}) - 轨道已激活（按键按下）");
    }

    /// <summary>
    /// 处理按键松开（按住/松开模式）
    /// </summary>
    private void HandleKeyUp(GearTrack track)
    {
        // 如果已经移除，不重复移除
        if (!track.isGearInstalled)
            return;

        // 移除齿轮（取消激活轨道）
        RemoveGear(track);

        // 隐藏工作台UI提示
        if (track.trackHintImage != null)
        {
            track.trackHintImage.gameObject.SetActive(false);
        }

        Debug.Log($"{track.trackName} ({track.keyCode}) - 轨道已移除（按键松开）");
    }

    /// <summary>
    /// 处理轨道按键输入（切换模式）
    /// </summary>
    private void HandleTrackInput(GearTrack track)
    {
        // 显示工作台UI提示
        if (showHintOnKeyPress)
        {
            ShowTrackHint(track);
        }

        // 触发轨道事件
        track.onTrackTriggered?.Invoke();

        // 切换模式：按一次安装，再按一次移除
        if (track.isGearInstalled)
        {
            RemoveGear(track);
        }
        else
        {
            InstallGear(track);
        }
    }

    /// <summary>
    /// 在指定轨道上安装齿轮
    /// </summary>
    public void InstallGear(GearTrack track)
    {
        if (track == null) return;

        track.isGearInstalled = true;
        UpdateGearVisual(track);
        track.onGearInstalled?.Invoke();
        Debug.Log($"{track.trackName} ({track.keyCode}) - 齿轮已安装");
    }

    /// <summary>
    /// 从指定轨道移除齿轮
    /// </summary>
    public void RemoveGear(GearTrack track)
    {
        if (track == null) return;

        track.isGearInstalled = false;
        UpdateGearVisual(track);
        track.onGearRemoved?.Invoke();
        Debug.Log($"{track.trackName} ({track.keyCode}) - 齿轮已移除");
    }

    /// <summary>
    /// 通过索引安装齿轮
    /// </summary>
    public void InstallGearByIndex(int index)
    {
        if (index >= 0 && index < gearTracks.Count)
        {
            InstallGear(gearTracks[index]);
        }
    }

    /// <summary>
    /// 通过索引移除齿轮
    /// </summary>
    public void RemoveGearByIndex(int index)
    {
        if (index >= 0 && index < gearTracks.Count)
        {
            RemoveGear(gearTracks[index]);
        }
    }

    /// <summary>
    /// 通过按键代码安装齿轮
    /// </summary>
    public void InstallGearByKey(KeyCode key)
    {
        var track = gearTracks.Find(t => t.keyCode == key);
        if (track != null)
        {
            InstallGear(track);
        }
    }

    /// <summary>
    /// 通过按键代码移除齿轮
    /// </summary>
    public void RemoveGearByKey(KeyCode key)
    {
        var track = gearTracks.Find(t => t.keyCode == key);
        if (track != null)
        {
            RemoveGear(track);
        }
    }

    /// <summary>
    /// 更新单个齿轮的视觉状态
    /// </summary>
    private void UpdateGearVisual(GearTrack track)
    {
        if (track.gearObject != null)
        {
            track.gearObject.SetActive(track.isGearInstalled);
        }
    }

    /// <summary>
    /// 更新所有齿轮的视觉状态
    /// </summary>
    private void UpdateAllGearVisuals()
    {
        foreach (var track in gearTracks)
        {
            if (track != null)
            {
                UpdateGearVisual(track);
            }
        }
    }

    /// <summary>
    /// 检查所有齿轮是否都已安装
    /// </summary>
    public bool AreAllGearsInstalled()
    {
        foreach (var track in gearTracks)
        {
            if (track == null || !track.isGearInstalled)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 获取已安装齿轮的数量
    /// </summary>
    public int GetInstalledGearCount()
    {
        int count = 0;
        foreach (var track in gearTracks)
        {
            if (track != null && track.isGearInstalled)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 重置所有轨道（移除所有齿轮）
    /// </summary>
    public void ResetAllTracks()
    {
        foreach (var track in gearTracks)
        {
            if (track != null && track.isGearInstalled)
            {
                RemoveGear(track);
            }
        }
    }

    /// <summary>
    /// 显示轨道提示图片（切换模式使用）
    /// </summary>
    public void ShowTrackHint(GearTrack track)
    {
        if (track == null || track.trackHintImage == null)
            return;

        track.trackHintImage.gameObject.SetActive(true);
    }

    /// <summary>
    /// 通过索引显示轨道提示
    /// </summary>
    public void ShowTrackHintByIndex(int index)
    {
        if (index >= 0 && index < gearTracks.Count)
        {
            ShowTrackHint(gearTracks[index]);
        }
    }

    /// <summary>
    /// 通过按键代码显示轨道提示
    /// </summary>
    public void ShowTrackHintByKey(KeyCode key)
    {
        var track = gearTracks.Find(t => t.keyCode == key);
        if (track != null)
        {
            ShowTrackHint(track);
        }
    }

    /// <summary>
    /// 立即隐藏所有轨道提示
    /// </summary>
    public void HideAllTrackHints()
    {
        foreach (var track in gearTracks)
        {
            if (track != null && track.trackHintImage != null)
            {
                track.trackHintImage.gameObject.SetActive(false);
            }
        }
    }
}

