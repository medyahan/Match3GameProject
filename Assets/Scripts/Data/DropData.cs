using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "DropData", menuName = "Data/Match3Game/DropData")]
    public class DropData : ScriptableObject
    {
        public DropType Type;
        public Sprite Sprite;
    }
    
    public enum DropType
    {
        Green,
        Yellow,
        Red,
        Blue
    }
}
