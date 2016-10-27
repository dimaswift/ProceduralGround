using UnityEngine;
using System.Collections.Generic;

namespace ProceduralGround
{
    [CreateAssetMenu(fileName ="Theme", menuName = "Procedural Ground/Theme")]
    public class ThemeContainer : ScriptableObject
    {
        public int order;
        public float tileScale = 7.6f;
        public int index;
        public int euler = 45;
        public Material groundMaterial;
        public List<GroundBuilder.LayoutGroup> groups = new List<GroundBuilder.LayoutGroup>();
    }
}
