using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.Animations;

public static class TA_AssetImporterUtility
{
    #region File Prefix
    private const string PREFIX_FBX = "FBX_";
    private const string PREFIX_SKINNEDMESH = "SK_";
    private const string PREFIX_STATICMESH = "ST_";
    private const string PREFIX_ANIMAVATARSOURCE = "AS_SK_";
    private const string PREFIX_ANIMAVATARMASK = "AM_SK_";
    private const string PREFIX_MATERIAL = "M_";
    #endregion

    #region File Suffix
    private const string SUFFIX_ASSET = ".asset";
    private const string SUFFIX_PREFAB = ".prefab";
    private const string SUFFIX_MATERIAL = ".mat";
    private const string SUFFIX_FBX = ".fbx";
    private const string SUFFIX_ANIM = ".anim";
    private const string SUFFIX_AVATARMASK = ".mask";
    private const string SUFFIX_PNG = ".png";
    private const string TEXSUFFIX_DIFFUSE = "_DIF";
    private const string TEXSUFFIX_EMISSION = "_EMI";
    private const string TEXSUFFIX_MASK = "_MSK";
    #endregion

    #region Folder
    private const string FOLDER_RELATIVEASSETS = "Assets";
    private const string FOLDER_PLACEHOLDERASSETS = "PlaceHolderAssets";
    private const string FOLDER_MODELS = "Models";
    private const string FOLDER_MATERIALS = "Materials";
    private const string FOLDER_PREFABS = "Prefabs";
    private const string FOLDER_TEXTURES = "Textures";

    private const string FOLDER_ANIMATION = "Animation";
    private const string FOLDER_ANIMCLIPS = "AnimClips";
    private const string FOLDER_ANIMCONTROLLER = "AnimController";
    private const string FOLDER_ANIMFBX = "AnimFBX";
    private const string FOLDER_AVATAR = "Avatar";
    #endregion

    #region Shader
    private const string SHADER_ENVIRONMENT = "Universal Render Pipeline/Lit";
    private const string SHADER_CHARACTER = "Universal Render Pipeline/Lit";
    #endregion

    #region Shader property
    private const string SHADERPROPERTY_DIFFUSE = "_BaseMap";
    private const string SHADERPROPERTY_EMISSION = "_EmissionMap";
    private const string SHADERPROPERTY_MASK = "_DetailMask";
    #endregion

    private const string PLACEHOLDER_TEXTURE = "T_Placeholder_DIF";
    //TODO Change to row below after Max has setup everythings
    private const string ROOTMOTIONLOCATOR = "SK_Alain_Armature/ROOT/ROOT_Motion_Locator";
    //private const string ROOTMOTIONLOCATOR = "SK_ROOT/S_Armature/ROOT/ROOT_Motion_Locator"; 

