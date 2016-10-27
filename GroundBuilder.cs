using UnityEngine;
using System.Collections.Generic;
using HandyUtilities.Events;
using HandyUtilities;

namespace ProceduralGround
{
    public class GroundBuilder : MonoBehaviour
    {
        static GroundBuilder m_instance;

        public static GroundBuilder instance { get { return m_instance; } }
        public static Vector3 hiddenPosition { get { return new Vector3(0, -10000); } }
        public readonly GameEvent<Tile, Tile, Tile> onTileChange = new GameEvent<Tile, Tile, Tile>();

        public Tile tilePrefab;
        public Transform target;
        public Tile[,] tilePool;
        public Material[] materials;
        public int currentGroup;
        public int currentTheme;
        public int currentLayout;
        public int currentVariant;
        public List<ThemeContainer> themes = new List<ThemeContainer>();
        public bool ransomize;
        public bool iterate;
        public bool teleporting;

        [HideInInspector]
        public Obstacle[] spawned;

        Transform m_cachedTransform;
        float m_yPos;
        int m_tileCount = 3;
        int m_matIndex = 1;
        float m_tileSize;
        Vector3 m_currentCenter;
        Vector3[] m_grid;

        void Awake()
        {
            Init();
        }

        public void Init()
        {
            if (m_instance != null && m_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            m_instance = this;
            BeginFromStartTheme();
            m_tileSize = tilePrefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * 2 * tilePrefab.transform.localScale.x;
            tilePrefab.Init(this);
            m_yPos = tilePrefab.cachedTransform.localPosition.y;
            tilePool = new Tile[m_tileCount, m_tileCount];
            var halfSize = (m_tileSize * m_tileCount) / 2;

            for (int x = 0; x < m_tileCount; x++)
            {
                for (int z = 0; z < m_tileCount; z++)
                {
                    var t = Instantiate(tilePrefab);
                    t.transform.SetParent(transform);
                    t.transform.localPosition = new Vector3((m_tileSize * x) - halfSize + (m_tileSize / 2), m_yPos, (m_tileSize * z) - halfSize + (m_tileSize / 2));
                    t.transform.localEulerAngles = new Vector3(0, 0, 0);
                    t.Init(this);
                    t.name = string.Format("tile_x{0}_z{1}", x, z);
                    tilePool[x, z] = t;
                    t.x = x;
                    t.z = z;
                    t.groups = new LayoutGroup[themes.Count];
                    if (z == 1 && x == 1)
                    {
                        t.empty = true;
                    }
                }
            }
            tilePrefab.gameObject.SetActive(false);
            m_cachedTransform = transform;

            foreach (var t in themes)
            {
                if (t.groups.Count < 9)
                {
                    int size = 9 - t.groups.Count;
                    for (int i = 0; i < size; i++)
                    {
                        t.groups.Add(t.groups.Random().Clone());
                    }
                }
            }

            foreach (var theme in themes)
            {
                foreach (var g in theme.groups)
                {
                    g.InitPool();
                    g.DisableAll();
                }
            }
        }

        void Update()
        {
            var targetPos = m_cachedTransform.InverseTransformPoint(target.position);
            targetPos.y = m_yPos;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_matIndex = materials.NextIndex(m_matIndex);
            }
            if (Mathf.Abs(targetPos.z - m_currentCenter.z) > m_tileSize / 2 || Mathf.Abs(targetPos.x - m_currentCenter.x) > m_tileSize / 2)
            {
                var d = float.MaxValue;
                int closestX = 0, closestZ = 0;
                for (int x = 0; x < m_tileCount; x++)
                {
                    for (int z = 0; z < m_tileCount; z++)
                    {
                        var t = tilePool[x, z].cachedTransform;
                        var dist = Vector3.SqrMagnitude(t.localPosition - targetPos);
                        if (dist < d)
                        {
                            if (!(z == 0 && x == 2) && !(z == 0 && x == 0) && !(z == 2 && x == 0) && !(z == 2 && x == 2))
                            {
                                d = dist;
                                closestZ = z;
                                closestX = x;
                            }
                        }
                    }
                }
                SetCenter(closestX, closestZ);
            }
        }

