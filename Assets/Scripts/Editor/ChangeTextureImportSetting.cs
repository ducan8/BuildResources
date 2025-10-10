using System.IO;
using UnityEditor;
using UnityEngine;

// /////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Batch Texture import settings modifier.
//
// Modifies all selected textures in the project window and applies the requested modification on the
// textures. Idea was to have the same choices for multiple files as you would have if you open the
// import settings of a single texture. Put this into Assets/Editor and once compiled by Unity you find
// the new functionality in HelpTools -> Texture import settings. Enjoy! :-)
//
// Based on the great work of benblo in this thread:
// http://forum.unity3d.com/viewtopic.php?t=16079&start=0&postdays=0&postorder=asc&highlight=textureimporter
//
// Developed by Martin Schultz, Decane in August 2009
// e-mail: ms@decane.net

// Extended by jite in September 2012
// + 5 platforms overrides support (set/clear)
// + mipmap mode, read/write mode, filter mode, aniso level, wrap mode
// + 1 predefined params complex set
// * textures formats for Unity 3.5.5
// * saves selection
// e-mail: jite.gs@gmail.com
//
// /////////////////////////////////////////////////////////////////////////////////////////////////////////
public class ChangeTextureImportSettings : ScriptableObject
{
    private static int currentMaxTextureSize;
    private static TextureImporterFormat currentTIFormat;
    private static string logTitle = "ChangeTextureImportSettings. ";

    //--- Internal Class ------------------------

    private class TextureImportParams
    {
        public Platform platform;
        public Actions action;
        public TextureImporterFormat tiFormat;
        public int maxSize;
        public bool mipMap;
        public bool readWriteMode;
        public FilterMode filterMode;
        public int anisoLevel;
        public TextureWrapMode wrapMode;

        public TextureImportParams(Actions oneAction, Platform somePlatform = ChangeTextureImportSettings.Platform.Default)
        {
            platform = somePlatform;
            action = oneAction;
        }
    }

    //--- Enums ---------------------------------

    public enum Platform
    {
        Default,
        Web,
        Standalone,
        iPhone,
        Android,
        FlashPlayer,
        All
    }

    private enum Actions
    {
        SetAll,
        SetTextureFormat,
        SetMaxTextureSize,
        SetMipMap,
        SetReadWrite,
        SetFilterMode,
        SetAniso,
        SetWrapMode,
        ClearOverrides
    }