    #region Environment Static Mesh
    public static void GenerateAndSetupEnvironmentStaticMeshFiles(List<ModelImporter> fbx)
    {
        int loopCount = fbx.Count;

        for (int i = 0; i < loopCount; i++)
        {
            ModelImporter originalAnimationFbxFile = fbx[i];

            string fbxAssetPath = originalAnimationFbxFile.assetPath;
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            
            string prefabName = fbxAsset.name.Substring(4, fbxAsset.name.Length - 4);
            string prefabFolderPath = GetEnvironmentStaticMeshPrefabFolderPath(originalAnimationFbxFile) + "/";

            GameObject prefab = Object.Instantiate(fbxAsset);
            MeshRenderer prefabMeshRenderer = prefab.GetComponent<MeshRenderer>();

            #region Create and load material
            List<Material> prefabMaterial = new List<Material>();
            prefabMaterial.Add(GetEnvironmentStaticMeshMaterial(originalAnimationFbxFile));
            #endregion

            #region Load all texture in this atlas
            List<string> listOfTexture = new List<string>();
            GetEnvironmentStaticMeshTextureName(originalAnimationFbxFile, prefabMaterial[0], listOfTexture);

            List<Texture2D> textureList = new List<Texture2D>();

            int loopTextureCount = listOfTexture.Count;

            for (int j = 0; j < loopTextureCount; j++)
            {
                textureList.Add(GetEnvironmentStaticMeshTexture(originalAnimationFbxFile, listOfTexture[j]));
            }
            #endregion

            SetupAllEnvironmentStaticMeshTextureInMaterial(prefabMaterial, textureList);

            #region Prefab Setup
            //Prefab Setup
            prefab.name = prefabName;
            prefab.isStatic = true;
            prefabMeshRenderer.staticShadowCaster = true;
            prefabMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            prefabMeshRenderer.SetMaterials(prefabMaterial);
            #endregion

            string fileToCreate = prefabFolderPath + "/" + prefab.name + SUFFIX_PREFAB;
            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, fileToCreate, InteractionMode.AutomatedAction);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private static string GetMaterialsFolder(ModelImporter fbx)
    {
        string originalFilePath = fbx.assetPath;
        string modelFolderPath = Directory.GetParent(originalFilePath).ToString();
        string mapFolderPath = Directory.GetParent(modelFolderPath).ToString();
        string materialFolderPath = mapFolderPath.Substring(mapFolderPath.IndexOf("Assets")) + "/" + FOLDER_MATERIALS;

        CheckFolderExistenceAndCreateFolder(materialFolderPath, mapFolderPath, FOLDER_MATERIALS);
      
        return materialFolderPath + "/";
    }
    private static string GenerateMaterialName(ModelImporter fbx)
    {
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx.assetPath);

        string fbxFileName = fbxAsset.name.Replace("FBX_SM", "M");
        fbxFileName = fbxFileName.Replace(SUFFIX_FBX, "");

        string materialPrefix = "";
        string atlasIdSuffix = "";

        #region Get ID Suffix
        //Get Atlas ID
        char[] tempOfSuffix = new char[3];
        tempOfSuffix[0] = fbxFileName[fbxFileName.Length - 3];
        tempOfSuffix[1] = fbxFileName[fbxFileName.Length - 2];
        tempOfSuffix[2] = fbxFileName[fbxFileName.Length - 1];

        for (int i = 0; i < tempOfSuffix.Length; i++)
        {
            atlasIdSuffix = atlasIdSuffix.Insert(i, char.ToString(tempOfSuffix[i]));
        }
        #endregion

        bool isNotUsedTextureAtlasMethod = atlasIdSuffix == "_00";
        int prefixLoopCount = 6;

        if (isNotUsedTextureAtlasMethod)
        {
            prefixLoopCount = 9;
        }

        #region Get Prefix
        char[] tempOfPrefix = new char[prefixLoopCount];
        for (int i = 0; i < tempOfPrefix.Length; i++)
        {
            tempOfPrefix[i] = fbxFileName[i];
            materialPrefix = materialPrefix.Insert(i, char.ToString(tempOfPrefix[i]));
        }
        #endregion

        return materialPrefix + atlasIdSuffix + SUFFIX_MATERIAL;
    }
    private static string GetEnvironmentStaticMeshPrefabFolderPath(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string prefabFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_PREFABS);

        CheckFolderExistenceAndCreateFolder(prefabFolderPath, rootFolderPath, FOLDER_PREFABS);