        public void BeginFromStartTheme()
        {
            var t = Random.Range(0, themes.Count);
            while (t == currentTheme)
                t = Random.Range(0, themes.Count);
            currentTheme = t;
        }

        public void SetStartTheme(int t)
        {
            currentTheme = t;
        }

        public ThemeContainer GetCurrentTheme()
        {
            return themes[currentTheme];
        }


        void ShuffleAll()
        {
            for (int i = 0; i < themes.Count; i++)
            {
                int _grIndex = 0;
                var _g = themes[i].groups;
                _g.Shuffle();
                foreach (var tile in tilePool)
                {
                    tile.groups[i] = _g[_grIndex++];
                }
            }
        }

        public void SetScale()
        {
            var theme = themes[currentTheme];
            foreach (var t in tilePool)
            {
                t.transform.localScale = new Vector3(theme.tileScale, 7.6f, theme.tileScale);
            }
            var p = tilePool[0, 0].transform;
            m_tileSize = p.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * 2 * p.localScale.x;
            var halfSize = (m_tileSize * m_tileCount) / 2;
            for (int x = 0; x < m_tileCount; x++)
            {
                for (int z = 0; z < m_tileCount; z++)
                {
                    var t = tilePool[x, z];
                    t.transform.localPosition = new Vector3((m_tileSize * x) - halfSize + (m_tileSize / 2), m_yPos, (m_tileSize * z) - halfSize + (m_tileSize / 2));
                    if (z == 1 && x == 1)
                    {
                        t.empty = true;
                    }
                    else t.empty = false;
                }
            }
        }

        void OnPrepare()
        {
            foreach (var t in themes)
            {
                foreach (var g in t.groups)
                {
                    g.DisableAll();
                }
            }

            var theme = themes[currentTheme];

            m_cachedTransform.eulerAngles = new Vector3(0, theme.euler, 0);

            SetScale();

            ShuffleAll();

            foreach (var g in theme.groups)
            {
                g.EnableAll();
            }

            for (int x = 0; x < m_tileCount; x++)
            {
                for (int z = 0; z < m_tileCount; z++)
                {
                    var t = tilePool[x, z];
                    t.ShuffleObstacles();
                    if (t.empty)
                    {
                        t.currentGroup.Hide();
                    }
                }
            }

            Update();
        }

        int GetNext(int i)
        {
            return i >= m_tileCount - 1 ? 0 : i + 1;
        }

        int GetPrev(int i)
        {
            return i <= 0 ? m_tileCount - 1 : i - 1;
        }

        void SetCenter(int closestX, int closestZ)
        {
            var centerTile = tilePool[closestX, closestZ];
            m_currentCenter = centerTile.cachedTransform.localPosition;

            var halfSize = ((m_tileSize * m_tileCount) / 2) - (m_tileSize / 2);

            if ((closestX == 1 && closestZ == 0))
            {
                for (int x = 0; x < 3; x++)
                {
                    var tOld = tilePool[x, 2];
                    var tMid = tilePool[x, 1];
                    var tNew = tilePool[x, 0];
                    tOld.cachedTransform.localPosition = tNew.cachedTransform.localPosition - new Vector3(0, 0, halfSize);
                    tilePool[x, 2] = tMid;
                    tilePool[x, 1] = tNew;
                    tilePool[x, 0] = tOld;

                }
                onTileChange.RaiseEvent(tilePool[0, 0], tilePool[1, 0], tilePool[2, 0]);
                return;
            }
            else if ((closestX == 1 && closestZ == 2))
            {
                for (int x = 0; x < 3; x++)
                {
                    var tOld = tilePool[x, 0];
                    var tMid = tilePool[x, 1];
                    var tNew = tilePool[x, 2];
                    tOld.cachedTransform.localPosition = tNew.cachedTransform.localPosition + new Vector3(0, 0, halfSize);
                    tilePool[x, 0] = tMid;
                    tilePool[x, 1] = tNew;
                    tilePool[x, 2] = tOld;
                }
                onTileChange.RaiseEvent(tilePool[0, 2], tilePool[1, 2], tilePool[2, 2]);
                return;
            }
            else if ((closestX == 0 && closestZ == 1))
            {
                for (int x = 0; x < 3; x++)
                {
                    var tOld = tilePool[2, x];
                    var tMid = tilePool[1, x];
                    var tNew = tilePool[0, x];
                    tOld.cachedTransform.localPosition = tNew.cachedTransform.localPosition - new Vector3(halfSize, 0, 0);
                    tilePool[2, x] = tMid;
                    tilePool[1, x] = tNew;
                    tilePool[0, x] = tOld;
                }
                onTileChange.RaiseEvent(tilePool[0, 0], tilePool[0, 1], tilePool[0, 2]);
                return;
            }
            else if ((closestX == 2 && closestZ == 1))
            {
                for (int x = 0; x < 3; x++)
                {
                    var tOld = tilePool[0, x];
                    var tMid = tilePool[1, x];
                    var tNew = tilePool[2, x];
                    tOld.cachedTransform.localPosition = tNew.cachedTransform.localPosition + new Vector3(halfSize, 0, 0);
                    tilePool[0, x] = tMid;
                    tilePool[1, x] = tNew;
                    tilePool[2, x] = tOld;
                }
                onTileChange.RaiseEvent(tilePool[2, 0], tilePool[2, 1], tilePool[2, 2]);
                return;
            }
        }

