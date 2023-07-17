using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SmartRooms.Generator.Patterns
{
    [CreateAssetMenu(menuName = "SmartRooms/Generation/ObjectPattern")]
    public class ObjectPattern : ScriptableObject
    {
        [field: SerializeField] public SpawningPattern Pattern { get; private set; }
        
        [Tooltip("When set to true then matching positions are only used once regardless of rotation or flipping.")]
        [field: SerializeField] public bool UniquePositions { get; private set; } = true;
        [field: SerializeField] public List<SpawnableObject> SpawnableObjects { get; private set; }

        public void Initialize()
        {
            Pattern.Initialize();
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// This class is based mostly on RuleTileEditor from Unity.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ObjectPattern))]
    internal class SpawningPatternEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Whether the SpawningPattern can extend its neighbors beyond directly adjacent ones
        /// </summary>
        public bool extendNeighbor;

        private ObjectPattern pattern => target as ObjectPattern;

        private SerializedProperty _foliageItems;
        private SerializedProperty _uniquePositions;

        private void OnEnable()
        {
            _foliageItems = serializedObject.FindProperty("<SpawnableObjects>k__BackingField");
            _uniquePositions = serializedObject.FindProperty("<UniquePositions>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Undo.RecordObject(target, "Spawning Pattern");

            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Pattern", EditorStyles.boldLabel);
            extendNeighbor = EditorGUILayout.Toggle("Auto extend neighbors", extendNeighbor);


            EditorGUILayout.Space(5f);
            GUILayout.Label("Rule");

            BoundsInt bounds = GetRuleGUIBounds(pattern.Pattern.GetBounds(), pattern.Pattern);
            float sideLength = EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.singleLineHeight * Mathf.Min(bounds.size.magnitude - 1, 10);
            Rect rect = EditorGUILayout.GetControlRect(false, sideLength, GUILayout.Width(sideLength));
            Rect matrixRect = new Rect(rect.x, rect.y, rect.height, rect.height);
            RuleMatrixOnGUI(matrixRect, bounds, pattern.Pattern);
            
            EditorGUILayout.Space(5f);
            
            GUILayout.Label("Flipping rule");
            rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2, GUILayout.Width(EditorGUIUtility.singleLineHeight * 2));
            
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            float w = rect.width;
            float h = rect.height;
            for (int y = 0; y <= 1; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }

            for (int x = 0; x <= 1; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }
            Handles.color = Color.white;
            
            RuleTransformOnGUI(rect, pattern.Pattern.RuleTransform);
            RuleTransformUpdate(rect, pattern.Pattern);
            
            GUILayout.Space(12);
            if (GUILayout.Button("Reset Pattern", GUILayout.Height(40), GUILayout.Width(150)))
            {
                pattern.Pattern.Clear();
            }

            GUILayout.Space(12);
            EditorGUILayout.PropertyField(_uniquePositions);
            EditorGUILayout.PropertyField(_foliageItems);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            GUILayout.Space(48);
        }

        /// <summary>
        /// Get the GUI bounds for a Rule.
        /// </summary>
        /// <param name="bounds">Cell bounds of the Rule.</param>
        /// <param name="pattern">Rule to get GUI bounds for.</param>
        /// <returns>The GUI bounds for a rule.</returns>
        public virtual BoundsInt GetRuleGUIBounds(BoundsInt bounds, SpawningPattern pattern)
        {
            if (extendNeighbor)
            {
                bounds.xMin--;
                bounds.yMin--;
                bounds.xMax++;
                bounds.yMax++;
            }

            bounds.xMin = Mathf.Min(bounds.xMin, -1);
            bounds.yMin = Mathf.Min(bounds.yMin, -1);
            bounds.xMax = Mathf.Max(bounds.xMax, 2);
            bounds.yMax = Mathf.Max(bounds.yMax, 2);
            return bounds;
        }

        public virtual void RuleMatrixOnGUI(Rect rect, BoundsInt bounds, SpawningPattern spawningPattern)
        {
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            int size = Mathf.Max(bounds.size.x, bounds.size.y);
            float w = rect.width / size;
            float h = rect.height / size;

            for (int y = 0; y <= size; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }

            for (int x = 0; x <= size; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }

            Handles.color = Color.white;

            Dictionary<Vector3Int, int> neighbors = spawningPattern.GetNeighbors();

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    Rect r = new Rect(rect.xMin + (x - bounds.xMin) * w, rect.yMin + (-y + bounds.yMax - 1) * h, w - 1, h - 1);
                    RuleMatrixIconOnGUI(spawningPattern, neighbors, pos, r);
                }
            }
        }


        /// <summary>
        /// Draws a transform matching rule
        /// </summary>
        /// <param name="rect">Rect to draw on</param>
        /// <param name="ruleTransform">The transform matching criteria</param>
        public virtual void RuleTransformOnGUI(Rect rect, SpawningPattern.Transform ruleTransform)
        {
            switch (ruleTransform)
            {
                // case SpawningPattern.Transform.Rotated:
                //     GUI.DrawTexture(rect, autoTransforms[0]);
                //     break;
                case SpawningPattern.Transform.MirrorX:
                    GUI.DrawTexture(rect, autoTransforms[1]);
                    break;
                case SpawningPattern.Transform.MirrorY:
                    GUI.DrawTexture(rect, autoTransforms[2]);
                    break;
                case SpawningPattern.Transform.Fixed:
                    GUI.DrawTexture(rect, autoTransforms[3]);
                    break;
                case SpawningPattern.Transform.MirrorXY:
                    GUI.DrawTexture(rect, autoTransforms[4]);
                    break;
            }

            GUI.Label(rect, new GUIContent("", ruleTransform.ToString()));
        }


        /// <summary>
        /// Handles a transform matching Rule update from user mouse input
        /// </summary>
        /// <param name="rect">Rect containing transform matching Rule GUI</param>
        /// <param name="tilingPattern">Tiling Rule to update transform matching rule</param>
        public void RuleTransformUpdate(Rect rect, SpawningPattern tilingPattern)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                tilingPattern.RuleTransform = (SpawningPattern.Transform)(int)Mathf.Repeat((int)tilingPattern.RuleTransform + GetMouseChange(), Enum.GetValues(typeof(SpawningPattern.Transform)).Length);
                GUI.changed = true;
                Event.current.Use();
            }
        }

        /// <summary>
        /// Gets the offset change for a mouse click input
        /// </summary>
        /// <returns>The offset change for a mouse click input</returns>
        public static int GetMouseChange()
        {
            return Event.current.button == 1 ? -1 : 1;
        }

        /// <summary>
        /// Determines the current mouse position is within the given Rect.
        /// </summary>
        /// <param name="rect">Rect to test mouse position for.</param>
        /// <returns>True if the current mouse position is within the given Rect. False otherwise.</returns>
        public virtual bool ContainsMousePosition(Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        /// <summary>
        /// Draws a Rule Matrix Icon for the given matching Rule for a RuleTile with the given position
        /// </summary>
        /// <param name="tilingPattern">Tile to draw rule for.</param>
        /// <param name="neighbors">A dictionary of neighbors</param>
        /// <param name="position">The relative position of the neighbor matching Rule</param>
        /// <param name="rect">GUI Rect to draw icon at</param>
        public void RuleMatrixIconOnGUI(SpawningPattern tilingPattern, Dictionary<Vector3Int, int> neighbors, Vector3Int position, Rect rect)
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (neighbors.ContainsKey(position))
                {
                    RuleOnGUI(rect, position, neighbors[position]);
                    RuleTooltipOnGUI(rect, neighbors[position]);
                }

                RuleNeighborUpdate(rect, tilingPattern, neighbors, position);
            }
        }

        /// <summary>
        /// Handles a neighbor matching Rule update from user mouse input
        /// </summary>
        /// <param name="rect">Rect containing neighbor matching Rule GUI</param>
        /// <param name="tilingPattern">Tiling Rule to update neighbor matching rule</param>
        /// <param name="neighbors">A dictionary of neighbors</param>
        /// <param name="position">The relative position of the neighbor matching Rule</param>
        public void RuleNeighborUpdate(Rect rect, SpawningPattern tilingPattern, Dictionary<Vector3Int, int> neighbors, Vector3Int position)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                var allConsts = pattern.Pattern.m_NeighborType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                var neighborConsts = allConsts.Select(c => (int)c.GetValue(null)).ToList();
                neighborConsts.Sort();

                if (neighbors.ContainsKey(position))
                {
                    int oldIndex = neighborConsts.IndexOf(neighbors[position]);
                    int newIndex = oldIndex + GetMouseChange();
                    if (newIndex >= 0 && newIndex < neighborConsts.Count)
                    {
                        newIndex = (int)Mathf.Repeat(newIndex, neighborConsts.Count);
                        neighbors[position] = neighborConsts[newIndex];
                    }
                    else
                    {
                        neighbors.Remove(position);
                    }
                }
                else
                {
                    neighbors.Add(position, neighborConsts[GetMouseChange() == 1 ? 0 : (neighborConsts.Count - 1)]);
                }

                tilingPattern.ApplyNeighbors(neighbors);

                GUI.changed = true;
                Event.current.Use();
            }
        }

        /// <summary>
        /// Draws a tooltip for the neighbor matching rule
        /// </summary>
        /// <param name="rect">Rect to draw on</param>
        /// <param name="neighbor">The index to the neighbor matching criteria</param>
        public void RuleTooltipOnGUI(Rect rect, int neighbor)
        {
            var allConsts = pattern.Pattern.m_NeighborType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var c in allConsts)
            {
                if ((int)c.GetValue(null) == neighbor)
                {
                    GUI.Label(rect, new GUIContent("", c.Name));
                    break;
                }
            }
        }

        /// <summary>
        /// Draws a neighbor matching rule
        /// </summary>
        /// <param name="rect">Rect to draw on</param>
        /// <param name="position">The relative position of the arrow from the center</param>
        /// <param name="neighbor">The index to the neighbor matching criteria</param>
        public virtual void RuleOnGUI(Rect rect, Vector3Int position, int neighbor)
        {
            switch (neighbor)
            {
                case SpawningPattern.Neighbor.Tile:
                    if (position is { x: 0, y: 0 })
                    {
                        GUI.DrawTexture(rect, arrows[10]);
                    }
                    else
                    {
                        GUI.DrawTexture(rect, arrows[GetArrowIndex(position)]);
                    }
                    break;
                case SpawningPattern.Neighbor.Air:
                    GUI.DrawTexture(rect, arrows[9]);
                    break;
                default:
                    var style = new GUIStyle();
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 10;
                    GUI.Label(rect, neighbor.ToString(), style);
                    break;
            }
        }

        /// <summary>
        /// Gets the index for a Rule with the RuleTile to display an arrow.
        /// </summary>
        /// <param name="position">The relative position of the arrow from the center.</param>
        /// <returns>Returns the index for a Rule with the RuleTile to display an arrow.</returns>
        public virtual int GetArrowIndex(Vector3Int position)
        {
            if (Mathf.Abs(position.x) == Mathf.Abs(position.y))
            {
                if (position.x < 0 && position.y > 0)
                    return 0;
                else if (position.x > 0 && position.y > 0)
                    return 2;
                else if (position.x < 0 && position.y < 0)
                    return 6;
                else if (position.x > 0 && position.y < 0)
                    return 8;
            }
            else if (Mathf.Abs(position.x) > Mathf.Abs(position.y))
            {
                if (position.x > 0)
                    return 5;
                else
                    return 3;
            }
            else
            {
                if (position.y > 0)
                    return 1;
                else
                    return 7;
            }

            return -1;
        }

        private const string s_Block = "iVBORw0KGgoAAAANSUhEUgAAABYAAAAXCAYAAAAP6L+eAAAACXBIWXMAAAYzAAAGMwH7vU8fAAAAGXRFWHRTb2Z0d2FyZQB3d3cuaW5rc2NhcGUub3Jnm+48GgAAAdpJREFUOI1j/P//PwMtABNNTB2SBrMQUvDm4w3jez8eRX/4/Vn0z//fbHwsvO+lOcW3KQqYbGNgYPiDSx8jrsi7/+m42bVPj9p2vjxpcebjLe6////C5ZS5ZP74iFte0uaVm6ov6jKPaIPPvdobveP1ifaNL47I4vONDq/S5xR5n3mW4p4F6HIYYXzz/WHrba+OdxAylIGBgeHK53u8U+6vSzv7dlcFQYNvfH7ctvnlURlChsLAna9POA+9upj15s1NKZwGP/541m3Di8PmxBoKA2tfHJS9++d+FU6DH3x7Fnr980N2Ug3+/e8vw/1vz/VwGvzu12eR/wzkZfHPf76J4DT4779/rGSZysDA8JfxP4peFIN5WDk+kGswJxMril4UgxW4JY6LsPGTZbA0p9htnAarCaov9JewvUGqocb86p8UOKQW4DSYgUH0i7GA6kJFLokfxBrKzsTKEC7tuFdFyHwXHoMZGIxF3TryFINXS3OI/CbG0ArV6JP2klZx6HK4CiHGM692dux6czZm84sjUv+wqDHmV/8ULuW4z15KI46BQf0zsQYzMDAwMLz6eEbp1rcXFQ+/v9D6+ueH6N///5g5mNk+SLOL3Vbilp6P7n2iDaYEDL2qCQCRN7vE21kF3wAAAABJRU5ErkJggg==";

        private const string s_XIconString = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABoSURBVDhPnY3BDcAgDAOZhS14dP1O0x2C/LBEgiNSHvfwyZabmV0jZRUpq2zi6f0DJwdcQOEdwwDLypF0zHLMa9+NQRxkQ+ACOT2STVw/q8eY1346ZlE54sYAhVhSDrjwFymrSFnD2gTZpls2OvFUHAAAAABJRU5ErkJggg==";
        private const string s_Arrow0 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPzZExDoQwDATzE4oU4QXXcgUFj+YxtETwgpMwXuFcwMFSRMVKKwzZcWzhiMg91jtg34XIntkre5EaT7yjjhI9pOD5Mw5k2X/DdUwFr3cQ7Pu23E/BiwXyWSOxrNqx+ewnsayam5OLBtbOGPUM/r93YZL4/dhpR/amwByGFBz170gNChA6w5bQQMqramBTgJ+Z3A58WuWejPCaHQAAAABJRU5ErkJggg==";
        private const string s_Arrow1 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPxYzBDYAgEATpxYcd+PVr0fZ2siZrjmMhFz6STIiDs8XMlpEyi5RkO/d66TcgJUB43JfNBqRkSEYDnYjhbKD5GIUkDqRDwoH3+NgTAw+bL/aoOP4DOgH+iwECEt+IlFmkzGHlAYKAWF9R8zUnAAAAAElFTkSuQmCC";
        private const string s_Arrow2 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAC0SURBVDhPjVE5EsIwDMxPKFKYF9CagoJH8xhaMskLmEGsjOSRkBzYmU2s9a58TUQUmCH1BWEHweuKP+D8tphrWcAHuIGrjPnPNY8X2+DzEWE+FzrdrkNyg2YGNNfRGlyOaZDJOxBrDhgOowaYW8UW0Vau5ZkFmXbbDr+CzOHKmLinAXMEePyZ9dZkZR+s5QX2O8DY3zZ/sgYcdDqeEVp8516o0QQV1qeMwg6C91toYoLoo+kNt/tpKQEVvFQAAAAASUVORK5CYII=";
        private const string s_Arrow3 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAB2SURBVDhPzY1LCoAwEEPnLi48gW5d6p31bH5SMhp0Cq0g+CCLxrzRPqMZ2pRqKG4IqzJc7JepTlbRZXYpWTg4RZE1XAso8VHFKNhQuTjKtZvHUNCEMogO4K3BhvMn9wP4EzoPZ3n0AGTW5fiBVzLAAYTP32C2Ay3agtu9V/9PAAAAAElFTkSuQmCC";
        private const string s_Arrow5 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPnY3BCYBADASvFx924NevRdvbyoLBmNuDJQMDGjNxAFhK1DyUQ9fvobCdO+j7+sOKj/uSB+xYHZAxl7IR1wNTXJeVcaAVU+614uWfCT9mVUhknMlxDokd15BYsQrJFHeUQ0+MB5ErsPi/6hO1AAAAAElFTkSuQmCC";
        private const string s_Arrow6 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACaSURBVDhPxZExEkAwEEVzE4UiTqClUDi0w2hlOIEZsV82xCZmQuPPfFn8t1mirLWf7S5flQOXjd64vCuEKWTKVt+6AayH3tIa7yLg6Qh2FcKFB72jBgJeziA1CMHzeaNHjkfwnAK86f3KUafU2ClHIJSzs/8HHLv09M3SaMCxS7ljw/IYJWzQABOQZ66x4h614ahTCL/WT7BSO51b5Z5hSx88AAAAAElFTkSuQmCC";
        private const string s_Arrow7 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABQSURBVDhPYxh8QNle/T8U/4MKEQdAmsz2eICx6W530gygr2aQBmSMphkZYxqErAEXxusKfAYQ7XyyNMIAsgEkaYQBkAFkaYQBsjXSGDAwAAD193z4luKPrAAAAABJRU5ErkJggg==";
        private const string s_Arrow8 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPxZE9DoAwCIW9iUOHegJXHRw8tIdx1egJTMSHAeMPaHSR5KVQ+KCkCRF91mdz4VDEWVzXTBgg5U1N5wahjHzXS3iFFVRxAygNVaZxJ6VHGIl2D6oUXP0ijlJuTp724FnID1Lq7uw2QM5+thoKth0N+GGyA7IA3+yM77Ag1e2zkey5gCdAg/h8csy+/89v7E+YkgUntOWeVt2SfAAAAABJRU5ErkJggg==";
        private const string s_MirrorX = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG1JREFUOE+lj9ENwCAIRB2IFdyRfRiuDSaXAF4MrR9P5eRhHGb2Gxp2oaEjIovTXSrAnPNx6hlgyCZ7o6omOdYOldGIZhAziEmOTSfigLV0RYAB9y9f/7kO8L3WUaQyhCgz0dmCL9CwCw172HgBeyG6oloC8fAAAAAASUVORK5CYII=";
        private const string s_MirrorY = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG9JREFUOE+djckNACEMAykoLdAjHbPyw1IOJ0L7mAejjFlm9hspyd77Kk+kBAjPOXcakJIh6QaKyOE0EB5dSPJAiUmOiL8PMVGxugsP/0OOib8vsY8yYwy6gRyC8CB5QIWgCMKBLgRSkikEUr5h6wOPWfMoCYILdgAAAABJRU5ErkJggg==";
        private const string s_MirrorXY = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABl0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC4yMfEgaZUAAAHkSURBVDhPrVJLSwJRFJ4cdXwjPlrVJly1kB62cpEguElXKgYKIpaC+EIEEfGxLqI/UES1KaJlEdGmRY9ltCsIWrUJatGm0eZO3xkHIsJdH3zce+ec75z5zr3cf2MMmLdYLA/BYFA2mUyPOPvwnR+GR4PXaDQLLpfrKpVKSb1eT6bV6XTeocAS4sIw7S804BzEZ4IgsGq1ykhcr9dlj8czwPdbxJdBMyX/As/zLiz74Ar2J9lsVulcKpUYut5DnEbsHFwEx8AhtFqtGViD6BOc1ul0B5lMRhGXy2Wm1+ufkBOE/2fsL1FsQpXCiCAcQiAlk0kJRZjf7+9TRxI3Gg0WCoW+IpGISHHERBS5UKUch8n2K5WK3O125VqtpqydTkdZie12W261WjIVo73b7RZVKccZDIZ1q9XaT6fTLB6PD9BFKhQKjITFYpGFw+FBNBpVOgcCARH516pUGZYZXk5R4B3efLBxDM9f1CkWi/WR3ICtGVh6Rd4NPE+p0iEgmkSRLRoMEjYhHpA4kUiIOO8iZRU8AmnadK2/QOOfhnjPZrO95fN5Zdq5XE5yOBwvuKoNxGfBkQ8FzXkPprnj9Xrfm82mDI8fsLON3x5H/Od+RwHdLfDds9vtn0aj8QoF6QH9JzjuG3acpxmu1RgPAAAAAElFTkSuQmCC";
        private const string s_Rotated = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAHdJREFUOE+djssNwCAMQxmIFdgx+2S4Vj4YxWlQgcOT8nuG5u5C732Sd3lfLlmPMR4QhXgrTQaimUlA3EtD+CJlBuQ7aUAUMjEAv9gWCQNEPhHJUkYfZ1kEpcxDzioRzGIlr0Qwi0r+Q5rTgM+AAVcygHgt7+HtBZs/2QVWP8ahAAAAAElFTkSuQmCC";
        private const string s_Fixed = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMjHxIGmVAAAA50lEQVQ4T51Ruw6CQBCkwBYKWkIgQAs9gfgCvgb4BML/qWBM9Bdo9QPIuVOQ3JIzosVkc7Mzty9NCPE3lORaKMm1YA/LsnTXdbdhGJ6iKHoVRTEi+r4/OI6zN01Tl/XM7HneLsuyW13XU9u2ous6gYh3kiR327YPsp6ZgyDom6aZYFqiqqqJ8mdZz8xoca64BHjkZT0zY0aVcQbysp6Z4zj+Vvkp65mZttxjOSozdkEzD7KemekcxzRNHxDOHSDiQ/DIy3pmpjtuSJBThStGKMtyRKSOLnSm3DCMz3f+FUpyLZTkOgjtDSWORSDbpbmNAAAAAElFTkSuQmCC";

        private static Texture2D[] s_AutoTransforms;

        /// <summary>
        /// Arrays of textures used for marking transform Rule matches
        /// </summary>
        public static Texture2D[] autoTransforms
        {
            get
            {
                if (s_AutoTransforms == null)
                {
                    s_AutoTransforms = new Texture2D[5];
                    s_AutoTransforms[0] = Base64ToTexture(s_Rotated);
                    s_AutoTransforms[1] = Base64ToTexture(s_MirrorX);
                    s_AutoTransforms[2] = Base64ToTexture(s_MirrorY);
                    s_AutoTransforms[3] = Base64ToTexture(s_Fixed);
                    s_AutoTransforms[4] = Base64ToTexture(s_MirrorXY);
                }

                return s_AutoTransforms;
            }
        }

        /// <summary>
        /// Converts a Base64 string to a Texture2D
        /// </summary>
        /// <param name="base64">Base64 string containing image data</param>
        /// <returns>Texture2D containing an image from the given Base64 string</returns>
        public static Texture2D Base64ToTexture(string base64)
        {
            Texture2D t = new Texture2D(1, 1);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.LoadImage(Convert.FromBase64String(base64));
            return t;
        }

        private static Texture2D[] s_Arrows;

        /// <summary>
        /// Array of arrow textures used for marking positions for Rule matches
        /// </summary>
        public static Texture2D[] arrows
        {
            get
            {
                if (s_Arrows == null)
                {
                    s_Arrows = new Texture2D[11];
                    s_Arrows[0] = Base64ToTexture(s_Arrow0);
                    s_Arrows[1] = Base64ToTexture(s_Arrow1);
                    s_Arrows[2] = Base64ToTexture(s_Arrow2);
                    s_Arrows[3] = Base64ToTexture(s_Arrow3);
                    s_Arrows[5] = Base64ToTexture(s_Arrow5);
                    s_Arrows[6] = Base64ToTexture(s_Arrow6);
                    s_Arrows[7] = Base64ToTexture(s_Arrow7);
                    s_Arrows[8] = Base64ToTexture(s_Arrow8);
                    s_Arrows[9] = Base64ToTexture(s_XIconString);
                    s_Arrows[10] = Base64ToTexture(s_Block);
                }

                return s_Arrows;
            }
        }
    }
#endif
}