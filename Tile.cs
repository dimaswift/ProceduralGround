using UnityEngine;
using System.Collections.Generic;

namespace ProceduralGround
{
    public class Tile : MonoBehaviour
    {
        public int theme;
        public int layout;
        public int x, z;
        public bool empty;

        public GroundBuilder.LayoutGroup[] groups { get; set; }
        public MeshRenderer meshRenderer { get { return m_rend; } }
        public GroundBuilder.LayoutGroup currentGroup { get { return groups[m_ground.currentTheme]; } }
        public Transform cachedTransform { get { return m_cachedTransform; } }

        GroundBuilder m_ground;
        Transform m_cachedTransform;
        MeshRenderer m_rend;

        public void Init(GroundBuilder ground)
        {
            m_ground = ground;
            m_cachedTransform = transform;
            m_rend = GetComponent<MeshRenderer>();
        }

        public void SetMaterial(Material mat)
        {
            m_rend.sharedMaterial = mat;
        }

        public void Spawn(Vector3 pos)
        {
            transform.position = pos;
        }

        public void ShuffleObstacles()
        {
            var currentTheme = m_ground.currentTheme;

            var group = groups[currentTheme];
            layout = Random.Range(0, group.layouts.Count);
            var l = group.layouts[layout];

            for (int i = 0; i < l.objects.Length; i++)
            {
                var layoutObj = l.objects[i];
                var obstacle = group.pool[i];

                obstacle.Place(this, layoutObj);

            }
        }
    }
}
