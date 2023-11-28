using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Object = UnityEngine.Object;

public class TA_AssetImporterGUI : TA_EditorUtility
{
    #region Const Variable
    public const string _windowTitle = "Asset Importer";
    public const string _lastUpdate = "27/11/2023";
    public const string _documentationLink = "https://urniquestudio.atlassian.net/wiki/spaces/TIMESLICE/pages/102793223/Timeslice+Asset+Importer";
    public string _warningFbxSentence = "Please select only FBX file in project window";

    private const float _elementnWidth = 300;
    private const float _buttontHeight = 30;
    #endregion

    public enum AssetImportFeature
    {
        FolderCreator, ST_EnvironmentAsset, SK_CharacterAsset, SK_AnimationAsset
    }

    public enum FolderCreationFeature
    {
        ST_EnvironmentAsset, SK_CharacterAsset
    }

    #region Variable 
    // Data for working in a scene
    private AssetImportFeature _userFeature;
    private FolderCreationFeature _userFolderCreationFeature;
    private string _inputFolderName;
   
    private GameObject _m_Selected;
    private ModelImporter _m_ModelImporter;
    private List<ModelImporter> _m_ModelImporterList = new List<ModelImporter>();
    private List<Object> _selectedObjects = new List<Object>();

    #endregion

    #region Unity GUI Method
    [MenuItem(_techArtToolsLable + _windowTitle)]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow(typeof(TA_AssetImporterGUI));
        window.minSize = new Vector2(320, 700);
        window.maxSize = new Vector2(320, 700);
    }

    private void OnEnable()
    {
        titleContent = new GUIContent(_windowTitle);

        this._myEditor = UnityEditor.Editor.CreateEditor(this);
    }

    private void OnDisable()
    {
        this._myEditor.SafeDestroy();
    }

    private void OnGUI()
    {
        this._scrollPos = EditorGUILayout.BeginScrollView(this._scrollPos);

        Draw_Header(_lastUpdate);
        Draw_FeatureList();
        Draw_ImportAsset(this._userFeature);

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region Draw Method
    public override void Draw_Header(string lastUpdate)
    {
        GUILayout.Label("\tIn-House: Asset Importer Tool", EditorStyles.whiteLargeLabel);
        GUILayout.Label("\t\tLast update: " + lastUpdate);
        GUILayout.Label("--------------------------------------------------");
        Draw_Documentation(_documentationLink, _elementnWidth, _buttontHeight);
        GUILayout.Label("--------------------------------------------------");
    }

    private void Draw_FeatureList()
    {
        GUILayout.Label("Section 1: Feature selection", EditorStyles.whiteLargeLabel);
        this._userFeature = (AssetImportFeature)EditorGUILayout.EnumPopup(" Feature:", this._userFeature, GUILayout.Width(_elementnWidth));
        GUILayout.Label("--------------------------------------------------");
    }

    private void Draw_ImportAsset(AssetImportFeature userFeature)
    {
        switch (userFeature)
        {
            case AssetImportFeature.FolderCreator:
                Draw_FolderCreator();
                break;
            case AssetImportFeature.ST_EnvironmentAsset:
                Draw_ImportFeatureEnvironmentStaticMesh();
                break;
            case AssetImportFeature.SK_CharacterAsset:
                Draw_ImportFeatureCharacterSkinnedMesh();
                break;
            case AssetImportFeature.SK_AnimationAsset:
                Draw_ImportFeatureCharacterAnimation();
                break;
        }
    }
    #endregion

    private void Draw_FolderCreator()
    {
        GUILayout.Label("Section 2: Choose Folder Type", EditorStyles.whiteLargeLabel);
        this._userFolderCreationFeature = (FolderCreationFeature)EditorGUILayout.EnumPopup(" Folder Type:", this._userFolderCreationFeature, GUILayout.Width(_elementnWidth));
        GUILayout.Space(5);
        this._inputFolderName = EditorGUILayout.TextField("Folder Name: ", this._inputFolderName, GUILayout.Width(_elementnWidth));

        GUILayout.Label("--------------------------------------------------");
        GUILayout.Space(10);

        if (GUILayout.Button("Generate folder", GUILayout.Width(_elementnWidth), GUILayout.Height(_buttontHeight)))
        {
            switch (this._userFolderCreationFeature)
            {
                case FolderCreationFeature.ST_EnvironmentAsset:
                    TA_AssetImporterUtility.GenerateEnvironmentFolder(this._inputFolderName);
                    break;
                case FolderCreationFeature.SK_CharacterAsset:
                    TA_AssetImporterUtility.GenerateCharacterFolder(this._inputFolderName);
                    break;
            }
        }
    }
    private void Draw_ImportFeatureEnvironmentStaticMesh()
    {
        bool isNotSelectFbxFile = !this._m_Selected;

        GUILayout.Label("Section 2: Files setup", EditorStyles.whiteLargeLabel);

        if (isNotSelectFbxFile)
        {
            EditorGUILayout.HelpBox(this._warningFbxSentence, MessageType.Info);
            return;
        }

        if (GUILayout.Button("Generate and setup static mesh file", GUILayout.Width(_elementnWidth), GUILayout.Height(_buttontHeight)))
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            AddFbxFilesToList();
            TA_AssetImporterUtility.GenerateAndSetupEnvironmentStaticMeshFiles(this._m_ModelImporterList);

            stopwatch.Stop();
            this._m_ModelImporterList.Clear();
            Debug.Log("Generate and setup environment static mesh files done within: " + (stopwatch.Elapsed.TotalSeconds) + " seconds.");
            stopwatch.Reset();
            
        }
    }
    private void Draw_ImportFeatureCharacterSkinnedMesh()
    {
        bool isNotSelectFbxFile = !this._m_Selected;

        GUILayout.Label("Section 2: Files setup", EditorStyles.whiteLargeLabel);

        if (isNotSelectFbxFile)
        {
            EditorGUILayout.HelpBox(this._warningFbxSentence, MessageType.Info);
        }

        else
        {
            if (GUILayout.Button("Generate and setup skinned mesh file", GUILayout.Width(_elementnWidth), GUILayout.Height(_buttontHeight)))
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                AddFbxFilesToList();
                TA_AssetImporterUtility.GenerateAndSetupSkinnedMeshFiles(this._m_ModelImporterList);

                stopwatch.Stop();
                this._m_ModelImporterList.Clear();
                Debug.Log("Generate and setup skinned mesh files done within: " + (stopwatch.Elapsed.TotalSeconds) + " seconds.");
                stopwatch.Reset();
            }
        }

        GUILayout.Label("--------------------------------------------------");

        GUILayout.Label("Section 3: Avatar source and Avatar mask clone", EditorStyles.whiteLargeLabel);

        if (isNotSelectFbxFile)
        {
            EditorGUILayout.HelpBox(this._warningFbxSentence, MessageType.Info);
        }

        else
        {
            if (GUILayout.Button("Clone avatar source and mask data", GUILayout.Width(_elementnWidth), GUILayout.Height(_buttontHeight)))
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                AddFbxFilesToList();
                TA_AssetImporterUtility.CloneAvatarSourceAndAvatarMaskData(this._m_ModelImporterList);

                stopwatch.Stop();
                this._m_ModelImporterList.Clear();
                Debug.Log("Clone avatar source and mask data done within: " + (stopwatch.Elapsed.TotalSeconds) + " seconds.");
                stopwatch.Reset();
            }
        }      
    }
    private void Draw_ImportFeatureCharacterAnimation()
    {
        bool isNotSelectFbxFile = !this._m_Selected;

        if (isNotSelectFbxFile)
        {
            EditorGUILayout.HelpBox(this._warningFbxSentence, MessageType.Info);
            return;
        }

        GUILayout.Label("Section 2: Files setup", EditorStyles.whiteLargeLabel);

        if (GUILayout.Button("Generate and setup animation file", GUILayout.Width(_elementnWidth), GUILayout.Height(_buttontHeight)))
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            AddFbxFilesToList();
            TA_AssetImporterUtility.GenerateAndSetupAnimationFiles(this._m_ModelImporterList);

            stopwatch.Stop();
            this._m_ModelImporterList.Clear();
            Debug.Log("Generate and setup animation files done within: " + (stopwatch.Elapsed.TotalSeconds) + " seconds.");
            stopwatch.Reset();
        }       
    }

    #region Selection Method
    private void OnSelectionChange()
    {
        GetMultiSelectionModelImporter();
        Repaint();
    }

    private void GetMultiSelectionModelImporter()
    {
        GameObject selected = Selection.activeGameObject;
        this._selectedObjects.Clear();

        foreach (var item in Selection.objects)
        {
            bool userAlreadySelectedFiles = item != null && !this._selectedObjects.Contains(item);
            if (userAlreadySelectedFiles)
            {
                bool isModel = PrefabUtility.GetPrefabAssetType(item) == PrefabAssetType.Model;

                if (!isModel)
                {
                    this._m_Selected = null;
                }

                else
                {
                    this._m_Selected = selected;
                    this._selectedObjects.Add(item);
                }

            }
        }
    }
    #endregion


    #region ListManagementMethod

    private void AddFbxFilesToList()
    {
        foreach (var item in _selectedObjects)
        {
            ModelImporter modelImporterObj = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item)) as ModelImporter;
            bool isFbx = modelImporterObj != null;
       
            if (isFbx)
            {
                this._m_ModelImporterList.Add(modelImporterObj);
            }
        }
    }

    #endregion
}
