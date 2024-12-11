
using System;
using System.Collections.Generic;
using Onemt.Core.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Gradient = Onemt.Core.UI.Gradient;

namespace Core.UI.StateController.Editor
{
    public abstract class SelectableStateBaseEditor<T> : UnityEditor.Editor
    {
        private GUIContent k_StateContent = new GUIContent("选中状态:");
        private GUIContent k_CurStateDataContent = new GUIContent("当前状态数据:");
        private GUIContent k_SelectStateContent = new GUIContent("选中");
        private GUIContent k_SelectedStateContent = new GUIContent("已选中");
        private GUIContent k_NonStateController = new GUIContent("！需要先在状态控制器内增加该状态节点");
        private GUIContent[] m_StateContents;

        private SerializedProperty m_StateControllerProperty;
        private SerializedProperty m_StateDataListProperty;
        private ReorderableList m_StateDataReorderableList;

        private SelectableStateBase<T> m_SelectableStateBase;
        private StateController m_StateController;
        private SerializedProperty m_StatesProperty;

        private void OnEnable()
        {
            InitData();
            //找一遍，如果控制器列表没有自己就添加进去，因为可能是复制粘贴出来的。
            AddSelectableStateToController();
        }

        private void InitData()
        {
            m_SelectableStateBase = target as SelectableStateBase<T>;
            m_StateDataListProperty = serializedObject.FindProperty("m_StateDataList");
            m_StateControllerProperty = serializedObject.FindProperty("stateController");
            if (m_SelectableStateBase.stateController == null)
            {
                return;
            }

            m_StateController = m_SelectableStateBase.stateController;
            SerializedObject stateControllerObject = new SerializedObject(m_StateController);

            m_StatesProperty = stateControllerObject.FindProperty("states");
            RefreshState();

            m_StateDataReorderableList = new ReorderableList(serializedObject, m_StateDataListProperty
                , false, true, false, false) { elementHeightCallback = ElementHeightCallback };
            m_StateDataReorderableList.drawHeaderCallback = (Rect rect) => { GUI.Label(rect, "状态数据列表："); };
            m_StateDataReorderableList.drawElementCallback = (Rect rect, int index, bool selected, bool focused) =>
            {
                int btnWidth = 55;
                Rect itemRect = new Rect(rect.x, rect.y, rect.width - btnWidth - 5, EditorGUIUtility.singleLineHeight);

                //根据index获取对应元素 
                SerializedProperty item = m_StateDataReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(itemRect, item, m_StateContents[index], true);
                if (EditorGUI.EndChangeCheck())
                {
                    if (index == m_StateController.selectedStateIndex)
                    {
                        m_SelectableStateBase.OnRefresh(index);
                    }
                }

                Rect btnRect = new Rect(rect.x + rect.width - btnWidth, rect.y, btnWidth,
                    EditorGUIUtility.singleLineHeight);
                bool isSelected = m_StateController.selectedStateIndex == index;
                GUI.color = isSelected ? Color.red : Color.green;
                GUIContent btnContent = isSelected ? k_SelectedStateContent : k_SelectStateContent;
                if (GUI.Button(btnRect, btnContent))
                {
                    SelectState(index);
                }

                GUI.color = Color.white;

                serializedObject.ApplyModifiedProperties();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            StateController oldStateController = m_StateController;
            EditorGUILayout.ObjectField(m_StateControllerProperty);
            if (serializedObject.ApplyModifiedProperties())
            {
                Debug.Log("StateController变更");
                if (m_StateControllerProperty.objectReferenceValue == null)
                {
                    if (EditorUtility.DisplayDialog("提示", $"确定删除 状态控制器:{m_StateController.name} 嘛？\n删除后当前的状态数据都会丢失！",
                            "确认", "取消"))
                    {
                        RemoveSelectableStateToController(oldStateController);
                        m_StateController = null;
                        Debug.Log("StateController置为空");
                    }
                    else
                    {
                        m_StateControllerProperty.objectReferenceValue = oldStateController;
                        m_StateControllerProperty.serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                    }
                    return;
                }
                //给None赋值
                if (oldStateController == null)
                {
                    InitData();
                    AddSelectableStateToController();
                }
                else if (oldStateController != m_StateControllerProperty.objectReferenceValue)
                {
                    if (EditorUtility.DisplayDialog("提示", $"确定修改 状态控制器:{oldStateController.name} 嘛？\n修改后当前的状态数据都会丢失！",
                            "确认", "取消"))
                    {
                        InitData();
                        //删除旧的
                        RemoveSelectableStateToController(oldStateController);

                        //添加新的
                        AddSelectableStateToController();
                    }
                    else
                    {
                        m_StateControllerProperty.objectReferenceValue = oldStateController;
                        m_StateControllerProperty.serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                    }
                }

                serializedObject.Update();
            }

            if (m_SelectableStateBase.stateController == null)
            {
                EditorGUILayout.LabelField(k_NonStateController);
                m_StateDataListProperty.ClearArray();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (m_StateDataListProperty.arraySize != m_StatesProperty.arraySize)
            {
                RefreshStateData();
            }

            //状态选择
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(k_StateContent, GUILayout.Width(80));
            int stateIndex = m_StateController != null ? m_StateController.selectedStateIndex : 0;
            int selectedStateIndex = EditorGUILayout.Popup(stateIndex, m_StateContents);
            SelectState(selectedStateIndex);
            EditorGUILayout.EndHorizontal();

            if (m_StateDataListProperty.arraySize == 0)
            {
                return;
            }

            if (stateIndex > m_StateDataListProperty.arraySize - 1)
            {
                return;
            }

            SerializedProperty stateDataProperty = m_StateDataListProperty.GetArrayElementAtIndex(stateIndex);
            EditorGUILayout.PropertyField(stateDataProperty, k_CurStateDataContent);
            m_StateDataReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void AddSelectableStateToController()
        {
            if (m_StateControllerProperty.objectReferenceValue != null)
            {
                SerializedObject newSerializedObject = new SerializedObject(m_StateControllerProperty.objectReferenceValue);
                SerializedProperty newStateBaseListProperty =
                    newSerializedObject.FindProperty("m_StateBases");
                //查重如果存在就不添加了
                bool hasSelfState = false;
                for (int i = 0; i < newStateBaseListProperty.arraySize; i++)
                {
                    SerializedProperty stateBase = newStateBaseListProperty.GetArrayElementAtIndex(i);
                    if (m_SelectableStateBase.Equals(stateBase.objectReferenceValue))
                    {
                        hasSelfState = true;
                        // Debug.Log("当前节点已存在于StateController中");
                        break;
                    }
                }

                if (!hasSelfState)
                {
                    newStateBaseListProperty.InsertArrayElementAtIndex(newStateBaseListProperty.arraySize);
                    SerializedProperty stateBaseProperty =
                        newStateBaseListProperty.GetArrayElementAtIndex(newStateBaseListProperty.arraySize -
                                                                        1);
                    stateBaseProperty.objectReferenceValue = m_SelectableStateBase;
                    newSerializedObject.ApplyModifiedProperties();

                    SerializedProperty grayStateIndexProperty = newSerializedObject.FindProperty("m_GrayStateIndex");
                    bool isIgnoreGray = grayStateIndexProperty.intValue != -1;
                    UpdateImageExIgnoreGray(isIgnoreGray);
                    
                    Debug.Log($"添加了新状态器{m_StateControllerProperty.objectReferenceValue.name}的状态节点");
                }
            }
        }

        private void RemoveSelectableStateToController(StateController oldStateController)
        {
            //删除旧的
            if (oldStateController != null)
            {
                SerializedObject oldSerializedObject = new SerializedObject(oldStateController);
                SerializedProperty oldStateBaseListProperty =
                    oldSerializedObject.FindProperty("m_StateBases");
                for (int i = 0; i < oldStateBaseListProperty.arraySize; i++)
                {
                    SerializedProperty oldStateBaseProperty =
                        oldStateBaseListProperty.GetArrayElementAtIndex(i);
                    if (m_SelectableStateBase.Equals(oldStateBaseProperty.objectReferenceValue))
                    {
                        oldStateBaseListProperty.DeleteArrayElementAtIndex(i);
                        oldSerializedObject.ApplyModifiedProperties();
                        Debug.Log($"删除了旧状态器{oldStateController.name}的状态节点");
                        break;
                    }
                }
            }

            UpdateImageExIgnoreGray(false);
        }

        private void UpdateImageExIgnoreGray(bool isIgnoreGray)
        {
            if (m_SelectableStateBase is StateImageColor|| m_SelectableStateBase is StateImageSprite)
            {
                ImageEx imageEx = m_SelectableStateBase.GetComponent<ImageEx>();
                SerializedObject imageSerializedObject = new SerializedObject(imageEx);
                SerializedProperty ignoreProperty = imageSerializedObject.FindProperty("m_IgnoreGray");
                ignoreProperty.boolValue = isIgnoreGray;
                imageSerializedObject.ApplyModifiedProperties();
                Debug.Log($"刷新ImageEx‘s IgnoreGray is {isIgnoreGray}");
            }
        }

        private void RefreshStateData()
        {
            int stateCount = m_StatesProperty.arraySize;
            int stateDataCount = m_StateDataListProperty.arraySize;
            if (stateCount > stateDataCount)
            {
                for (int i = stateDataCount; i < stateCount; i++)
                {
                    m_StateDataListProperty.InsertArrayElementAtIndex(i);
                    SerializedProperty stateDataProperty = m_StateDataListProperty.GetArrayElementAtIndex(i);
                    SetComponentOfDefaultStateData(stateDataProperty);
                }

                serializedObject.ApplyModifiedProperties();
            }

            if (stateCount < stateDataCount)
            {
                for (int i = stateDataCount; i >= stateCount; i--)
                {
                    m_StateDataListProperty.DeleteArrayElementAtIndex(m_StateDataListProperty.arraySize - 1);
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        private void RefreshState()
        {
            List<GUIContent> guiContents = new List<GUIContent>();
            if (m_StateController != null && m_StatesProperty != null)
            {
                for (int i = 0; i < m_StatesProperty.arraySize; i++)
                {
                    SerializedProperty state = m_StatesProperty.GetArrayElementAtIndex(i);
                    string stateName = state.stringValue;
                    guiContents.Add(new GUIContent(stateName));
                }
            }

            m_StateContents = guiContents.ToArray();
        }

        private void SelectState(int index)
        {
            if (index != m_StateController.selectedStateIndex)
            {
                m_StateController.selectedStateIndex = index;
                //editor下需要手动触发
                SerializedObject stateControllerObject = new SerializedObject(m_StateController);
                SerializedProperty stateBasesProperty = stateControllerObject.FindProperty("m_StateBases");
                for (int i = 0; i < stateBasesProperty.arraySize; i++)
                {
                    SerializedProperty stateBaseProperty = stateBasesProperty.GetArrayElementAtIndex(i);
                    if (stateBaseProperty.objectReferenceValue is StateBase stateBase)
                    {
                        stateBase.OnRefresh(index);
                    }
                }

                serializedObject.Update();
                EditorUtility.SetDirty(target);
            }
        }

        // 计算每个Element的高度
        private float ElementHeightCallback(int index)
        {
            // 获取对应的Element
            SerializedProperty element = m_StateDataReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element) + EditorGUIUtility.standardVerticalSpacing;
        }

        protected abstract T GetDefaultStateData();
        protected abstract void SetComponentOfDefaultStateData(SerializedProperty serializedProperty);
    }

    [CustomEditor(typeof(StateImageSprite))]
    public class SelectableStateImageSprite : SelectableStateBaseEditor<Sprite>
    {
        protected override Sprite GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            return stateBase.gameObject.GetComponent<ImageEx>().sprite;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.objectReferenceValue = GetDefaultStateData();
        }
    }

    [CustomEditor(typeof(StateImageColor))]
    public class SelectableStateImageColor : SelectableStateBaseEditor<Color>
    {
        protected override Color GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            return stateBase.gameObject.GetComponent<ImageEx>().color;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.colorValue = GetDefaultStateData();
        }
    }

    [CustomEditor(typeof(StateTextColor))]
    public class SelectableStateTextColor : SelectableStateBaseEditor<Color>
    {
        protected override Color GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            return stateBase.gameObject.GetComponent<TextEx>().color;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.colorValue = GetDefaultStateData();
        }
    }

    [CustomEditor(typeof(StateImageMirror))]
    public class SelectableStateImageMirror : SelectableStateBaseEditor<ImageTPBase.MirrorType>
    {
        protected override ImageTPBase.MirrorType GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            return stateBase.gameObject.GetComponent<ImageEx>().mirrorType;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.enumValueIndex = (int)GetDefaultStateData();
        }
    }