    [MenuItem("HelpTools/Texture import settings/Set predefined params (like defaults, see in script)")]
    private static void SelectedSetDefaults()
    {
        TextureImportParams tiParams = new TextureImportParams(Actions.SetAll, Platform.Default);
        tiParams.anisoLevel = 1;
        tiParams.filterMode = FilterMode.Bilinear;
        tiParams.maxSize = 4096;
        tiParams.mipMap = false;
        tiParams.readWriteMode = true;
        tiParams.tiFormat = TextureImporterFormat.RGBA32;
        tiParams.wrapMode = TextureWrapMode.Clamp;
        Debug.Log(System.String.Format(
          "{0} Set predefined params @ {1} platform: TextureImporterFormat {2}, MaxTextureSize {3}, MipMap {4}, RWMode {5}, FilterMode {6}, AnisoLevel {7}, WrapMode {8}",
          logTitle, tiParams.platform, tiParams.tiFormat, tiParams.maxSize, tiParams.mipMap, tiParams.readWriteMode,
          tiParams.filterMode, tiParams.anisoLevel, tiParams.wrapMode));
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB Compressed DXT1")]
    private static void ChangeTextureFormat_Android_DXT1()
    {
        ChangeTextureFormat(TextureImporterFormat.DXT1, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB Compressed DXT5")]
    private static void ChangeTextureFormat_Android_DXT5()
    {
        ChangeTextureFormat(TextureImporterFormat.DXT5, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB Compressed ETC 4 bit")]
    private static void ChangeTextureFormat_Android_ETC_RGB4()
    {
        ChangeTextureFormat(TextureImporterFormat.ETC_RGB4, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB Compressed PVRTC 2 bit")]
    private static void ChangeTextureFormat_Android_PVRTC_RGB2()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGB2, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGBA Compressed PVRTC 2 bit")]
    private static void ChangeTextureFormat_Android_PVRTC_RGBA2()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGBA2, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB Compressed PVRTC 4 bit")]
    private static void ChangeTextureFormat_Android_PVRTC_RGB4()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGB4, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGBA Compressed PVRTC 4 bit")]
    private static void ChangeTextureFormat_Android_PVRTC_RGBA4()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGBA4, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB Compressed ATC 4 bit")]
    private static void ChangeTextureFormat_Android_ATC_RGB4()
    {
        ChangeTextureFormat(TextureImporterFormat.ETC_RGB4, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGBA Compressed ATC 8 bit")]
    private static void ChangeTextureFormat_Android_ATC_RGBA8()
    {
        ChangeTextureFormat(TextureImporterFormat.ETC2_RGBA8, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB 16 bit")]
    private static void ChangeTextureFormat_Android_RGB16()
    {
        ChangeTextureFormat(TextureImporterFormat.RGB16, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGB 24 bit")]
    private static void ChangeTextureFormat_Android_RGB24()
    {
        ChangeTextureFormat(TextureImporterFormat.RGB24, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/Alpha 8 bit")]
    private static void ChangeTextureFormat_Android_Alpha8()
    {
        ChangeTextureFormat(TextureImporterFormat.Alpha8, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGBA 16 bit")]
    private static void ChangeTextureFormat_Android_ARGB16()
    {
        ChangeTextureFormat(TextureImporterFormat.ARGB16, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/Android/RGBA 32 bit")]
    private static void ChangeTextureFormat_Android_RGBA32()
    {
        ChangeTextureFormat(TextureImporterFormat.RGBA32, Platform.Android);
    }

    //--- ChangeTextureFormat. iPhone

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGB Compressed PVRTC 2 bit")]
    private static void ChangeTextureFormat_iPhone_PVRTC_RGB2()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGB2, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGBA Compressed PVRTC 2 bit")]
    private static void ChangeTextureFormat_iPhone_PVRTC_RGBA2()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGBA2, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGB Compressed PVRTC 4 bit")]
    private static void ChangeTextureFormat_iPhone_PVRTC_RGB4()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGB4, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGBA Compressed PVRTC 4 bit")]
    private static void ChangeTextureFormat_iPhone_PVRTC_RGBA4()
    {
        ChangeTextureFormat(TextureImporterFormat.PVRTC_RGBA4, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGB 16 bit")]
    private static void ChangeTextureFormat_iPhone_RGB16()
    {
        ChangeTextureFormat(TextureImporterFormat.RGB16, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGB 24 bit")]
    private static void ChangeTextureFormat_iPhone_RGB24()
    {
        ChangeTextureFormat(TextureImporterFormat.RGB24, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/Alpha 8 bit")]
    private static void ChangeTextureFormat_iPhone_Alpha8()
    {
        ChangeTextureFormat(TextureImporterFormat.Alpha8, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGBA 16 bit")]
    private static void ChangeTextureFormat_iPhone_ARGB16()
    {
        ChangeTextureFormat(TextureImporterFormat.ARGB16, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/iPhone/RGBA 32 bit")]
    private static void ChangeTextureFormat_iPhone_RGBA32()
    {
        ChangeTextureFormat(TextureImporterFormat.RGBA32, Platform.iPhone);
    }

    //--- ChangeTextureFormat. FlashPlayer

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/FlashPlayer/RGB 24 bit")]
    private static void ChangeTextureFormat_FlashPlayer_RGB24()
    {
        ChangeTextureFormat(TextureImporterFormat.RGB24, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Texture Format/FlashPlayer/RGBA 32 bit")]
    private static void ChangeTextureFormat_FlashPlayer_RGBA32()
    {
        ChangeTextureFormat(TextureImporterFormat.RGBA32, Platform.FlashPlayer);
    }

    //--- ChangeMaxTextureSize ------------------

    //--- ChangeMaxTextureSize. Default

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/32")]
    private static void ChangeTextureSize_32()
    {
        ChangeMaxTextureSize(32);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/64")]
    private static void ChangeTextureSize_64()
    {
        ChangeMaxTextureSize(64);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/128")]
    private static void ChangeTextureSize_128()
    {
        ChangeMaxTextureSize(128);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/256")]
    private static void ChangeTextureSize_256()
    {
        ChangeMaxTextureSize(256);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/512")]
    private static void ChangeTextureSize_512()
    {
        ChangeMaxTextureSize(512);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/1024")]
    private static void ChangeTextureSize_1024()
    {
        ChangeMaxTextureSize(1024);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/2048")]
    private static void ChangeTextureSize_2048()
    {
        ChangeMaxTextureSize(2048);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Default/4096")]
    private static void ChangeTextureSize_4096()
    {
        ChangeMaxTextureSize(4096);
    }

    //--- ChangeMaxTextureSize. Web

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/32")]
    private static void ChangeTextureSizeWeb_32()
    {
        ChangeMaxTextureSize(32, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/64")]
    private static void ChangeTextureSizeWeb_64()
    {
        ChangeMaxTextureSize(64, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/128")]
    private static void ChangeTextureSizeWeb_128()
    {
        ChangeMaxTextureSize(128, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/256")]
    private static void ChangeTextureSizeWeb_256()
    {
        ChangeMaxTextureSize(256, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/512")]
    private static void ChangeTextureSizeWeb_512()
    {
        ChangeMaxTextureSize(512, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/1024")]
    private static void ChangeTextureSizeWeb_1024()
    {
        ChangeMaxTextureSize(1024, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/2048")]
    private static void ChangeTextureSizeWeb_2048()
    {
        ChangeMaxTextureSize(2048, Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Web/4096")]
    private static void ChangeTextureSizeWeb_4096()
    {
        ChangeMaxTextureSize(4096, Platform.Web);
    }

    //--- ChangeMaxTextureSize. Standalone

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/32")]
    private static void ChangeTextureSizeStandalone_32()
    {
        ChangeMaxTextureSize(32, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/64")]
    private static void ChangeTextureSizeStandalone_64()
    {
        ChangeMaxTextureSize(64, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/128")]
    private static void ChangeTextureSizeStandalone_128()
    {
        ChangeMaxTextureSize(128, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/256")]
    private static void ChangeTextureSizeStandalone_256()
    {
        ChangeMaxTextureSize(256, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/512")]
    private static void ChangeTextureSizeStandalone_512()
    {
        ChangeMaxTextureSize(512, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/1024")]
    private static void ChangeTextureSizeStandalone_1024()
    {
        ChangeMaxTextureSize(1024, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/2048")]
    private static void ChangeTextureSizeStandalone_2048()
    {
        ChangeMaxTextureSize(2048, Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Standalone/4096")]
    private static void ChangeTextureSizeStandalone_4096()
    {
        ChangeMaxTextureSize(4096, Platform.Standalone);
    }

    //--- ChangeMaxTextureSize. Android

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/32")]
    private static void ChangeTextureSizeAndroid_32()
    {
        ChangeMaxTextureSize(32, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/64")]
    private static void ChangeTextureSizeAndroid_64()
    {
        ChangeMaxTextureSize(64, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/128")]
    private static void ChangeTextureSizeAndroid_128()
    {
        ChangeMaxTextureSize(128, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/256")]
    private static void ChangeTextureSizeAndroid_256()
    {
        ChangeMaxTextureSize(256, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/512")]
    private static void ChangeTextureSizeAndroid_512()
    {
        ChangeMaxTextureSize(512, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/1024")]
    private static void ChangeTextureSizeAndroid_1024()
    {
        ChangeMaxTextureSize(1024, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/2048")]
    private static void ChangeTextureSizeAndroid_2048()
    {
        ChangeMaxTextureSize(2048, Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/Android/4096")]
    private static void ChangeTextureSizeAndroid_4096()
    {
        ChangeMaxTextureSize(4096, Platform.Android);
    }

    //--- ChangeMaxTextureSize. iPhone

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/32")]
    private static void ChangeTextureSizeIPhone_32()
    {
        ChangeMaxTextureSize(32, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/64")]
    private static void ChangeTextureSizeIPhone_64()
    {
        ChangeMaxTextureSize(64, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/128")]
    private static void ChangeTextureSizeIPhone_128()
    {
        ChangeMaxTextureSize(128, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/256")]
    private static void ChangeTextureSizeIPhone_256()
    {
        ChangeMaxTextureSize(256, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/512")]
    private static void ChangeTextureSizeIPhone_512()
    {
        ChangeMaxTextureSize(512, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/1024")]
    private static void ChangeTextureSizeIPhone_1024()
    {
        ChangeMaxTextureSize(1024, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/2048")]
    private static void ChangeTextureSizeIPhone_2048()
    {
        ChangeMaxTextureSize(2048, Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/iPhone/4096")]
    private static void ChangeTextureSizeIPhone_4096()
    {
        ChangeMaxTextureSize(4096, Platform.iPhone);
    }

    //--- ChangeMaxTextureSize. FlashPlayer

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/32")]
    private static void ChangeTextureSizeFlashPlayer_32()
    {
        ChangeMaxTextureSize(32, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/64")]
    private static void ChangeTextureSizeFlashPlayer_64()
    {
        ChangeMaxTextureSize(64, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/128")]
    private static void ChangeTextureSizeFlashPlayer_128()
    {
        ChangeMaxTextureSize(128, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/256")]
    private static void ChangeTextureSizeFlashPlayer_256()
    {
        ChangeMaxTextureSize(256, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/512")]
    private static void ChangeTextureSizeFlashPlayer_512()
    {
        ChangeMaxTextureSize(512, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/1024")]
    private static void ChangeTextureSizeFlashPlayer_1024()
    {
        ChangeMaxTextureSize(1024, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/2048")]
    private static void ChangeTextureSizeFlashPlayer_2048()
    {
        ChangeMaxTextureSize(2048, Platform.FlashPlayer);
    }

    [MenuItem("HelpTools/Texture import settings/Change Max Texture Size/FlashPlayer/4096")]
    private static void ChangeTextureSizeFlashPlayer_4096()
    {
        ChangeMaxTextureSize(4096, Platform.FlashPlayer);
    }

    //--- ChangeMipMap --------------------------

    [MenuItem("HelpTools/Texture import settings/Change MipMap/Enable MipMap")]
    private static void ChangeMipMap_On()
    {
        ChangeMipMap(true);
    }

    [MenuItem("HelpTools/Texture import settings/Change MipMap/Disable MipMap")]
    private static void ChangeMipMap_Off()
    {
        ChangeMipMap(false);
    }

    //--- Change ReadWrite ----------------------

    [MenuItem("HelpTools/Texture import settings/Change ReadWrite/Enable")]
    private static void ChangeRW_On()
    {
        ChangeRW(true);
    }

    [MenuItem("HelpTools/Texture import settings/Change ReadWrite/Disable")]
    private static void ChangeRW_Off()
    {
        ChangeRW(false);
    }

    //--- Change WrapMode -----------------------

    [MenuItem("HelpTools/Texture import settings/Change WrapMode/Clamp")]
    private static void ChangeWrapMode_On()
    {
        ChangeWrapMode(TextureWrapMode.Clamp);
    }

    [MenuItem("HelpTools/Texture import settings/Change WrapMode/Repeat")]
    private static void ChangeWrapMode_Off()
    {
        ChangeWrapMode(TextureWrapMode.Repeat);
    }

    //--- Change FilterMode ---------------------

    [MenuItem("HelpTools/Texture import settings/Change FilterMode/Point")]
    private static void ChangeFilterMode_Point()
    {
        ChangeFilterMode(FilterMode.Point);
    }

    [MenuItem("HelpTools/Texture import settings/Change FilterMode/Bilinear")]
    private static void ChangeFilterMode_Bilinear()
    {
        ChangeFilterMode(FilterMode.Bilinear);
    }

    [MenuItem("HelpTools/Texture import settings/Change FilterMode/Trilinear")]
    private static void ChangeFilterMode_Trilinear()
    {
        ChangeFilterMode(FilterMode.Trilinear);
    }

    //--- Change Aniso level ---------------------

    [MenuItem("HelpTools/Texture import settings/Change Aniso level/0")]
    private static void ChangeAniso_0()
    {
        ChangeAniso(0);
    }

    [MenuItem("HelpTools/Texture import settings/Change Aniso level/1")]
    private static void ChangeAniso_1()
    {
        ChangeAniso(1);
    }

    [MenuItem("HelpTools/Texture import settings/Change Aniso level/2")]
    private static void ChangeAniso_2()
    {
        ChangeAniso(2);
    }

    [MenuItem("HelpTools/Texture import settings/Change Aniso level/3")]
    private static void ChangeAniso_3()
    {
        ChangeAniso(3);
    }

    [MenuItem("HelpTools/Texture import settings/Change Aniso level/4")]
    private static void ChangeAniso_4()
    {
        ChangeAniso(4);
    }

    //--- Clear platform overrides --------------

    [MenuItem("HelpTools/Texture import settings/Clear platform overrides/All")]
    private static void SelectedClearOverrides_All()
    {
        ClearOverrides();
    }

    [MenuItem("HelpTools/Texture import settings/Clear platform overrides/Standalone")]
    private static void SelectedClearOverrides_Standalone()
    {
        ClearOverrides(Platform.Standalone);
    }

    [MenuItem("HelpTools/Texture import settings/Clear platform overrides/Android")]
    private static void SelectedClearOverrides_Android()
    {
        ClearOverrides(Platform.Android);
    }

    [MenuItem("HelpTools/Texture import settings/Clear platform overrides/iPhone")]
    private static void SelectedClearOverrides_iPhone()
    {
        ClearOverrides(Platform.iPhone);
    }

    [MenuItem("HelpTools/Texture import settings/Clear platform overrides/Web")]
    private static void SelectedClearOverrides_Web()
    {
        ClearOverrides(Platform.Web);
    }

    [MenuItem("HelpTools/Texture import settings/Clear platform overrides/FlashPlayer")]
    private static void SelectedClearOverrides_FlashPlayer()
    {
        ClearOverrides(Platform.FlashPlayer);
    }

    //--- Work ----------------------------------

    private static void ChangeRW(bool flag, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set ReadWriteMode '{2}' @ {1} platform", logTitle, somePlatform, flag));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetReadWrite, somePlatform);
        tiParams.readWriteMode = flag;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    private static void ChangeWrapMode(TextureWrapMode newMode, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set TextureWrapMode '{2}' @ {1} platform", logTitle, somePlatform, newMode));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetWrapMode, somePlatform);
        tiParams.wrapMode = newMode;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    private static void ChangeFilterMode(FilterMode mode, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set FilterMode '{2}' @ {1} platform", logTitle, somePlatform, mode));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetFilterMode, somePlatform);
        tiParams.filterMode = mode;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    private static void ChangeAniso(int newLevel, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set AnisoLevel '{2}' @ {1} platform", logTitle, somePlatform, newLevel));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetAniso, somePlatform);
        tiParams.anisoLevel = newLevel;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    private static void ChangeMipMap(bool flag, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set MipMap '{2}' @ {1} platform", logTitle, somePlatform, flag));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetMipMap, somePlatform);
        tiParams.mipMap = flag;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    private static void ChangeMaxTextureSize(int newSize, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set MaxTextureSize '{2}' @ {1} platform", logTitle, somePlatform, newSize));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetMaxTextureSize, somePlatform);
        tiParams.maxSize = newSize;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    public static void ChangeTextureFormat(TextureImporterFormat newFormat, Platform somePlatform = Platform.Default)
    {
        Debug.Log(System.String.Format("{0} Set TextureImporterFormat '{2}' @ {1} platform", logTitle, somePlatform, newFormat));
        TextureImportParams tiParams = new TextureImportParams(Actions.SetTextureFormat, somePlatform);
        tiParams.tiFormat = newFormat;
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    private static void ClearOverrides(Platform somePlatform = Platform.All)
    {
        Debug.Log(System.String.Format("{0} Clear overrides @ {1} platform", logTitle, somePlatform));
        TextureImportParams tiParams = new TextureImportParams(Actions.ClearOverrides, somePlatform);
        SelectedChangeAnyPlatformSettings(tiParams);
    }

    /// <summary>
    /// Main work method
    /// </summary>
    private static void SelectedChangeAnyPlatformSettings(TextureImportParams tip)
    {
        // string FolderSelect = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");

        int processingTexturesNumber;
        Object[] originalSelection = Selection.objects;
        Object[] textures = GetSelectedTextures();
        Selection.objects = new Object[0]; //Clear selection (for correct data representation on GUI)
        processingTexturesNumber = textures.Length;
        if (processingTexturesNumber == 0)
        {
            Debug.LogWarning(logTitle + "Nothing to do. Please select objects/folders with 2d textures in Project tab");
            return;
        }
        AssetDatabase.StartAssetEditing();
        foreach (Texture2D texture in textures)
        {
            string path = AssetDatabase.GetAssetPath(texture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;


            textureImporter.SetPlatformTextureSettings(Platform.Android.ToString(), 2048, TextureImporterFormat.ETC2_RGBA8Crunched, 100, false);


            //TextureImporterPlatformSettings _Settings = new TextureImporterPlatformSettings();
            //_Settings.compressionQuality = 0;
            //_Settings.maxTextureSize = 2048;
            //_Settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            //_Settings.format = TextureImporterFormat.ETC2_RGBA8Crunched;


            //textureImporter.SetPlatformTextureSettings(_Settings);



            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);


        }

        AssetDatabase.StopAssetEditing();
        Selection.objects = originalSelection; //Restore selection
        Debug.Log("Textures processed: " + processingTexturesNumber);

        //textures = GetSelectedTextures();

        //foreach (Texture2D texture in textures)
        //{
        //    string path = AssetDatabase.GetAssetPath(texture);

        //    string name = Path.GetFileNameWithoutExtension(path);


        //    string Root = Path.GetDirectoryName(path);

        //    string outputFolder = FolderSelect + "//" + Root;

        //    if (!Directory.Exists(outputFolder))
        //    {
        //        Directory.CreateDirectory(outputFolder);
        //    }

        //    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        //    Debug.Log("WRITER BUILD : " + outputFolder + "/" + name + ".unity3d");

        //    BuildPipeline.BuildAssetBundle(texture, null, outputFolder + "/" + name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
        //}

    }

    private static Object[] GetSelectedTextures()
    {
        return Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
    }

    private static void ClearPlatformOverrides(string platformName, TextureImporter importer)
    {

    }
}