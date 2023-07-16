using UnityEditor;
using UnityEngine;
using System;

namespace SmartRooms.Editor
{
    /// <summary>
    /// Custom attribute which hides the script field in the Unity inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HideScriptField : Attribute { }
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class HideScriptEditor : UnityEditor.Editor
    {
        private bool hideScriptField;

        protected void OnEnable()
        {
            hideScriptField = target.GetType().GetCustomAttributes(typeof(HideScriptField), false).Length > 0;
        }

        public override void OnInspectorGUI()
        {
            if (hideScriptField)
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                DrawPropertiesExcluding(serializedObject, "m_Script");
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                base.OnInspectorGUI();
            }
        }
    }
}