        return prefabFolderPath;
    }
    private static void GetEnvironmentStaticMeshTextureName(ModelImporter fbx, Material materialOfAsset, List<string> listOfTexture)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string textureFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_TEXTURES);

        string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolderPath });
        List<Texture2D> loadedTexture = new List<Texture2D>();
        List<Texture2D> textureOfThisAtlas = new List<Texture2D>();

        string materialKeywordToSearch = materialOfAsset.name.Replace("M_", "");
 
       //Load all texture in folder
        foreach (string item in texturePaths)
        {
            loadedTexture.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(item)));
        }

        //Filter texture
        for (int i = 0; i < loadedTexture.Count; i++)
        {
            bool foundTextureWithRightKeyword = loadedTexture != null && loadedTexture[i].name.Contains(materialKeywordToSearch);

            if (foundTextureWithRightKeyword)
            {
                textureOfThisAtlas.Add(loadedTexture[i]);
            }
        }

        foreach (var item in textureOfThisAtlas)
        {
            listOfTexture.Add(item.name);
        }
    }
    private static Texture2D GetEnvironmentStaticMeshTexture(ModelImporter fbx, string textureName)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string textureFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_TEXTURES);

        string textureToLoadPath = textureFolderPath + "/" + textureName + SUFFIX_PNG;

        return AssetDatabase.LoadAssetAtPath<Texture2D>(textureToLoadPath);
    }
    private static Material GetEnvironmentStaticMeshMaterial(ModelImporter fbx)
    {
        // Make sure we have a Materials folder
        string pathToCreateMaterial = GetMaterialsFolder(fbx);
        string generatedMaterialName = GenerateMaterialName(fbx);
      
        //if material does not exist create material file in project
        bool materialIsNotExist = !AssetDatabase.LoadAssetAtPath<Material>(pathToCreateMaterial + generatedMaterialName);

        if (materialIsNotExist)
        {
            Material materialToCreate = new Material(Shader.Find(SHADER_ENVIRONMENT));
            AssetDatabase.CreateAsset(materialToCreate, pathToCreateMaterial + generatedMaterialName);
            AssetDatabase.Refresh();
        }

        Material materialToReturn = AssetDatabase.LoadAssetAtPath<Material>(pathToCreateMaterial + generatedMaterialName);

        return materialToReturn;
    }
    private static string GetShaderPropertyTextureType(string textureSuffix)
    {
        string propertyToReturn = "";
        bool isDiffuseTexture = textureSuffix == TEXSUFFIX_DIFFUSE;
        bool isEmissionTexture = textureSuffix == TEXSUFFIX_EMISSION;
        bool isMaskTexture = textureSuffix == TEXSUFFIX_MASK;

        if (isDiffuseTexture)
        {
            propertyToReturn = SHADERPROPERTY_DIFFUSE;
        }

        else if (isEmissionTexture)
        {
            propertyToReturn = SHADERPROPERTY_EMISSION;
        }

        else if (isMaskTexture)
        {
            propertyToReturn = SHADERPROPERTY_MASK;
        }

        return propertyToReturn;
    }
    private static void SetupAllEnvironmentStaticMeshTextureInMaterial(List<Material> materialListToSet, List<Texture2D> textureListToSet)
    {
        //TODO Change property when right shader come in
        int loopCount = textureListToSet.Count;

        for (int i = 0; i < loopCount; i++)
        {
            Texture2D currentTexture = textureListToSet[i];
            string textureType = GetShaderPropertyTextureType(currentTexture.name.Substring(currentTexture.name.Length - 4));
            
            bool isEmissionMap = textureType == SHADERPROPERTY_EMISSION;
            if (isEmissionMap)
            {
                materialListToSet[0].EnableKeyword("_EMISSION");
                materialListToSet[0].globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                materialListToSet[0].SetColor("_EmissionColor", Color.white);
            }

            materialListToSet[0].SetTexture(textureType, textureListToSet[i]);         
        }
    }
    #endregion

    #region Character Skinned mesh
    public static void GenerateAndSetupSkinnedMeshFiles(List<ModelImporter> fbx)
    {
        int loopCount = fbx.Count;

        for (int i = 0; i < loopCount; i++)
        {
            ModelImporter originalAnimationFbxFile = fbx[i];
            
            string fbxAssetPath = originalAnimationFbxFile.assetPath;
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            string fileName = fbxAsset.name.Substring(fbxAsset.name.IndexOf(PREFIX_SKINNEDMESH));
            string characterID = GetCharacterSkinnedMeshID(fileName);

            #region FBX Setup
            //FBX file setup
            originalAnimationFbxFile.animationType = GetAnimationType(originalAnimationFbxFile);
            originalAnimationFbxFile.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            EditorUtility.SetDirty(originalAnimationFbxFile);
            AssetDatabase.ImportAsset(fbxAssetPath, ImportAssetOptions.ForceSynchronousImport);
            #endregion

            //Clone skinned mesh into another object
            GameObject prefab = Object.Instantiate(fbxAsset);
            prefab.name = fileName;

            //Get Animator Controller component
            Animator animator = prefab.GetComponent<Animator>();
            SkinnedMeshRenderer skinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

            #region Material
            //Create Materials
            List<Material> characterMaterial = new List<Material>();
            characterMaterial.Add(GetCharacterMaterial(originalAnimationFbxFile));
            skinnedMeshRenderer.SetMaterials(characterMaterial);
            #endregion

            #region Texture
            //Set texture
            Texture2D characterTexture = GetCharacterTexture(originalAnimationFbxFile, TEXSUFFIX_DIFFUSE);

            foreach (var item in characterMaterial)
            {
                item.SetTexture(SHADERPROPERTY_DIFFUSE, characterTexture);
            }
            #endregion

            #region Generate Avatar source and Avatar Mask 

            string avatarFolderPath = GetSkinnedMeshAvatarFolderPath(originalAnimationFbxFile);
            string avatarSourcePath = GetAvatarAssetFilePath(PREFIX_ANIMAVATARSOURCE, characterID, avatarFolderPath);
            string avatarMaskPath = GetAvatarAssetFilePath(PREFIX_ANIMAVATARMASK, characterID, avatarFolderPath);

            //Avatar Source setup
            Avatar originalAvatar = animator.avatar;
            Avatar newAvatarSource = GetCharacterAvatarSource(originalAnimationFbxFile, originalAvatar, prefab);

            string newAvatarSourceName = PREFIX_ANIMAVATARSOURCE + characterID;
            newAvatarSource.name = newAvatarSourceName;                    

            //Avatar Mask setup
            AvatarMask avatarMask = new AvatarMask();
            avatarMask.AddTransformPath(prefab.transform);

            //Assign new avatar source to prefab
            animator.avatar = newAvatarSource;

            #endregion

            #region Files generation
            //Create avatar source and mask
            AssetDatabase.CreateAsset(newAvatarSource, avatarSourcePath);
            AssetDatabase.CreateAsset(avatarMask, avatarMaskPath);

            //Create prefab         
            string prefabPath = GetSkinnedMeshPrefabFolderPath(originalAnimationFbxFile);
            string fileToCreate = prefabPath + "/" + prefab.name + SUFFIX_PREFAB;

            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, fileToCreate, InteractionMode.AutomatedAction);
            #endregion
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    public static void CloneAvatarSourceAndAvatarMaskData(List<ModelImporter> fbx)
    {
        int loopCount = fbx.Count;

        for (int i = 0; i < loopCount; i++)
        {
            ModelImporter originalAnimationFbxFile = fbx[i];

            string fbxAssetPath = originalAnimationFbxFile.assetPath;
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            string fileName = fbxAsset.name.Substring(fbxAsset.name.IndexOf(PREFIX_SKINNEDMESH));
            string characterID = GetCharacterSkinnedMeshID(fileName);

            string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
            string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
            string prefabFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_PREFABS);

            //Clone skinned mesh into another object
            GameObject clonePrefab = fbxAsset;
            Animator clonePrefabAnimator = clonePrefab.GetComponent<Animator>();

            #region Load Original Data
            //Load prefab from folder
            string prefabPath = prefabFolderPath + "/" + fileName + SUFFIX_PREFAB;
            GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            //Get Animator Controller component
            Animator originalPrefabAnimator = originalPrefab.GetComponent<Animator>();

            //Avatar
            string avatarFolderPath = GetSkinnedMeshAvatarFolderPath(originalAnimationFbxFile);
            string avatarSourcePath = GetAvatarAssetFilePath(PREFIX_ANIMAVATARSOURCE, characterID, avatarFolderPath);
            string avatarMaskPath = GetAvatarAssetFilePath(PREFIX_ANIMAVATARMASK, characterID, avatarFolderPath);

            Avatar originalAvatarSource = AssetDatabase.LoadAssetAtPath<Avatar>(avatarSourcePath);
            AvatarMask originalAvatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(avatarMaskPath);
            #endregion

            #region Transfer Data
            originalAvatarMask.AddTransformPath(clonePrefab.transform);
            originalPrefabAnimator.avatar = originalAvatarSource;
            #endregion
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #region Get skinned mesh character data method
    private static string GetCharacterSkinnedMeshID(string fileName)
    {      
        return fileName.Substring(3, fileName.Length - 6);
    }
    private static string GetSkinnedMeshPrefabFolderPath(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string prefabFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_PREFABS);

        //Make sure we have Prefab Folder
        CheckFolderExistenceAndCreateFolder(prefabFolderPath, rootFolderPath, FOLDER_PREFABS);

        string pathToReturn = prefabFolderPath;
        return pathToReturn;
    }
    private static string GetSkinnedMeshAvatarFolderPath(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string animationFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_ANIMATION);

        //Make sure we have animation Folder
        CheckFolderExistenceAndCreateFolder(animationFolderPath, rootFolderPath, FOLDER_ANIMATION);   

        string animationAvatarFolderPath = animationFolderPath + "/" + FOLDER_AVATAR;

        //Make sure we have avatar Folder
        CheckFolderExistenceAndCreateFolder(animationAvatarFolderPath, animationFolderPath, FOLDER_AVATAR);

        string pathToReturn = animationAvatarFolderPath;
        return pathToReturn;
    }
    private static string GetAvatarAssetFilePath(string prefixOfAvatar, string characterID , string avatarFolderPath)
    {
        bool isAvatarSource = prefixOfAvatar == PREFIX_ANIMAVATARSOURCE;
        string suffixOfFile = "";

        if (isAvatarSource)
        {
            suffixOfFile = SUFFIX_ASSET;
        }

        else
        {
            suffixOfFile = SUFFIX_AVATARMASK;
        }

        string pathToReturn = avatarFolderPath + "/" + prefixOfAvatar + characterID + suffixOfFile;
        return pathToReturn;
    }
    private static Avatar GetCharacterAvatarSource(ModelImporter fbx, Avatar originalAvatar,GameObject prefab)
    {
        Avatar avatar = null;

        switch (fbx.animationType)
        {
            case ModelImporterAnimationType.Generic:
                avatar = AvatarBuilder.BuildGenericAvatar(prefab, "");
                break;
            case ModelImporterAnimationType.Human:
                HumanDescription humanDescription = originalAvatar.humanDescription;
                humanDescription.hasTranslationDoF = true;
                avatar = AvatarBuilder.BuildHumanAvatar(prefab, humanDescription);
                break;
        }

        return avatar;
    }
    private static Material GetCharacterMaterial(ModelImporter fbx)
    {
        Material materialToReturn = null;

        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string materialFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_MATERIALS);

        //Make sure we have material Folder
        CheckFolderExistenceAndCreateFolder(materialFolderPath, rootFolderPath, FOLDER_MATERIALS);

        string fbxFileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("SK_"));
        string characterID = fbxFileName.Substring(0, fbxFileName.Length - 4);
        string materialName = "M_" + characterID;

        string materialPath = materialFolderPath + "/" + materialName + SUFFIX_MATERIAL;

        bool materialDoesNotExist = !AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (materialDoesNotExist)
        {
            Material materialToCreate = new Material(Shader.Find(SHADER_CHARACTER));
            AssetDatabase.CreateAsset (materialToCreate, materialPath);
        }

        materialToReturn = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        return materialToReturn;
    }
    private static Texture2D GetCharacterTexture(ModelImporter fbx, string textureSuffix)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string rootFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string textureFolderPath = ConvertDirectoryPathToRelativePath(rootFolderPath + "/" + FOLDER_TEXTURES);

        string fileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));
        int startIndex = fileName.IndexOf("FBX_SK_") + "FBX_SK_".Length;
        int endIndex = fileName.LastIndexOf(".fbx");

        string textureName =  "T_" + fileName.Substring(startIndex, endIndex - startIndex) + textureSuffix + SUFFIX_PNG;       

        bool textureDoesNotExist = !AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolderPath + "/" + textureName);

        if (textureDoesNotExist)
        {
            //Create and load place holder texture
            string characterFolder = Directory.GetParent(rootFolderPath).ToString();
            string artFolder = Directory.GetParent(characterFolder).ToString();
            string placeHolderAssetsFolder = ConvertDirectoryPathToRelativePath(artFolder + "/" + FOLDER_PLACEHOLDERASSETS);

            Texture2D placeHolderCharacterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(placeHolderAssetsFolder + "/" + PLACEHOLDER_TEXTURE + SUFFIX_PNG);

            RenderTexture renderTexture = new RenderTexture(placeHolderCharacterTexture.width, placeHolderCharacterTexture.height, 0);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            RenderTexture.active = renderTexture;
            Graphics.Blit(placeHolderCharacterTexture, renderTexture);

            // Create a new Texture2D to hold the copied contents
            Texture2D copiedTexture = new Texture2D(placeHolderCharacterTexture.width, placeHolderCharacterTexture.height);
            copiedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            copiedTexture.Apply();

            // Save the copied texture as a new asset
            byte[] bytes = copiedTexture.EncodeToPNG();
            File.WriteAllBytes(textureFolderPath + "/" + textureName, bytes);
            
            AssetDatabase.Refresh();
            RenderTexture.active = null;
        }

        Texture2D textureToReturn = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolderPath + "/" + textureName);

        return textureToReturn;
    }
  
    #endregion

    #endregion

    #region Character Animation
    public static void GenerateAndSetupAnimationFiles(List<ModelImporter> fbx)
    {
        int loopCount = fbx.Count;

        for (int i = 0; i < loopCount; i++)
        {
            ModelImporter originalAnimationFbxFile = fbx[i];
            string fbxAssetPath = originalAnimationFbxFile.assetPath;

            SetupAnimationInitialDataInFbx(originalAnimationFbxFile);

            AvatarMask avatarMaskFromFBX = GetAnimationAvatarMask(originalAnimationFbxFile);
            SetupAnimationClipDataInFbx(originalAnimationFbxFile, avatarMaskFromFBX);

            EditorUtility.SetDirty(originalAnimationFbxFile);
            AssetDatabase.ImportAsset(fbxAssetPath, ImportAssetOptions.ForceSynchronousImport);

            #region Generate AnimationClip file
            AnimationClip clipFromFbx = GetAnimationClip(originalAnimationFbxFile);
            AnimationClip clipToCreate = new AnimationClip();

            EditorUtility.CopySerialized(clipFromFbx, clipToCreate);

            string clipName = clipToCreate.name;       
            string pathOfAnimClipFolder = GetAnimationClipFolderPath(originalAnimationFbxFile, clipName) + "/";
            string pathToCreateAnimationClip = pathOfAnimClipFolder + clipName + SUFFIX_ANIM;

            // AnimClip file is exist
            bool clipToCreateIsExist = AssetDatabase.LoadAssetAtPath<AnimationClip>(pathToCreateAnimationClip);
            if (clipToCreateIsExist)
            {
                AssetDatabase.DeleteAsset(pathToCreateAnimationClip);
            }

            AssetDatabase.CreateAsset(clipToCreate, pathToCreateAnimationClip);
            #endregion
        }       
    }
    private static void SetupAnimationInitialDataInFbx(ModelImporter fbx)
    {
        #region Setup AnimationType and Avatar
        Avatar avatarFromFolder = GetAnimationAvatarSource(fbx);

        fbx.animationType = GetAnimationType(fbx);
        fbx.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        fbx.sourceAvatar = avatarFromFolder;
        #endregion

        #region Setup basic animation data
        fbx.importAnimation = true;
        fbx.SaveAndReimport();

        fbx.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        fbx.animationPositionError = 0.5f;
        fbx.animationRotationError = 0.5f;
        fbx.animationScaleError = 0.5f;
        #endregion        
    }
    private static void SetupAnimationClipDataInFbx(ModelImporter fbx, AvatarMask avatarMask)
    {       
        List <AnimationClip> clips = GetAllFromModel<AnimationClip>(fbx);

        ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clips[0])) as ModelImporter;
        ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;

        //Setup Mask
        clipAnimations[0].maskType = ClipAnimationMaskType.CopyFromOther;
        clipAnimations[0].maskSource = avatarMask;

        //Setup Root motion
        //string rootMotionToSet = ROOTMOTIONLOCATOR;
        //fbx.motionNodeName = rootMotionToSet;
    
        #region Name setup
        string prefixName = "A_";
        string animationType = GetAnimationTypeName(fbx) + "_";
        string characterID = GetAnimationCharacterID(fbx) + "_";
        string animationID = GetAnimationID(fbx) + "_";
        string originalClipName = clipAnimations[0].name + "_";
        string animationTypeSuffix = GetAnimationTypeSuffix(fbx);

        string clipNameToChange = prefixName + animationType + characterID + animationID + originalClipName + animationTypeSuffix;

        clipAnimations[0].name = clipNameToChange;
        #endregion

        #region Loop setup
        bool clipIsLoopType = animationTypeSuffix == "LP";
        if (clipIsLoopType)
        {
            clipAnimations[0].loopTime = true;
        }
        #endregion

        modelImporter.clipAnimations = clipAnimations;
    }
    private static Avatar GetAnimationAvatarSource(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxfileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string animationFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string avatarFolderPath = animationFolderPath + "/" + FOLDER_AVATAR;

        string avatarSourcePrefix = PREFIX_ANIMAVATARSOURCE;

        char[] fileID = new char[6];
        int loopCount = fileID.Length;
        int startIndex = 6;

        for (int i = 0; i < loopCount; i++)
        {
            fileID[i] = fbxfileName[i+ startIndex];
        }

        string avatarID = new string(fileID);
        string pathToGetFile = ConvertDirectoryPathToRelativePath(avatarFolderPath + "/" + avatarSourcePrefix + avatarID + SUFFIX_ASSET);

        Avatar avatarToReturn = AssetDatabase.LoadAssetAtPath<Avatar>(pathToGetFile);
        
        return avatarToReturn;
    }
    private static AvatarMask GetAnimationAvatarMask(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxfileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string animationFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string avatarFolderPath = animationFolderPath + "/" + FOLDER_AVATAR;

        string avatarMaskPrefix = PREFIX_ANIMAVATARMASK;

        char[] fileID = new char[6];
        int loopCount = fileID.Length;
        int startIndex = 6;

        for (int i = 0; i < loopCount; i++)
        {
            fileID[i] = fbxfileName[i + startIndex];
        }

        string avatarID = new string(fileID);
        string pathToGetFile = ConvertDirectoryPathToRelativePath(avatarFolderPath + "/" + avatarMaskPrefix + avatarID + SUFFIX_AVATARMASK);

        AvatarMask avatarMaskToReturn = AssetDatabase.LoadAssetAtPath<AvatarMask>(pathToGetFile);

        return avatarMaskToReturn;
    }
    private static AnimationClip GetAnimationClip(ModelImporter fbx)
    {
        List<AnimationClip> clips = GetAllFromModel<AnimationClip>(fbx);
        int indexOfAnimationClip = 0;

        for (int i = 0; i < clips.Count; i++)
        {
            if (!clips[i].name.Contains("__preview__"))
            {
                indexOfAnimationClip = i;
            }
        }

        AnimationClip clipToReturn = clips[indexOfAnimationClip];
        clipToReturn.name = clips[indexOfAnimationClip].name;

        return clipToReturn;
    }
    private static string GetAnimationClipFolderPath(ModelImporter fbx, string originalClipname)
    {
        string fbxAssetPath = fbx.assetPath;
        string fbxFolderPath = Directory.GetParent(fbxAssetPath).ToString();
        string animationFolderPath = Directory.GetParent(fbxFolderPath).ToString();
        string animationClipFolderPath = ConvertDirectoryPathToRelativePath(animationFolderPath + "/" + FOLDER_ANIMCLIPS);

        // Make sure we have a AnimClips folder
        CheckFolderExistenceAndCreateFolder(animationClipFolderPath, animationFolderPath, FOLDER_ANIMCLIPS);       

        string folderPathToGenerateAsset = animationClipFolderPath;
        return folderPathToGenerateAsset;
    }
    private static ModelImporterAnimationType GetAnimationType(ModelImporter fbx)
    {
        ModelImporterAnimationType typeToSet = ModelImporterAnimationType.None;

        string fbxAssetPath = fbx.assetPath;
        string fileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        char animationType = fileName[4];
        bool isGenericType = animationType == 'G';

        if (isGenericType)
        {
            typeToSet = ModelImporterAnimationType.Generic;
        }

        else
        {
            typeToSet = ModelImporterAnimationType.Human;
        }

        return typeToSet;
    }

    #region Animation Name Method
    private static string GetAnimationTypeName(ModelImporter fbx)
    {
        string dataToReturn = "";
        string fbxAssetPath = fbx.assetPath;
        string fileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        char animationType = fileName[4];

        switch (animationType)
        {
            case 'G':
                dataToReturn = "G";
                break;
            case 'H':
                dataToReturn = "H";
                break;
        }

        return dataToReturn;
    }
    private static string GetAnimationCharacterID(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        char[] animationID = new char[7];
        int loopCount = animationID.Length;
        int startIndex = 6;

        for (int i = 0; i < loopCount; i++)
        {
            animationID[i] = fileName[i + startIndex];
        }

        string dataToReturn = new string(animationID);

        return dataToReturn;
    }
    private static string GetAnimationID(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        char[] animationID = new char[2];
        int loopCount = animationID.Length;
        int startIndex = 14;

        for (int i = 0; i < loopCount; i++)
        {
            animationID[i] = fileName[i + startIndex];
        }

        string dataToReturn = new string(animationID);
        return dataToReturn;
    }
    private static string GetAnimationTypeSuffix(ModelImporter fbx)
    {
        string fbxAssetPath = fbx.assetPath;
        string fileName = fbxAssetPath.Substring(fbxAssetPath.IndexOf("FBX_"));

        int fileSuffixToDelete = fileName.IndexOf(SUFFIX_FBX);
        bool findFileSuffixToDelete = fileSuffixToDelete != -1;

        string nameWithoutSuffix = "";
        string fileSuffix = "";
        if (findFileSuffixToDelete)
        {
            nameWithoutSuffix = fileName.Remove(fileSuffixToDelete);
        }

        char[] animationTypeSuffix = new char[2];
        animationTypeSuffix[0] = nameWithoutSuffix[nameWithoutSuffix.Length - 2];
        animationTypeSuffix[1] = nameWithoutSuffix[nameWithoutSuffix.Length - 1];

        for (int i = 0; i < animationTypeSuffix.Length; i++)
        {
            fileSuffix = fileSuffix.Insert(i, char.ToString(animationTypeSuffix[i]));
        }

        return fileSuffix;
    }
    #endregion

    #endregion

    #region AssetDataBase Utility
    public static string ConvertDirectoryPathToRelativePath(string directoryPathToConvert)
    {
        return directoryPathToConvert.Substring(directoryPathToConvert.IndexOf(FOLDER_RELATIVEASSETS));
    }
    public static List<T> GetAllFromModel<T>(ModelImporter fbx) where T : class
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbx.assetPath).Where(x => x is T).Select(x => (x as T)).ToList();
    }
    public static void CheckFolderExistenceAndCreateFolder(string pathToCheckFolder, string parentPathToCreateFolder, string folderName)
    {
        //Check folder exist
        bool folderDoesNotExist = !AssetDatabase.IsValidFolder(pathToCheckFolder);

        if (folderDoesNotExist)
        {
            string pathToCreateFolder = ConvertDirectoryPathToRelativePath(parentPathToCreateFolder);
            AssetDatabase.CreateFolder(pathToCreateFolder, folderName);
        }
    }
    #endregion

}
