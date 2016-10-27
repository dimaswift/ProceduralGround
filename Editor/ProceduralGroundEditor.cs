using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using HandyUtilities;

namespace ProceduralGround
{
    [CustomEditor(typeof(GroundBuilder))]
    public class ProceduralGroundEditor : Editor
    {
        static readonly float[] isometricAngles = new float[] { 45, 135, 225, 315 };


        static GroundBuilder ground;
        static ThemeContainer theme;
        static List<GroundBuilder.LayoutGroup> groups;
        static Transform tile;

        [MenuItem("Procedural Ground/Rotate %J")]
        static void Rotate(MenuCommand command)
        {
            var selection = Selection.activeGameObject;
            if (selection)
            {
                var euler = selection.transform.eulerAngles;
                euler.y = Helper.To360Angle(euler.y + 90);
                selection.transform.eulerAngles = euler;
            }
        }

        [MenuItem("Procedural Ground/Adjust Iso Camera")]
        static void RotateEditorCam(MenuCommand command)
        {
            var cams = SceneView.GetAllSceneCameras();
            foreach (var c in cams)
            {
                c.transform.rotation = GameObject.FindGameObjectWithTag("MainCamera").transform.rotation;

            }
            SceneView.lastActiveSceneView.rotation = GameObject.FindGameObjectWithTag("MainCamera").transform.rotation;
        }

       

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            ground = target as GroundBuilder;

            theme = ground.themes.Count > 0 && ground.currentTheme < ground.themes.Count ? ground.themes[ground.currentTheme] : null;

            if (theme == null) return;

            groups = theme.groups;

          

            if(ground.GetComponentInChildren<Tile>() == null)
            {
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<Tile>();
                plane.transform.SetParent(ground.transform);
                plane.transform.localPosition = Vector3.zero;
                plane.transform.localEulerAngles = Vector3.zero;
                ground.tilePrefab = plane;
                EditorUtility.SetDirty(ground);
            }

            tile = ground.transform.GetChild(0);

            if (GUILayout.Button("Create Layout Group"))
            {
                var group = new GroundBuilder.LayoutGroup();
                group.obstacles = FindObjectsOfType<Obstacle>();
                for (int i = 0; i < group.obstacles.Length; i++)
                {
                    group.obstacles[i] = PrefabUtility.GetPrefabParent(group.obstacles[i]) as Obstacle;
                }
                group.index = groups.Count;
                ground.currentGroup = groups.Count;
                groups.Add(group);
                var layout = new GroundBuilder.Layout();
                var obstacles = FindObjectsOfType<Obstacle>();
                ground.spawned = obstacles;
                layout.objects = new GroundBuilder.Layout.LayoutObject[obstacles.Length];
                for (int i = 0; i < obstacles.Length; i++)
                {
                    var obs = obstacles[i];
                    layout.objects[i] = new GroundBuilder.Layout.LayoutObject()
                    {
                        euler = obs.transform.eulerAngles.y,
                        index = i,
                        position = tile.InverseTransformPoint(obs.transform.position)
                    };
                }
                layout.index = group.layouts.Count;
                group.layouts.Add(layout);
                Undo.RecordObject(target, "Create Layout Group");
                EditorUtility.SetDirty(ground);
                EditorUtility.SetDirty(theme);
            }
            if (GUILayout.Button("Add Layout"))
            {
                if (ground.currentGroup >= 0 && ground.currentGroup < groups.Count)
                {
                    var group = groups[ground.currentGroup];
                    var layout = new GroundBuilder.Layout();
                    var obstacles = ground.spawned == null || ground.spawned.Length == 0 ? FindObjectsOfType<Obstacle>() : ground.spawned;
                    layout.objects = new GroundBuilder.Layout.LayoutObject[obstacles.Length];
                    for (int i = 0; i < obstacles.Length; i++)
                    {
                        var obs = obstacles[i];
                        layout.objects[i] = new GroundBuilder.Layout.LayoutObject()
                        {
                            euler = obs.transform.eulerAngles.y,
                            index = i,
                            position = tile.InverseTransformPoint(obs.transform.position)
                        };
                    }
                    layout.index = group.layouts.Count;
                    group.layouts.Add(layout);
                    Undo.RecordObject(target, "Add layout");
                    EditorUtility.SetDirty(ground);
                    EditorUtility.SetDirty(theme);
                }
                else Debug.Log(string.Format("no group found with {0} index.", ground.currentGroup));
            }
            if (GUILayout.Button("Save Layout"))
            {
                if (ground.currentGroup >= 0 && ground.currentGroup < groups.Count)
                {
                    var group = groups[ground.currentGroup];
                    var layout = new GroundBuilder.Layout();
                    if (ground.spawned == null)
                    {

                        Debug.Log(string.Format("{0}", "saving failed"));
                        return;
                    }
                    var list = new List<GroundBuilder.Layout.LayoutObject>();

                    for (int i = 0; i < ground.spawned.Length; i++)
                    {
                        var obs = ground.spawned[i];
                        if (obs != null)
                        {
                            list.Add(new GroundBuilder.Layout.LayoutObject()
                            {
                                euler = obs.transform.eulerAngles.y,
                                index = i,
                                position = tile.InverseTransformPoint(obs.transform.position)
                            });
                        }
                    }
                    layout.objects = list.ToArray();
                    group.layouts[ground.currentLayout] = layout;
                    EditorUtility.SetDirty(ground);
                    EditorUtility.SetDirty(theme);
                }
                else Debug.Log(string.Format("no group found with {0} index.", ground.currentGroup));
            }
            if (GUILayout.Button("Spawn Group"))
            {
                SpawnCurrent();
            }

