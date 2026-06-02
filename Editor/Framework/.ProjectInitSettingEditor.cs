using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;

namespace SWeetJelly.Initialiser.Framework.Editor
{
    [CustomEditor(typeof(ProjectInitSetting))]
    public class ProjectInitSettingsEditor : Editor
    {
        private const string MODULES_PROPERTY_NAME = "modules";
        private const string DEFAULT_PROJECT_INIT_SETTINGS_PATH = "Assets/Project Data/Content/Settings/Project Init Settings.asset";

        private SerializedProperty modulesProperty;
        private List<InitModuleContainer> initModulesEditors;

        private GUIContent arrowDownContent;
        private GUIContent arrowUpContent;

        private GUIStyle arrowButtonStyle;

        private ProjectInitSetting projectInitSettings;
        private GenericMenu modulesGenericMenu;

        private static InitModulesHandler modulesHandler;
        private static bool isModuleAdded;

        [MenuItem("SWeetJelly/Editor/Project Init Settings")]
        public static void SelectProjectInitSettings()
        {
            UnityEngine.Object selectedObject = AssetDatabase.LoadAssetAtPath(DEFAULT_PROJECT_INIT_SETTINGS_PATH, typeof(ProjectInitSetting));

            if(selectedObject == null)
            {
                selectedObject = EditorUtils.GetAsset<ProjectInitSetting>();

                if(selectedObject == null)
                {
                    Debug.LogError("Asset with type \"ProjectInitSettings\" don`t exist.");
                }
                else
                {
                    Selection.activeObject = selectedObject;
                    Debug.LogWarning($"Asset with type \"ProjectInitSettings\" is misplaced. Expected path: {DEFAULT_PROJECT_INIT_SETTINGS_PATH} .Actual path: {AssetDatabase.GetAssetPath(selectedObject)}");
                }
            }
            else
            {
                Selection.activeObject = selectedObject;
            }
        }

        protected void OnEnable()
        {
            isModuleAdded = false;

            projectInitSettings = (ProjectInitSetting)target;

            modulesHandler = new InitModulesHandler();

            modulesProperty = serializedObject.FindProperty(MODULES_PROPERTY_NAME);

            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private void Styles()
        {
            arrowDownContent = EditorCustomStyles.FoldoutArrowDown;
            arrowUpContent = EditorCustomStyles.FoldoutArrowUp;

            arrowButtonStyle = new GUIStyle(EditorCustomStyles.Padding00);
        }

        private void InitGenericMenu()
        {
            modulesGenericMenu = new GenericMenu();

            //Load all modules
            InitModule[] initModules = projectInitSettings.Modules;

            IEnumerable<Type> registeredTypes = GetRegisteredAttributes();

            foreach(Type type in registeredTypes)
            {
                RegisterModuleAttribute registerModuleAttribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(type, typeof(RegisterModuleAttribute));

                if(registerModuleAttribute != null)
                {
                    if(!registerModuleAttribute.Core)
                    {
                        bool isAlreadyActive = initModules != null && initModules.Any(x => x != null && x.GetType() == type);

                        if(isAlreadyActive)
                        {
                            modulesGenericMenu.AddDisabledItem(new GUIContent("Add Module/" + registerModuleAttribute.Path), false);
                        }
                        else
                        {
                            modulesGenericMenu.AddItem(new GUIContent("Add Module/" + registerModuleAttribute.Path), false, delegate
                            {
                                AddModule(type);

                                InitGenericMenu();
                            });
                        }
                    }
                }
            }
        }

        private IEnumerable<Type> GetRegisteredAttributes()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach(Assembly assembly in assemblies)
            {
                IEnumerable<Type> types = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(InitModule)));

