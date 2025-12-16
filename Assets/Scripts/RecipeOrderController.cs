using UnityEngine;
using TMPro;

public class RecipeOrderController : MonoBehaviour
{
    [System.Serializable]
    public struct RoundRecipe
    {
        public int leftTrackNumber;   // 1-5
        public int rightTrackNumber;  // 6-10（10 用 0 键）
        public KeyCode leftTrackKey;
        public KeyCode rightTrackKey;

        public KeyCode leftGearKey;   // A/B/C
        public KeyCode rightGearKey;  // A/B/C

        public string GetDebugString()
        {
            return $"L{leftTrackNumber}({leftTrackKey})-{leftGearKey} | R{rightTrackNumber}({rightTrackKey})-{rightGearKey}";
        }
    }

    [Header("UI")]
    public TextMeshProUGUI orderText;

    [Header("随机规则")]
    public bool allowSameGearBothSides = true;

    private readonly KeyCode[] gearPool = new KeyCode[] { KeyCode.A, KeyCode.B, KeyCode.C };

    private RoundRecipe current;

    private void Start()
    {
        GenerateNewRecipe();
    }

    public void GenerateNewRecipe()
    {
        // 左轨道：1-5
        int leftTrack = Random.Range(1, 6);

        // 右轨道：6-10
        int rightTrack = Random.Range(6, 11);

        // 齿轮：A/B/C
        KeyCode leftGear = gearPool[Random.Range(0, gearPool.Length)];
        KeyCode rightGear = gearPool[Random.Range(0, gearPool.Length)];

        if (!allowSameGearBothSides)
        {
            int safety = 20;
            while (rightGear == leftGear && safety-- > 0)
                rightGear = gearPool[Random.Range(0, gearPool.Length)];
        }

        current = new RoundRecipe
        {
            leftTrackNumber = leftTrack,
            rightTrackNumber = rightTrack,
            leftTrackKey = TrackNumberToKeyCode(leftTrack),
            rightTrackKey = TrackNumberToKeyCode(rightTrack),
            leftGearKey = leftGear,
            rightGearKey = rightGear
        };

        UpdateOrderText();
    }

    public RoundRecipe GetCurrentRecipe()
    {
        return current;
    }

    private void UpdateOrderText()
    {
        if (orderText == null) return;

        // 给玩家看的文字（你也可以改成中英混合）
        // 强调：左边轨道 1-5，右边轨道 6-10（10 用 0 键）
        orderText.text =
            $"Recipe:\n" +
            $"Left Track {current.leftTrackNumber}  ← Gear {KeyToGearLetter(current.leftGearKey)}\n" +
            $"Right Track {current.rightTrackNumber} ← Gear {KeyToGearLetter(current.rightGearKey)}\n\n" ;
            //$"(Put the LEFT gear out first, then the RIGHT gear)";
    }

    public static KeyCode TrackNumberToKeyCode(int trackNumber)
    {
        // 1-9 => Alpha1..Alpha9, 10 => Alpha0
        switch (trackNumber)
        {
            case 1: return KeyCode.Alpha1;
            case 2: return KeyCode.Alpha2;
            case 3: return KeyCode.Alpha3;
            case 4: return KeyCode.Alpha4;
            case 5: return KeyCode.Alpha5;
            case 6: return KeyCode.Alpha6;
            case 7: return KeyCode.Alpha7;
            case 8: return KeyCode.Alpha8;
            case 9: return KeyCode.Alpha9;
            case 10: return KeyCode.Alpha0;
            default: return KeyCode.None;
        }
    }

    private static string KeyToGearLetter(KeyCode key)
    {
        if (key == KeyCode.A) return "A";
        if (key == KeyCode.B) return "B";
        if (key == KeyCode.C) return "C";
        return key.ToString();
    }
}
