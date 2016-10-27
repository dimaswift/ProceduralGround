using UnityEngine;
using HandyUtilities.PoolSystem;

namespace ProceduralGround
{
    public class Obstacle : PooledObject<Obstacle>
    {
        public bool IsVisible { get { return m_isVisible; } set { m_isVisible = value; } }

        public enum Type { Coin, Launcher, Chest, Teleport, Obstacle }

        public Type obstacleType;

        public Bounds bounds { get { return m_hasCollider ? m_collider.bounds : new Bounds(cachedTransform.position, Vector3.one); } }

        bool m_isVisible;

        bool m_hasCollider;

        public bool m_canBeHidden;

        float m_yPos = 0;

        GroundBuilder.Layout.LayoutObject m_layoutObj;

        Collider m_collider;

        MeshRenderer[] m_renderers;

        public override PoolContainer<Obstacle> pool
        {
            get
            {
                return null;
            }

            set
            {

            }
        }

        public override Obstacle Object
        {
            get
            {
                return this;
            }
        }

        public void Hide(bool hide)
        {
            var pos = cachedTransform.position;
            pos.y = hide ? -1000 : m_yPos;
            cachedTransform.position = pos;
        }

        public override void Init()
        {
            base.Init();
            m_renderers = GetComponentsInChildren<MeshRenderer>(true);
            m_collider = GetComponent<Collider>();
            m_hasCollider = m_collider != null;
        }

        public virtual void Place(Tile tile, GroundBuilder.Layout.LayoutObject layout)
        {
            this.m_layoutObj = layout;
            var pos = tile.cachedTransform.TransformPoint(layout.position);
            cachedTransform.position = pos;
            cachedTransform.eulerAngles = new Vector3(0, layout.euler, 0);
        }

        public void TryToHide(Vector3 playerPos, float camSizeSqrd)
        {
            if (m_layoutObj.canBeHidden && !IsVisibleByCamera(playerPos, camSizeSqrd))
            {
                m_collider.enabled = false;
                for (int i = 0; i < m_renderers.Length; i++)
                {
                    m_renderers[i].enabled = false;
                }
            }
        }

        public bool IsVisibleByCamera(Vector3 playerPos, float camSizeSqrd)
        {
            return Vector3.SqrMagnitude(playerPos - cachedTransform.position) < camSizeSqrd;
        }

        public void ShowBack(Vector3 playerPos, float camSizeSqrd)
        {
            if (m_layoutObj.canBeHidden && !IsVisibleByCamera(playerPos, camSizeSqrd))
            {
                m_collider.enabled = true;
                for (int i = 0; i < m_renderers.Length; i++)
                {
                    m_renderers[i].enabled = true;
                }
            }
        }
    }
}
