using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class ABTools :Editor
{
    static string resPath = Application.dataPath + "/Res";
    [MenuItem("ABTools/SetBundleName")]
    static void SetBundleName()
    {
        Debug.Log("Begin:SetAssetBundleName On " + resPath);
        DirectoryInfo resDirectory = new DirectoryInfo(resPath);
        DirectoryInfo[] sceneDirectory = resDirectory.GetDirectories();
        foreach (DirectoryInfo sceneDirtItem in sceneDirectory)
        {
            Dictionary<string, string> namePathDict = new Dictionary<string, string>();
            OnSceneFileStreamInfo(sceneDirtItem, sceneDirtItem.Name, namePathDict);
            OnWriteConfig(sceneDirtItem.Name, namePathDict);
        }
    }

    private static void OnWriteConfig(string sceneName, Dictionary<string, string> namePathDict)
    {
        string path = Application.streamingAssetsPath + "/" + sceneName + "_Record.txt";

        if (!Directory.Exists(Application.streamingAssetsPath))
            Directory.CreateDirectory(Application.streamingAssetsPath);
      
        using (FileStream fs = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Write))
        {
            using(StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(namePathDict.Count);
                foreach (KeyValuePair<string, string> item in namePathDict)
                    sw.WriteLine(item.Key + "  " + item.Value);
            }
        }
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileSystemInfo">�ļ���Ϣ</param>
    /// <param name="name">����</param>
    /// <param name="namePathDict"></param>
    private static void OnSceneFileStreamInfo(FileSystemInfo fileSystemInfo, string sceneName, Dictionary<string, string> namePathDict)
    {
        DirectoryInfo directoryInfo = fileSystemInfo as DirectoryInfo;
        FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
        foreach (var item in fileSystemInfos)
        {
            FileInfo fileInfo = item as FileInfo;
            if (fileInfo == null)  //�ļ��м����ݹ����
                OnSceneFileStreamInfo(item, sceneName, namePathDict);
            else                   //�ļ�
                setLable(fileInfo, sceneName, namePathDict);
        }
    }
    private static void setLable(FileInfo fileInfo, string sceneName, Dictionary<string, string> namePathDict)
    {
        if (fileInfo.Extension.Equals(".meta"))
            return;
        string bundleName = getBundleName(fileInfo, sceneName);
        if (bundleName == null)
            return;
        string bundlePath = fileInfo.FullName.Substring(fileInfo.FullName.IndexOf("Assets"));
        AssetImporter assetImporter = AssetImporter.GetAtPath(bundlePath);
        assetImporter.assetBundleName = bundleName;
        assetImporter.assetBundleVariant = "assetbundle";

        string folderName = string.Empty;
        if (bundleName.Contains("/"))
            folderName = bundleName.Split('/')[1];
        else
            folderName = bundleName;

        if (!namePathDict.ContainsKey(folderName))
            namePathDict.Add(folderName, bundleName + "." + assetImporter.assetBundleVariant);

    }
    private static string getBundleName(FileInfo fileInfo, string sceneName)
    {
        string windowPath = fileInfo.FullName;
        string unityPath = windowPath.Replace(@"\", "/");

        int index = unityPath.IndexOf(sceneName)+sceneName.Length+1;
        string bundlePath = unityPath.Substring(index);
        if(bundlePath.Contains("/"))
        {
            if (bundlePath.Split('/')[0] == "LuaScripts" && fileInfo.Extension.Equals(".lua"))
                return IsLuaScriptsFile(fileInfo, bundlePath, sceneName);
            return sceneName + "/" + bundlePath.Split('/')[0];  //���ܰ�
        }else
        {
            return sceneName + "/" + fileInfo.Name;  //����Դ��
        }
    }
    private static string IsLuaScriptsFile(FileInfo fileInfo,string bundlePath ,string sceneName)
    {
        string newFilePath = fileInfo.FullName + ".txt";
        File.Move(fileInfo.FullName, newFilePath);
        SecurityUtil.XorToTxtPath(newFilePath);
        AssetImporter asset = AssetImporter.GetAtPath(newFilePath.Substring(newFilePath.IndexOf("Assets")));
        asset.assetBundleName = sceneName + "/" + bundlePath.Split('/')[0];
        asset.assetBundleVariant = "assetbundle";
        return null;
    }
}
/// <summary>
/// �ļ�����---�˴������ڹ����� ���Կ�����������
/// </summary>
public sealed class SecurityUtil
{
    private SecurityUtil() { }

    #region xorScale �������
    /// <summary>
    /// �������
    /// </summary>
    private static readonly byte[] xorScale = new byte[] { 45, 66, 38, 55, 23, 254, 9, 165, 90, 19, 41, 45, 201, 58, 55, 37, 254, 185, 165, 169, 19, 171 };//.data�ļ���xor�ӽ�������
    #endregion //��������ܳ���256

    /// <summary>
    /// ������������
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static byte[] Xor(byte[] buffer)
    {
        int iScaleLen = xorScale.Length;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(buffer[i] ^ xorScale[i % iScaleLen]);
        }
        return buffer;
    }
    public static void XorToTxtPath(string txtPath)
    {
        byte[] bytes = File.ReadAllBytes(txtPath);
        byte[] newBytes = SecurityUtil.Xor(bytes);
        File.WriteAllBytes(txtPath, newBytes);
    }
}