// Decompiled with JetBrains decompiler
// Type: DllSwitcher
// Assembly: DllSwitcher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B4178718-2A83-4686-A7E9-A256F6BFAB35
// Assembly location: F:\Projects\NMY\DllSwitcher.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class DllSwitcher : EditorWindow
{
    public const string DEFAULT_FILE_ID_OF_SCRIPT = "11500000";
    public UnityEngine.Object dllFile;
    public UnityEngine.Object replaceDir;
    public UnityEngine.Object srcDir;
    public string srcDirPath;
    public string replaceDirPath;
    private Dictionary<string, string> fileIDMappingTableFromDll;
    private Dictionary<string, string> guidMappingTableFromScripts;
    private string guidOfDllFile;
    private string dllFilePath;
    private const int PreLabelWidth = 140;
    private DllSwitcher.PathType dllInputPath;
    private DllSwitcher.DirectoryType srcDirectory;
    private DllSwitcher.DirectoryType resDirectory;

    [MenuItem("Window/DllSwitcher")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(DllSwitcher));
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
        EditorGUILayout.LabelField("Dll File", new GUILayoutOption[1]
        {
      GUILayout.MaxWidth(140f)
        });
        this.dllInputPath = (DllSwitcher.PathType)EditorGUILayout.EnumPopup((Enum)this.dllInputPath, new GUILayoutOption[1]
        {
      GUILayout.ExpandWidth(true)
        });
        EditorGUILayout.EndHorizontal();
        if (this.dllInputPath.Equals((object)DllSwitcher.PathType.Reference))
            this.dllFile = EditorGUILayout.ObjectField(this.dllFile, typeof(System.Object), false, new GUILayoutOption[0]);
        else
            this.dllFilePath = EditorGUILayout.TextField(this.dllFilePath, new GUILayoutOption[0]);


        EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
        EditorGUILayout.LabelField("Source Code Directory", new GUILayoutOption[1]
        {
      GUILayout.MaxWidth(140f)
        });
        this.srcDirectory = (DllSwitcher.DirectoryType)EditorGUILayout.EnumPopup((Enum)this.srcDirectory, new GUILayoutOption[1]
        {
      GUILayout.ExpandWidth(true)
        });
        EditorGUILayout.EndHorizontal();
        if (this.srcDirectory.Equals((object)DllSwitcher.DirectoryType.SpecificDirectory))
            this.srcDir = EditorGUILayout.ObjectField(this.srcDir, typeof(UnityEngine.Object), false, new GUILayoutOption[0]);
        else if (this.srcDirectory.Equals((object)DllSwitcher.DirectoryType.AbsolutePath))
        {
            this.srcDirPath = EditorGUILayout.TextField(this.srcDirPath, new GUILayoutOption[0]);
        }

        EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
        EditorGUILayout.LabelField("Replace Dirctory", new GUILayoutOption[1]
        {
      GUILayout.MaxWidth(140f)
        });
        this.resDirectory = (DllSwitcher.DirectoryType)EditorGUILayout.EnumPopup((Enum)this.resDirectory, new GUILayoutOption[1]
        {
      GUILayout.ExpandWidth(true)
        });
        EditorGUILayout.EndHorizontal();
        if (this.resDirectory.Equals((object)DllSwitcher.DirectoryType.SpecificDirectory))
            this.replaceDir = EditorGUILayout.ObjectField(this.replaceDir, typeof(UnityEngine.Object), false, new GUILayoutOption[0]);
        else if (this.resDirectory.Equals((object)DllSwitcher.DirectoryType.AbsolutePath))
        {
            this.replaceDirPath = EditorGUILayout.TextField(this.replaceDirPath, new GUILayoutOption[0]);
        }

        if (GUILayout.Button("Replace From Dll To Src", new GUILayoutOption[0]))
            this.replaceSriptReference(true);
        if (!GUILayout.Button("Replace From Src To Dll", new GUILayoutOption[0]))
            return;
        this.replaceSriptReference(false);
    }

    public void replaceSriptReference(bool dllToSrc)
    {
        if (this.resDirectory.Equals((object)DllSwitcher.DirectoryType.SpecificDirectory) || this.resDirectory.Equals((object)DllSwitcher.DirectoryType.AbsolutePath))
            this.replaceSriptReferenceOfSelectDirectory(dllToSrc);
        else
            this.replaceSriptReferenceOfAllScripts(dllToSrc);
    }

    public void replaceSriptReferenceOfSelectDirectory(bool dllToSrc = true)
    {
        string assetPath = string.Empty;
        if (this.resDirectory.Equals((object)DllSwitcher.DirectoryType.AbsolutePath))
        {
            assetPath = this.replaceDirPath;
        }
        else
        {
            assetPath = AssetDatabase.GetAssetPath(this.replaceDir);
        }
        this.initFileIDMappingTableOfDll(dllToSrc);
        this.InitGuidMappingTable(dllToSrc);
        this.ReplaceSriptReferenceOfPath(assetPath, dllToSrc);
    }

    public void replaceSriptReferenceOfAllScripts(bool dllToSrc = true)
    {
        this.initFileIDMappingTableOfDll(dllToSrc);
        this.InitGuidMappingTable(dllToSrc);
        this.ReplaceSriptReferenceOfPath(Application.dataPath, dllToSrc);
    }

    private void ReplaceSriptReferenceOfPath(string path, bool dllToSrc = true)
    {
        List<string> allFileWithSuffixs = this.FindAllFileWithSuffixs(path, new string[3]
        {
      ".asset",
      ".prefab",
      ".unity"
        });
        for (int index = 0; index < allFileWithSuffixs.Count; ++index)
        {
            EditorUtility.DisplayProgressBar("Replace Dll", allFileWithSuffixs[index], (float)index * 1f / (float)allFileWithSuffixs.Count);
            this.ReplaceScriptReference(allFileWithSuffixs[index], dllToSrc);
        }
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    private void initFileIDMappingTableOfDll(bool dllToSrc = true)
    {
        if (this.dllInputPath.Equals((object)DllSwitcher.PathType.Reference))
            this.dllFilePath = AssetDatabase.GetAssetPath(this.dllFile);
        this.fileIDMappingTableFromDll = new Dictionary<string, string>();
        Assembly assembly = Assembly.LoadFrom(this.dllFilePath);
        Type[] typeArray;
        try
        {
            typeArray = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            typeArray = ((IEnumerable<Type>)ex.Types).Where<Type>((Func<Type, bool>)(t => t != null)).ToArray<Type>();
            foreach (object loaderException in ex.LoaderExceptions)
                Debug.LogWarning(loaderException);
        }
        int num;
        foreach (Type t in typeArray)
        {
            if (dllToSrc)
            {
                Dictionary<string, string> mappingTableFromDll1 = this.fileIDMappingTableFromDll;
                num = FileIDUtil.Compute(t);
                string key = num.ToString();
                if (mappingTableFromDll1.ContainsKey(key))
                {
                    num = FileIDUtil.Compute(t);
                    Debug.LogWarning((object)("Reduplicated GUID:" + num.ToString() + ";Script Name:" + t.Name));
                }
                else
                {
                    Dictionary<string, string> mappingTableFromDll2 = this.fileIDMappingTableFromDll;
                    num = FileIDUtil.Compute(t);
                    string index = num.ToString();
                    string name = t.Name;
                    mappingTableFromDll2[index] = name;
                }
            }
            else
            {
                Dictionary<string, string> mappingTableFromDll = this.fileIDMappingTableFromDll;
                string name = t.Name;
                num = FileIDUtil.Compute(t);
                string str = num.ToString();
                mappingTableFromDll[name] = str;
            }
        }
        if (dllToSrc)
            return;
        this.initGuidOfDllFile();
    }

    private Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(
      object sender,
      ResolveEventArgs args)
    {
        Debug.LogWarning((object)("Need Loading:" + args.Name));
        return Assembly.ReflectionOnlyLoad(this.dllFilePath.Substring(0, this.dllFilePath.LastIndexOf("\\")) + "\\" + args.Name);
    }

    private void InitGuidMappingTable(bool dllToSrc)
    {
        if (this.srcDirectory.Equals((object)DllSwitcher.DirectoryType.SpecificDirectory) || this.srcDirectory.Equals((object)DllSwitcher.DirectoryType.AbsolutePath))
            this.InitGuidMappingTableOfSelectScripts(dllToSrc);
        else
            this.InitGuidMappingTableOfAllScripts(dllToSrc);
    }

    private void InitGuidMappingTableOfAllScripts(bool dllToSrc = true)
    {
        this.guidMappingTableFromScripts = new Dictionary<string, string>();
        this.InitGuidMappingTableOfPath(Application.dataPath, dllToSrc);
    }

    private void InitGuidMappingTableOfSelectScripts(bool dllToSrc = true)
    {
        this.guidMappingTableFromScripts = new Dictionary<string, string>();
        if (this.srcDirectory.Equals((object)DllSwitcher.DirectoryType.AbsolutePath))
            this.InitGuidMappingTableOfPath(this.srcDirPath, dllToSrc);
        else
            this.InitGuidMappingTableOfPath(AssetDatabase.GetAssetPath(this.srcDir), dllToSrc);
    }

    private void InitGuidMappingTableOfPath(string path, bool dllToSrc = true)
    {
        foreach (string str in this.FindAllFileWithSuffixs(path, new string[2]
        {
      ".cs.meta",
      ".js.meta"
        }))
        {
            if (dllToSrc)
                this.guidMappingTableFromScripts[this.getFileNameFromPath(str)] = this.GetGuidFromMeta(str);
            else
                this.guidMappingTableFromScripts[this.GetGuidFromMeta(str)] = this.getFileNameFromPath(str);
        }
    }

    private void initGuidOfDllFile()
    {
        this.guidOfDllFile = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this.dllFile));
    }

    private void ReplaceScriptReference(string filePath, bool dllToSrc = true)
    {
        Debug.Log((object)("Ready to replace:" + filePath));
        string[] contents = File.ReadAllLines(filePath);
        int index = 0;
        bool flag = false;
        for (; index < contents.Length; ++index)
        {
            if (contents[index].StartsWith("MonoBehaviour:"))
            {
                do
                {
                    ++index;
                }
                while (!contents[index].TrimStart().StartsWith("m_Script:"));
                if (dllToSrc)
                    flag |= this.replaceGUIDAnfFileIDFromDllToSrc(ref contents[index]);
                else
                    flag |= this.replaceGUIDAnfFileIDFromSrcToDll(ref contents[index]);
            }
        }
        if (!flag)
            return;
        File.WriteAllLines(filePath, contents);
    }

    private string GetGuidFromMeta(string filePath)
    {
        string str1 = "";
        using (StreamReader streamReader = new StreamReader(filePath))
        {
            while (!streamReader.EndOfStream)
            {
                string str2 = streamReader.ReadLine();
                if (str2.StartsWith("guid:"))
                {
                    string str3 = str2;
                    str1 = str3.Substring(str3.IndexOf(":") + 2);
                    break;
                }
            }
            streamReader.Close();
        }
        return str1;
    }

    private bool replaceGUIDAnfFileIDFromDllToSrc(ref string lineStr)
    {
        bool flag = false;
        string scriptReferenceLine1 = this.getFileIDFrommScriptReferenceLine(lineStr);
        if (scriptReferenceLine1 == null || scriptReferenceLine1.Equals("11500000"))
            return false;
        string key;
        if (this.fileIDMappingTableFromDll.TryGetValue(scriptReferenceLine1, out key))
        {
            string newValue;
            if (this.guidMappingTableFromScripts.TryGetValue(key, out newValue))
            {
                string scriptReferenceLine2 = this.getGUIDFrommScriptReferenceLine(lineStr);
                Debug.Log((object)("Replacing script reference:" + key));
                ref string local1 = ref lineStr;
                local1 = local1.Replace(scriptReferenceLine1, "11500000");
                ref string local2 = ref lineStr;
                local2 = local2.Replace(scriptReferenceLine2, newValue);
                flag = true;
            }
            else
                Debug.LogWarning((object)("Can't find the GUID of file:" + key));
        }
        else
            Debug.LogWarning((object)("Can't find the file of fileID:" + scriptReferenceLine1));
        return flag;
    }

    private bool replaceGUIDAnfFileIDFromSrcToDll(ref string lineStr)
    {
        bool flag = false;
        string scriptReferenceLine = this.getGUIDFrommScriptReferenceLine(lineStr);
        if (scriptReferenceLine == null)
            return false;
        string key;
        if (this.guidMappingTableFromScripts.TryGetValue(scriptReferenceLine, out key))
        {
            string newValue;
            if (this.fileIDMappingTableFromDll.TryGetValue(key, out newValue))
            {
                Debug.Log((object)("Replacing script reference:" + key));
                ref string local1 = ref lineStr;
                local1 = local1.Replace("11500000", newValue);
                ref string local2 = ref lineStr;
                local2 = local2.Replace(scriptReferenceLine, this.guidOfDllFile);
                flag = true;
            }
            else
                Debug.LogWarning((object)("Can't find the GUID of file:" + key));
        }
        else
            Debug.LogWarning((object)("Can't find the file of GUID:" + scriptReferenceLine));
        return flag;
    }

    private string getFileIDFrommScriptReferenceLine(string lineStr)
    {
        int startIndex = lineStr.IndexOf("fileID:") + "fileID: ".Length;
        int length = lineStr.IndexOf(",") - startIndex;
        return length <= 0 ? (string)null : lineStr.Substring(startIndex, length);
    }

    private string getGUIDFrommScriptReferenceLine(string lineStr)
    {
        int startIndex = lineStr.IndexOf("guid:") + "guid: ".Length;
        int length = lineStr.LastIndexOf(",") - startIndex;
        return length <= 0 ? (string)null : lineStr.Substring(startIndex, length);
    }

    private string getFileNameFromPath(string path)
    {
        string str1 = path;
        string str2 = str1.Substring(str1.LastIndexOf("\\") + 1);
        return str2.Substring(0, str2.IndexOf("."));
    }

    private List<string> FindAllFileWithSuffixs(string path, string[] suffixs)
    {
        List<string> resultList = new List<string>();
        this.FindAllFileWithSuffixs(path, suffixs, ref resultList);
        return resultList;
    }

    private void FindAllFileWithSuffixs(string path, string[] suffixs, ref List<string> resultList)
    {
        if (File.Exists(path))
        {
            resultList.Add(path);
        }
        else
        {
            if (string.IsNullOrEmpty(path))
                return;
            foreach (string file in Directory.GetFiles(path))
            {
                foreach (string str in suffixs)
                {
                    if (file.EndsWith(str))
                    {
                        resultList.Add(file);
                        break;
                    }
                }
            }
            foreach (string directory in Directory.GetDirectories(path))
                this.FindAllFileWithSuffixs(directory, suffixs, ref resultList);
        }
    }

    private void WriteDebugFile(string[] lines, string filename)
    {
    }

    public DllSwitcher()
    {
        //base.\u002Ector();
        base.Show();
    }

    private enum PathType
    {
        Reference,
        AbsolutePath,
    }

    private enum DirectoryType
    {
        Root,
        SpecificDirectory,
        AbsolutePath,
    }
}