            if (GUILayout.Button("Delete Spawned"))
            {
                for (int i = 0; i < ground.spawned.Length; i++)
                {
                    if (ground.spawned[i] != null)
                        Undo.DestroyObjectImmediate(ground.spawned[i].gameObject);
                }
            }

            if (GUILayout.Button("Delete Obstacles Only"))
            {
                for (int i = 0; i < ground.spawned.Length; i++)
                {
                    var o = ground.spawned[i];
                    if (o != null && o.obstacleType == Obstacle.Type.Obstacle)
                        Undo.DestroyObjectImmediate(o.gameObject);
                }
            }

            if (GUILayout.Button("Randomize Euler"))
            {
                var obstacles = FindObjectsOfType<Obstacle>();
                for (int i = 0; i < obstacles.Length; i++)
                {
                    obstacles[i].transform.eulerAngles = new Vector3(0, isometricAngles.Random(), 0);
                }
            }

            if (GUILayout.Button("Find Spawned"))
            {
                ground.spawned = FindObjectsOfType<Obstacle>();
                EditorUtility.SetDirty(ground);
                EditorUtility.SetDirty(theme);
            }

            if (GUILayout.Button("Shuffle All"))
            {
                var obstacles = ground.spawned == null || ground.spawned.Length == 0 ? FindObjectsOfType<Obstacle>() : ground.spawned;
                var tileBounds = ground.transform.GetChild(0).GetComponent<BoxCollider>().bounds.extents * .7f;
                foreach (var obstacle in obstacles)
                {
                    var pos = new Vector3(Random.Range(-tileBounds.x, tileBounds.x), 0, Random.Range(-tileBounds.z, tileBounds.z));
                    foreach (var other in obstacles)
                    {
                        if (other != obstacle)
                        {
                            int c = 0;
                            while (Vector3.SqrMagnitude(other.transform.position - pos) < 50)
                            {
                                c++;
                                if (c > 100)
                                {
                                    Debug.Log(string.Format("{0}", "too much man..."));
                                    break;
                                }
                                pos = new Vector3(Random.Range(-tileBounds.x, tileBounds.x), 0, Random.Range(-tileBounds.z, tileBounds.z));
                            }
                        }
                    }
                    obstacle.transform.position = ground.transform.InverseTransformPoint(pos);
                }
            }
            if (GUILayout.Button("Fix Height"))
            {
                foreach (var t in ground.themes)
                {
                    foreach (var g in t.groups)
                    {
                        foreach (var l in g.layouts)
                        {
                            foreach (var o in l.objects)
                            {
                                o.position.y = 0;
                            }
                        }
                    }
                }

                EditorUtility.SetDirty(ground);
                EditorUtility.SetDirty(theme);
            }

            if (GUILayout.Button("Spawn Next"))
            {
                SpawnNext();
            }

            if (GUILayout.Button("Sort"))
            {
                ground.themes.Sort((t1, t2) => t1.order.CompareTo(t2.order));
                EditorUtility.SetDirty(ground);
                EditorUtility.SetDirty(theme);
            }

            if (GUILayout.Button("Save Sun Euler"))
            {
                theme = ground.themes[ground.currentTheme];
                EditorUtility.SetDirty(ground);
                EditorUtility.SetDirty(theme);
            }
        }

        void SpawnNext()
        {
            ground.currentLayout++;
            if (ground.currentLayout == ground.themes[ground.currentTheme].groups[ground.currentGroup].layouts.Count)
            {
                ground.currentLayout = 0;
                ground.currentGroup++;
                if (ground.currentGroup == ground.themes[ground.currentTheme].groups.Count)
                {
                    ground.currentGroup = 0;
                    ground.currentTheme++;
                    if (ground.currentTheme >= ground.themes.Count)
                        ground.currentTheme = 0;
                }
            }
            SpawnCurrent();

        }


        void SpawnCurrent()
        {
            if (ground.currentGroup >= 0 && ground.currentGroup < groups.Count)
            {
                var group = groups[ground.currentGroup];
                if (group != null)
                {
                    var layout = group.layouts[ground.currentLayout];
                    var obstacles = FindObjectsOfType<Obstacle>();
                    ground.spawned = new Obstacle[layout.objects.Length];
                    ground.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = theme.groundMaterial;
                    ground.transform.eulerAngles = new Vector3(0, theme.euler, 0);
                    for (int i = 0; i < obstacles.Length; i++)
                    {
                        Undo.DestroyObjectImmediate(obstacles[i].gameObject);
                    }
                    for (int i = 0; i < layout.objects.Length; i++)
                    {
                        var l = layout.objects[i];

                        var obs = PrefabUtility.InstantiatePrefab(group.obstacles[i]) as Obstacle;
                        ground.spawned[i] = obs;
                        Undo.RegisterCreatedObjectUndo(obs, "SpawnLayout");
                        obs.transform.position = tile.transform.TransformPoint(l.position);
                        obs.transform.eulerAngles = new Vector3(0, l.euler, 0);
                    }
                }
            }

            else Debug.Log(string.Format("no group found with {0} index.", ground.currentGroup));
        }

        // new ProceduralGround.Layout.LayoutObject() { euler = obs.transform.eulerAngles.y, index = obs.index, position = tile.InverseTransformPoint(obs.transform.position) };
    }

}
