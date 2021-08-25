using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
* Main Editor(s): Ryan Lewin, Fadwa Hasanin, Martin Jones, John Montgomery.
* Date Created: 22/02/21
* Date Modified: 10/05/2021
*
* Access the value in this property using YourVariableName.m_property.
* Add an event listener to the variable using YourVariableName.m_event.AddListener(YourMethod);
* This will call YourMethod when the variable YourVariableName is edited.
* Remove once done with using YourVariableName.m_event.RemoveListener(YourMethod).
*/

namespace PropertyListenerTool
{
    /// <summary>
    /// A generic property which can be used to manage properties cleanly elsewhere.
    /// Add a new class at the bottom of the script to match the intended type to be affected.
    /// </summary>
    [Serializable]
    public class Property<T>
    {
        /// <summary> Listener for when m_property's value is changed. </summary>
        public EventProperty m_event; //Testing editor stuffs

        [SerializeField] protected T m_variable; // The variable of the property.
        /// <summary> Used for accessing the value in this property, if value is changed will call any listeners added to m_event. </summary>
        public virtual T m_property // The property itself.
        {
            get => m_variable;// Returns the variable component.
            set // Set the value of the component and trigger the event component.
            {
                if (!value.Equals(m_variable))
                {
                    m_variable = value;
                    if (m_event != null)
                        m_event.Invoke();
                }
            }
        }
    }

    /// <Summary>
    /// Used for adding a listener to the property class
    /// </Summary>
    [Serializable]
    public class EventProperty
    {
        [SerializeField] protected UnityEvent m_variable; // The variable of the property.
        [SerializeField] private List<string> m_listenerPaths = new List<string>() { };
        private List<object> m_listeningObjects = new List<object>();

        /// <summary>
        /// <para> Add a listener to the event, this action will be called whenever YourVariableName.m_property is changed. </para>
        /// <para> e.g.<!--  -->
        /// YourVariableName.m_event.AddListener(YourMethod); </para>
        /// <para> Once Finished with, make sure to remove with: <seealso cref="EventProperty.RemoveListener(UnityAction)"/></para>
        /// </summary>
        public void AddListener(UnityAction action)
        {
            //Adds the listeners path name to a list of strings for the property drawer and adds the listener to the Action (m_variable)
            //Format of eventName is "[ObjectName][Script] - [Method]"
            string eventName = $"{action.Target.ToString()}{action.Method.Name}".Replace("(", " - ").Replace(")", ".");

            //if the list already contains the action, then remove it to reset the call.
            RemoveListener(action);

            //Add the action as a listener and add the path and the target to lists to keep track of what is being called.
            m_variable.AddListener(action);
            m_listenerPaths.Add(eventName);
            m_listeningObjects.Add(action.Target);
        }

        public void RemoveListener(UnityAction action)
        {
            //Removes the given action from the lists and removes the listener from the Action (m_variable)
            string eventName = $"{action.Target.ToString()}{action.Method.Name}".Replace("(", " - ").Replace(")", ".");
            for (int i = 0; i < m_listeningObjects.Count; i++)
            {
                if (m_listeningObjects[i] == action.Target &&
                        m_listenerPaths.Contains(eventName))
                {
                    m_variable.RemoveListener(action);
                    m_listenerPaths.RemoveAt(i);
                    m_listeningObjects.RemoveAt(i);
                    break;
                }
            }
        }

        public void RemoveAllListeners()
        {
            m_listenerPaths.Clear();
            m_listeningObjects.Clear();
            m_variable.RemoveAllListeners();
        }

        public void Invoke() => m_variable.Invoke();
    }

    /// <summary>
    /// <br> Access the value in this property using YourVariableName.m_property. </br>
    /// <br> Add an event listener to the variable using YourVariableName.m_event.AddListener(YourMethod);
    /// This will call YourMethod when the variable YourVariableName is edited. </br>
    /// </summary>
    [Serializable] // A Property for Integer types, inherits from the Property Script.
    public class IntProperty : Property<int> { }

    /// <summary>
    /// <br> Access the value in this property using YourVariableName.m_property. </br>
    /// <br> Add an event listener to the variable using YourVariableName.m_event.AddListener(YourMethod);
    /// This will call YourMethod when the variable YourVariableName is edited. </br>
    /// </summary>
    [Serializable] // A Property for Float types, inherits from the Property Script.
    public class FloatProperty : Property<float> { }

    /// <summary>
    /// <br> Access the value in this property using YourVariableName.m_property. </br>
    /// <br> Add an event listener to the variable using YourVariableName.m_event.AddListener(YourMethod);
    /// This will call YourMethod when the variable YourVariableName is edited. </br>
    /// </summary>
    [Serializable] // A Property for Bool types, inherits from the Property Script.
    public class BoolProperty : Property<bool> { }
}