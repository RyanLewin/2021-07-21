#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/*
* Main Editor(s): Ryan Lewin
* Date Created: 23/04/21
* Date Modified: 28/04/21
* 
* A custom property drawer for property fields that allows for the property to show the variable 
* without a dropdown and also a list of listeners
*/

namespace PropertyListenerTool
{
    public class PropertyGUI : PropertyDrawer
    {
        private bool dropdownOpen = false; //Holds whether the dropdown showing methods is open or not

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //Gets the number of listeners to add to the line count, then sets the property drawer's height
            var n = property.FindPropertyRelative("m_event").FindPropertyRelative("m_listenerPaths");
            int arrayLength = 2;
            if (dropdownOpen)
            {
                n.Next(true);
                n.Next(true);
                arrayLength += n.intValue;
            }
            return EditorGUIUtility.singleLineHeight * arrayLength + EditorGUIUtility.standardVerticalSpacing * (arrayLength-1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            //Add tooltip onto the property for when hovering over
            var tooltip = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true);
            if (tooltip != null && tooltip.Length > 0) label.tooltip = ((TooltipAttribute)tooltip[0]).tooltip; 

            string propertyName = label.text;
            label.text = $"{propertyName}: Property";
            position = new Rect(position.x, position.y, position.size.x, EditorGUIUtility.singleLineHeight);

            //Get the variable value in the property and display it
            //Floats and ints don't need the prefix as it disables the drag to change functionality 
            SerializedProperty variable = property.FindPropertyRelative("m_variable");
            if (variable.propertyType == SerializedPropertyType.Float)
            {
                variable.floatValue = EditorGUI.FloatField(position, label, variable.floatValue);
            }
            else if (variable.propertyType == SerializedPropertyType.Integer)
            {
                variable.intValue = EditorGUI.IntField(position, label, variable.intValue);
            }
            else
            {
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                EditorGUI.PropertyField(position, variable, GUIContent.none);
            }

            //Indent to show its a child of the property
            EditorGUI.indentLevel += 1;

            //Get list of listener paths from properties m_event then move through until you get to the values
            //Once there, adds them all to a list for ease of use
            var n = property.FindPropertyRelative("m_event").FindPropertyRelative("m_listenerPaths");
            if (n != null)
            {
                int arrayLength = 0;
                n.Next(true);
                n.Next(true);
                arrayLength = n.intValue;
                n.Next(true);
                // n.Next(false);

                List<string> values = new List<string>(arrayLength);
                for (int i = 0; i < arrayLength; i++)
                {
                    if(n.propertyType == SerializedPropertyType.String)
                    {
                        values.Add(n.stringValue);
                    }
                    n.Next(false);
                }

                //moves position down a line and indents again
                position = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel += 1;

                //if the list is empty then tell the user nothing is listening, else
                //display a foldout area showing all the listeners in a list
                if (values.Count <= 0)
                {
                    label.text = $"Currently no listeners attached to {propertyName}. May be added in runtime.";
                    EditorGUI.LabelField(position, label);
                }
                else
                {
                    label.text = $"{(dropdownOpen ? "Hide" : "Show")} Listeners Assigned to {propertyName}:";
                    dropdownOpen = EditorGUI.Foldout(position, dropdownOpen, label); //Sets to true if the foldout is shown or false if not
                    if (dropdownOpen)
                    {
                        GUIContent listenerLabel = new GUIContent();
                        if (tooltip != null && tooltip.Length > 0) listenerLabel.tooltip = label.tooltip;
                        EditorGUI.indentLevel += 1;
                        foreach (var name in values)
                        {
                            position = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                            listenerLabel.text = name;
                            EditorGUI.LabelField(position, listenerLabel);
                        }
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(IntProperty))]
    public class IntPropertyGUI : PropertyGUI {}

    [CustomPropertyDrawer(typeof(FloatProperty))]
    public class FloatPropertyGUI : PropertyGUI {}

    [CustomPropertyDrawer(typeof(BoolProperty))]
    public class BoolPropertyGUI : PropertyGUI {}
}
#endif