    [CustomEditor(typeof(StateTextOutline))]
    public class SelectableStateTextOutline : SelectableStateBaseEditor<Material>
    {
        protected override Material GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            return stateBase.gameObject.GetComponent<TextEx>().material;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.objectReferenceValue = GetDefaultStateData();
        }
    }
    
    [CustomEditor(typeof(StateGradient))]
    public class SelectableStateGradient : SelectableStateBaseEditor<StateGradientParam>
    {
        protected override StateGradientParam GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            Gradient gradient = stateBase.GetComponent<Gradient>();
            StateGradientParam stateGradientParam = new StateGradientParam(gradient.enabled,gradient.topColor,gradient.bottomColor);
            return stateGradientParam;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            // StateGradientParam stateGradientParam = GetDefaultStateData();
       
        }
    }
    
    [CustomEditor(typeof(StateRecTransform))]
    public class SelectableStateRecTransform : SelectableStateBaseEditor<Rect>
    {
        protected override Rect GetDefaultStateData()
        {
            StateBase stateBase = target as StateBase;
            RectTransform rectTransform = stateBase.GetComponent<RectTransform>();
            Vector2 pos = rectTransform.anchoredPosition;
            Rect rect = new Rect(pos, rectTransform.rect.size);
            return rect;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.rectValue = GetDefaultStateData();
        }
    }
    
    [CustomEditor(typeof(StateActive))]
    public class SelectableStateActive : SelectableStateBaseEditor<bool>
    {
        protected override bool GetDefaultStateData()
        {
            return true;
        }

        protected override void SetComponentOfDefaultStateData(SerializedProperty serializedProperty)
        {
            serializedProperty.boolValue = GetDefaultStateData();
        }
    }
}