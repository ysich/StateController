
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Core.UI.StateController;
using Onemt.Core.UI;
using Unity.VisualScripting;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace Core.UI.StateController.Editor
{
    [CustomEditor(typeof(StateController), true)]
    public class StateControllerEditor:UnityEditor.Editor
    {
        private StateController m_StateController;
        private int m_GrayStateIndex;
        private SerializedProperty m_StatesProperty;
        private SerializedProperty m_StateBasesProperty;
        private SerializedProperty m_GrayStateIndexProperty;
        
        private GUIContent k_StateContent = new GUIContent("选中状态:");
        private GUIContent k_SelectStateContent = new GUIContent("选中");
        private GUIContent k_SelectedStateContent = new GUIContent("已选中");
        private GUIContent k_GrayStateContent = new GUIContent("置灰状态:");
        private GUIContent k_ResetContent = new GUIContent("重置");
        
        private GUIContent[] m_StateControllerContents;
        private GUIContent[] m_StateContents;

        private GUIContent[] m_StateComponentContents;
        
        private string m_AddStateName;
        private int m_RemoveStateIndex;
        
        private ReorderableList m_StatesReorderableList;
        private ReorderableList m_StateComponentsReorderableList;

        private HashSet<Object> m_CheckSet = new HashSet<Object>();
        protected void OnEnable()
        {
            m_StateController = target as StateController;
            m_StatesProperty = serializedObject.FindProperty("states");
            RefreshState();
            //状态
            m_StatesReorderableList = new ReorderableList(serializedObject,m_StatesProperty
                , false, true, true, true)
            {
                elementHeightCallback = ElementHeightCallback,
                onAddCallback = StatesAddItemCallBack,
                onRemoveCallback = StatesRemoveItemCallBack
            };
            m_StatesReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                GUI.Label(rect, "状态列表：");
            };
            m_StatesReorderableList.drawElementCallback = (Rect rect,int index,bool selected,bool focused) =>
            {
                int btnWidth = 55;
                Rect itemRect = new Rect(rect.x,rect.y,rect.width - btnWidth - 5,EditorGUIUtility.singleLineHeight);
                
                //根据index获取对应元素 
                SerializedProperty item = m_StatesReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(itemRect, item,m_StateContents[index],true);
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshState();
                }
                Rect btnRect = new Rect(rect.x + rect.width - btnWidth, rect.y, btnWidth, EditorGUIUtility.singleLineHeight);
                bool isSelected = m_StateController.selectedStateIndex == index;
                GUI.color = isSelected ? Color.red : Color.green;
                GUIContent btnContent = isSelected ? k_SelectedStateContent : k_SelectStateContent;
                if (GUI.Button(btnRect,btnContent))
                {
                    SelectState(index);
                }
                GUI.color = Color.white;
                serializedObject.ApplyModifiedProperties();
            };
            
            //状态控制节点
            m_StateBasesProperty = serializedObject.FindProperty("m_StateBases");
            m_GrayStateIndexProperty = serializedObject.FindProperty("m_GrayStateIndex");
            m_GrayStateIndex = m_GrayStateIndexProperty.intValue;
            RefreshStateBaseList();
            RefreshStateComponent();
            m_StateComponentsReorderableList = new ReorderableList(serializedObject, m_StateBasesProperty
                , false, true, false, true);
            m_StateComponentsReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                GUI.Label(rect, "状态节点列表：");
            };
            m_StateComponentsReorderableList.drawElementCallback = (Rect rect,int index,bool selected,bool focused) =>
            {
                //根据index获取对应元素 
                SerializedProperty item = m_StateBasesProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, item,m_StateComponentContents[index],true);
            };
            m_StateComponentsReorderableList.onRemoveCallback = StateComponentRemoveItemCallBack;
        }

        public override void OnInspectorGUI()
        {
            if (m_StateContents.Length != m_StatesProperty.arraySize)
            {
                RefreshState();
            }
            RefreshStateBaseList();
            if (m_StateComponentContents.Length != m_StateBasesProperty.arraySize)
            {
                RefreshStateComponent();
            }
            
            //状态选择
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(k_StateContent,GUILayout.Width(80));
            int selectedStateIndex = EditorGUILayout.Popup(m_StateController.selectedStateIndex, m_StateContents);
            SelectState(selectedStateIndex);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(k_GrayStateContent,GUILayout.Width(80));
            int grayStateIndex = EditorGUILayout.Popup(m_GrayStateIndex, m_StateContents);
            if(grayStateIndex != m_GrayStateIndex)
            {
                SetGrayStateIndex(grayStateIndex);
            }

            if (GUILayout.Button(k_ResetContent,GUILayout.Width(80)))
            {
                SetGrayStateIndex(-1);
            }
            EditorGUILayout.EndHorizontal();
            m_StatesReorderableList.DoLayoutList();
            m_StateComponentsReorderableList.DoLayoutList();
        }

        private void RefreshState()
        {
            List<GUIContent> guiContents = new List<GUIContent>();
            for (int i = 0; i < m_StatesProperty.arraySize; i++)
            {
                SerializedProperty stateProperty = m_StatesProperty.GetArrayElementAtIndex(i);
                string stateName = stateProperty.stringValue;
                guiContents.Add(new GUIContent(stateName));
            }

            m_StateContents = guiContents.ToArray();
        }  

        private void AddState(string stateName)
        {
            int index = m_StatesProperty.arraySize;
            m_StatesProperty.InsertArrayElementAtIndex(index);
            SerializedProperty stateProperty = m_StatesProperty.GetArrayElementAtIndex(index);
            stateProperty.stringValue = stateName;
            
            //通知状态控制节点去添加默认状态数据
            if (m_StatesProperty.arraySize == 1)
            {
                m_StateController.SelectedIndex(0);
            }

            serializedObject.ApplyModifiedProperties();
            RefreshState();
        }

        private void RemoveState(int index)
        {
            if (index > m_StatesProperty.arraySize - 1)
            {
                return;
            }
            
            //通知状态控制节点去删除状态数据
            m_StatesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            m_StateController.SelectedIndex(0);
            RefreshState();
        }

        private void SetGrayStateIndex(int index)
        {
            m_GrayStateIndex = index;
            m_GrayStateIndexProperty.intValue = m_GrayStateIndex;
            serializedObject.ApplyModifiedProperties();
            //设置了置灰状态后需要去设置Image的Gray忽略
            bool isIgnoreGray = index != -1;
            for (int i = 0; i < m_StateBasesProperty.arraySize; i++)
            {
                SerializedProperty stateProperty = m_StateBasesProperty.GetArrayElementAtIndex(i);
                StateBase stateBase = stateProperty.objectReferenceValue as StateBase;
                if (stateBase is StateImageColor|| stateBase is StateImageSprite)
                {
                    ImageEx imageEx = stateBase.GetComponent<ImageEx>();
                    SerializedObject imageSerializedObject = new SerializedObject(imageEx);
                    SerializedProperty ignoreProperty = imageSerializedObject.FindProperty("m_IgnoreGray");
                    ignoreProperty.boolValue = isIgnoreGray;
                    imageSerializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void SelectState(int index)
        {
            if (index != m_StateController.selectedStateIndex)
            {
                m_StateController.selectedStateIndex = index;
                //editor下需要手动触发
                for (int i = 0; i < m_StateBasesProperty.arraySize; i++)
                {
                    SerializedProperty stateBaseProperty = m_StateBasesProperty.GetArrayElementAtIndex(i);
                    if (stateBaseProperty.objectReferenceValue is StateBase stateBase)
                    {
                        stateBase.OnRefresh(index);
                    }
                }
                EditorUtility.SetDirty(target);
            }
        }

        // 计算每个Element的高度
        private float ElementHeightCallback(int index)
        {
            // 获取对应的Element
            SerializedProperty element = m_StatesReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element) + EditorGUIUtility.standardVerticalSpacing;
        }

        private void RefreshStateComponent()
        {
            List<GUIContent> guiContents = new List<GUIContent>();
            for (int i = 0; i < m_StateBasesProperty.arraySize; i++)
            {
                SerializedProperty stateBaseProperty = m_StateBasesProperty.GetArrayElementAtIndex(i);
                StateBase stateBase = stateBaseProperty.objectReferenceValue as StateBase;
                if (stateBase != null)
                {
                    guiContents.Add(new GUIContent(stateBase.name));
                }
                else
                {
                    guiContents.Add(new GUIContent(string.Empty));
                }
                
            }

            m_StateComponentContents = guiContents.ToArray();
        }
        
        private void StatesAddItemCallBack(ReorderableList list)
        {
            AddState("defaultState");
            ReorderableList.defaultBehaviours.DoAddButton(list);
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        private void StatesRemoveItemCallBack(ReorderableList list)
        {
            SerializedProperty stateProperty = m_StatesProperty.GetArrayElementAtIndex(list.index);
            string stateName = stateProperty.stringValue;
            if (!EditorUtility.DisplayDialog("提示", $"确定删除 状态:{stateName} 嘛？", "确认","取消"))
            {
                return;
            }
            RemoveState(list.index);
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshStateBaseList()
        {
            m_CheckSet.Clear();
            for (int i = m_StateBasesProperty.arraySize-1; i >= 0; i--)
            {
                SerializedProperty stateBaseProperty = m_StateBasesProperty.GetArrayElementAtIndex(i);
                Object stateBase = stateBaseProperty.objectReferenceValue;
                if (stateBase == null)
                {
                    m_StateBasesProperty.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    RefreshStateComponent();
                    continue;
                }

                if (m_CheckSet.Contains(stateBase))
                {
                    m_StateBasesProperty.DeleteArrayElementAtIndex(i);
                    Debug.Log($"{m_StateController.name}删除了重复的状态节点{stateBase.name}");
                }
                else
                {
                    m_CheckSet.Add(stateBase);
                }
            }
        }
        
        private void StateComponentRemoveItemCallBack(ReorderableList list)
        {
            SerializedProperty stateBaseProperty = m_StateBasesProperty.GetArrayElementAtIndex(list.index);
            StateBase stateBase = stateBaseProperty.objectReferenceValue as StateBase;
            if (!EditorUtility.DisplayDialog("提示", $"确定删除 节点:{stateBase.name} 嘛？", "确认","取消"))
            {
                return;
            }
            if (stateBase != null)
            {
                stateBase.stateController = null;
            }
            m_StateBasesProperty.DeleteArrayElementAtIndex(list.index);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}