                foreach(Type type in types)
                {
                    yield return type;
                }
            }
        }

        private void InitModules(InitModule[] modules)
        {
            List<Type> requiredModules = GetRequiredModules(modules);

            if(requiredModules.Count > 0)
            {
                foreach(var type in requiredModules)
                {
                    AddModule(type);
                }

                LoadEditorsList();

                EditorUtility.SetDirty(target);

                AssetDatabase.SaveAssets();
            }
        }

        private List<Type> GetRequiredModules(InitModule[] modules)
        {
            IEnumerable<Type> types = GetRegisteredAttributes();

            List<Type> requiredModules = new List<Type>();

            foreach(Type type in types)
            {
                RegisterModuleAttribute registerModuleAttribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(type, typeof(RegisterModuleAttribute));

                if(registerModuleAttribute == null)
                    continue;

                if(registerModuleAttribute.Core)
                {
                    bool isExists = modules != null && modules.Any(x => x != null && x.GetType() == type);

                    if(!isExists)
                    {
                        requiredModules.Add(type);
                    }
                }
            }

            var sortedModules = requiredModules.OrderBy(t =>
            {
                var attribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(t, typeof(RegisterModuleAttribute));
                return attribute != null ? attribute.Order : int.MaxValue;
            }).ToList();

            return sortedModules;
        }

        public override void OnInspectorGUI()
        {
            if(!isModuleAdded)
            {
                InitModules(projectInitSettings.Modules);
                InitGenericMenu();

                LoadEditorsList();

                Styles();
                isModuleAdded = true;

                return;
            }

            EditorGUILayoutCustom.BeginMenuBoxGroup("Modules", modulesGenericMenu);

            DrawModules(modulesProperty);

            EditorGUILayoutCustom.EndBoxGroup();

            GUILayout.FlexibleSpace();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= LogPlayModeState;
        }

        private void OnDestroy()
        {
            ClearInitModules();
        }

        private void LogPlayModeState(PlayModeStateChange obj)
        {
            if (Selection.activeObject == target)
                Selection.activeObject = null;
        }

        private void LoadEditorsList()
        {
            ClearInitModules();
            SerializedProperty initModule;
            SerializedObject initModuleSerializedObject;

            for(int i = 0; i < modulesProperty.arraySize; i++)
            {
                initModule = modulesProperty.GetArrayElementAtIndex(i);

                if(initModule.objectReferenceValue != null)
                {
                    initModuleSerializedObject = new SerializedObject(initModule.objectReferenceValue);

                    initModulesEditors.Add(new InitModuleContainer(initModule.objectReferenceValue.GetType(), initModuleSerializedObject, Editor.CreateEditor(initModuleSerializedObject.targetObject), modulesHandler.IsCoreModule(initModule.objectReferenceValue.GetType())));
                }
            }
        }

        private InitModuleContainer GetEditor(Type type)
        {
            for(int i = 0; i < initModulesEditors.Count; i++)
            {
                if(initModulesEditors[i].Type == type)
                    return initModulesEditors[i];
            }

            return null;
        }

        private void ClearInitModules()
        {
            if (initModulesEditors != null)
            {
                // Destroy old editors
                for (int i = 0; i < initModulesEditors.Count; i++)
                {
                    if (initModulesEditors[i] != null && initModulesEditors[i].Editor != null)
                    {
                        DestroyImmediate(initModulesEditors[i].Editor);
                    }
                }

                initModulesEditors.Clear();
            }
            else
            {
                initModulesEditors = new List<InitModuleContainer>();
            }
        }

        private void DrawModules(SerializedProperty arrayProperty)
        {
            if(arrayProperty.arraySize > 0)
            {
                for (int i = 0; i < arrayProperty.arraySize; i++)
                {
                    SerializedProperty initModuleProperty = arrayProperty.GetArrayElementAtIndex(i);

                    if (initModuleProperty.objectReferenceValue != null)
                    {
                        InitModule initModule = (InitModule)initModuleProperty.objectReferenceValue;

                        SerializedObject moduleSeializedObject = new SerializedObject(initModuleProperty.objectReferenceValue);

                        moduleSeializedObject.Update();

                        Rect moduleRect;

                        initModuleProperty.isExpanded = EditorGUILayoutCustom.BeginFoldoutBoxGroup(initModuleProperty.isExpanded, initModule.ModuleName, out moduleRect);

                        if (initModuleProperty.isExpanded)
                        {
                            InitModuleContainer moduleContainer = GetEditor(initModuleProperty.objectReferenceValue.GetType());

                            if(moduleContainer == null)
                                continue;
                            
                            moduleContainer.OnInspectorGUI();

                            GUILayout.Space(8);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            moduleContainer.DrawButtons();

                            if (!moduleContainer.IsCore)
                            {
                                if(GUILayout.Button("Remove", GUILayout.Width(128)))
                                {
                                    if (EditorUtility.DisplayDialog("This object will be removed!", "Are you sure?", "Remove", "Cancel"))
                                    {
                                        UnityEngine.Object removedObject = initModuleProperty.objectReferenceValue;
                                        initModuleProperty.isExpanded = false;
                                        arrayProperty.RemoveFromVariableArrayAt(i);

                                        LoadEditorsList();
                                        AssetDatabase.RemoveObjectFromAsset(removedObject);

                                        DestroyImmediate(removedObject, true);

                                        EditorUtility.SetDirty(target);
                                        AssetDatabase.SaveAssets();

                                        return;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayoutCustom.EndFoldoutBoxGroup();

                        if (GUI.Button(new Rect(moduleRect.x + moduleRect.width - 16, moduleRect.y, 8, 8), arrowUpContent, arrowButtonStyle))
                        {
                            if (i > 0)
                            {
                                bool expandState = arrayProperty.GetArrayElementAtIndex(i- 1).isExpanded;

                                arrayProperty.MoveArrayElement(i, i - 1);

                                arrayProperty.GetArrayElementAtIndex(i- 1).isExpanded = initModuleProperty.isExpanded;
                                arrayProperty.GetArrayElementAtIndex(i).isExpanded = expandState;
                                serializedObject.ApplyModifiedProperties();

                                GUIUtility.ExitGUI();
                                Event.current.Use();

                                return;
                            }
                        }
                        if (GUI.Button(new Rect(moduleRect.x + moduleRect.width - 16, moduleRect.y + 8, 8, 8), arrowDownContent, arrowButtonStyle))
                        {
                            if (i + 1 < arrayProperty.arraySize)
                            {
                                bool expandState = arrayProperty.GetArrayElementAtIndex(i+ 1).isExpanded;

                                arrayProperty.MoveArrayElement(i, i + 1);

                                arrayProperty.GetArrayElementAtIndex(i+ 1).isExpanded = initModuleProperty.isExpanded;
                                arrayProperty.GetArrayElementAtIndex(i).isExpanded = expandState;
                                serializedObject.ApplyModifiedProperties();

                                GUIUtility.ExitGUI();
                                Event.current.Use();

                                return;
                            }
                        }

                        if (GUI.Button(moduleRect, GUIContent.none, GUIStyle.none))
                        {
                            initModuleProperty.isExpanded = !initModuleProperty.isExpanded;
                        }

                        moduleSeializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal(EditorCustomStyles.Box);
                        EditorGUILayout.BeginHorizontal(EditorCustomStyles.Padding00);
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.warnicon"), EditorCustomStyles.Padding00, GUILayout.Width(16), GUILayout.Height(16));
                        EditorGUILayout.LabelField("Object reference is null");
                        if (GUILayout.Button("Remove", EditorStyles.miniButton))
                        {
                            arrayProperty.RemoveFromVariableArrayAt(i);

                            InitGenericMenu();

                            GUIUtility.ExitGUI();
                            Event.current.Use();

                            return;
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Modules list is empty!", MessageType.Info);
            }
        }

        public void AddModule(Type moduleType)
        {
            Debug.Log("Add Module : " + moduleType);

            if(!moduleType.IsSubclassOf(typeof(InitModule)))
            {
                Debug.LogError("[Initialiser]: Module type should be subclass of InitModule class!");

                return;
            }

            Undo.RecordObject(target, "Add Module");

            modulesProperty = serializedObject.FindProperty(MODULES_PROPERTY_NAME);

            serializedObject.Update();

            modulesProperty.arraySize++;

            InitModule initModule = (InitModule)ScriptableObject.CreateInstance(moduleType);
            initModule.name = moduleType.ToString();

            AssetDatabase.AddObjectToAsset(initModule, target);

            modulesProperty.GetArrayElementAtIndex(modulesProperty.arraySize - 1).objectReferenceValue = initModule;

            // module 추출
            List<InitModule> modules = new List<InitModule>();
            for(int i=0; i<modulesProperty.arraySize; i++)
            {
                InitModule module = modulesProperty.GetArrayElementAtIndex(i).objectReferenceValue as InitModule;
                if(module != null)
                {
                    modules.Add(module);
                }
            }

            // module ordering
            modules = modules.OrderBy(m =>
            {
                var registerModuleAttribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(m.GetType(), typeof(RegisterModuleAttribute));
                return registerModuleAttribute != null ? registerModuleAttribute.Order : int.MaxValue;
            }).ToList();

            // SerializedProperty 배열 재설정
            for(int i=0;i<modules.Count;i++)
            {
                modulesProperty.GetArrayElementAtIndex(i).objectReferenceValue = modules[i];
            }

            serializedObject.ApplyModifiedProperties();
            LoadEditorsList();

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private class InitModulesHandler
        {
            private IEnumerable<ModuleConfig> moduleConfigs;

            public InitModulesHandler()
            {
                moduleConfigs = GetModuleConfigs();
            }

            private IEnumerable<ModuleConfig> GetModuleConfigs()
            {
                IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(InitModule)));
                
                foreach(Type type in types)
                {
                    RegisterModuleAttribute registerModuleAttribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(type, typeof(RegisterModuleAttribute));

                    if(registerModuleAttribute != null)
                    {
                        yield return new ModuleConfig
                        {
                            classType = type,
                            Attribute = registerModuleAttribute
                        };
                    }
                }
            }

            public bool IsCoreModule(Type type)
            {
                return moduleConfigs.Any(x => x.classType == type && x.Attribute.Core);
            }

            public class ModuleConfig
            {
                public Type classType;
                public RegisterModuleAttribute Attribute;
            }
        }

        private class InitModuleContainer
        {
            public Type Type;
            public SerializedObject SerializedObject;
            public Editor Editor;

            public bool isModuleInitEditor;
            public InitModuleEditor initModuleEditor;

            public bool IsCore;

            public InitModuleContainer(Type type, SerializedObject serializedObject, Editor editor, bool isCore)
            {
                Type = type;
                SerializedObject = serializedObject;
                Editor = editor;
                IsCore = isCore;

                initModuleEditor = editor as InitModuleEditor;
                isModuleInitEditor = initModuleEditor != null;
            }

            public void OnInspectorGUI()
            {
                if (Editor != null)
                {
                    Editor.OnInspectorGUI();
                }
            }

            public void DrawButtons()
            {
                if(!isModuleInitEditor) return;

                initModuleEditor.Buttons();
            }
        }
    }
}

// -----------------
// Initialiser v 0.4.2
// -----------------