        [System.Serializable]
        public class Theme
        {
            public string name;
            public int order;
            public Material groundMat;
            public Vector3 sunEuler;
            public int index;
            public int euler = 45;
            public List<LayoutGroup> groups = new List<LayoutGroup>();
        }



        [System.Serializable]
        public class LayoutGroup
        {
            public int index;
            public Obstacle[] obstacles;
            public Obstacle[] pool { get; set; }
            public List<Layout> layouts = new List<Layout>();
            public bool enabled = true;
            public bool isCenter;

            public void InitPool()
            {
                pool = new Obstacle[obstacles.Length];
                for (int i = 0; i < obstacles.Length; i++)
                {
                    var o = Instantiate(obstacles[i]);
                    o.Init();
                    o.cachedTransform.position = hiddenPosition;
                    pool[i] = o;
                }
                enabled = true;
            }

            public void EnableAll()
            {
                if (!enabled)
                {
                    enabled = true;
                    for (int i = 0; i < pool.Length; i++)
                    {
                        Enable(pool[i]);
                    }
                }
            }

            public void Hide()
            {
                if (pool == null) return;
                for (int i = 0; i < pool.Length; i++)
                {
                    var p = pool[i];
                    p.cachedTransform.position = hiddenPosition;
                }
            }

            public static void Enable(Obstacle obj)
            {
                obj.gameObject.SetActive(true);
            }

            public static void Disable(Obstacle obj)
            {
                obj.gameObject.SetActive(false);
            }

            public void DisableAll()
            {
                if (enabled)
                {
                    enabled = false;
                    for (int i = 0; i < pool.Length; i++)
                    {
                        Disable(pool[i]);
                    }
                }
            }

            public void Destroy()
            {
                if (pool == null) return;
                for (int i = 0; i < pool.Length; i++)
                {
                    pool[i].gameObject.SetActive(false);
                    Object.Destroy(pool[i].gameObject);
                }
            }

            public LayoutGroup Clone()
            {
                return new LayoutGroup() { index = index, layouts = layouts, obstacles = obstacles };
            }
        }

        [System.Serializable]
        public class Layout
        {
            public int index;
            public LayoutObject[] objects;

            [System.Serializable]
            public class LayoutObject
            {
                public int index;
                public Vector3 position;
                public bool canBeHidden;
                public bool replaceWithTeleport;
                public float euler;
            }
        }

        struct Point
        {
            public int x;
            public int z;
            public Point(int x, int z)
            {
                this.x = x;
                this.z = z;
            }
            public bool IsInsideRange(int range)
            {
                return x >= 0 && z >= 0 && x < range && z < range;
            }
        }
    }

}
