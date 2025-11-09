using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CheckUpdate : MonoBehaviour
{
    private class DownLoadInfo
    {
        public List<string> CatalogKeys = new List<string>();
    }
    private DownLoadInfo _downLoadInfo = new DownLoadInfo();
    
    private List<object> _catalogKeys = new List<object>();
    
    private void Start()
    {
        DontDestroyOnLoad(this);
        StartCoroutine(Check());
    }

    private IEnumerator Check()
    {
        //补充元数据
        yield return LoadMetaData();
        //检查目录，更新目录
        yield return CheckAssetUpdate();
        //下载资源
        yield return DownloadAssets();
        //加载程序集
        yield return LoadAssembly();
        //加载场景1
        yield return LoadScene("EntryScene");
        //加载场景2
        yield return LoadScene("GameScene");
        //生成player
        yield return LoadSphere();

    }

    private IEnumerator CheckAssetUpdate()
    {
        AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("检查目录成功！");
            List<string> updatedCatalogKeys = checkHandle.Result;

            // 先检查本地是否有未完成的下载
            if (PlayerPrefs.HasKey("DownLoadKeys"))
            {
                string jsonStr = PlayerPrefs.GetString("DownLoadKeys");
                DownLoadInfo savedInfo = JsonUtility.FromJson<DownLoadInfo>(jsonStr);
                if (savedInfo?.CatalogKeys?.Count > 0)
                {
                    _downLoadInfo.CatalogKeys = savedInfo.CatalogKeys;
                    Debug.Log("检测到未完成的下载，继续下载...");
                }
            }

            // 如果有新的更新，优先使用新的
            if (updatedCatalogKeys?.Count > 0)
            {
                _downLoadInfo.CatalogKeys = updatedCatalogKeys;
                // 保存新的下载信息
                string jsonStr = JsonUtility.ToJson(_downLoadInfo);
                PlayerPrefs.SetString("DownLoadKeys", jsonStr);
                PlayerPrefs.Save();
                Debug.Log($"发现 {updatedCatalogKeys.Count} 个目录需要更新");
            }

            // 执行目录更新
            if (_downLoadInfo.CatalogKeys?.Count > 0)
            {
                AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs(_downLoadInfo.CatalogKeys, false);
                yield return updateHandle;

                if (updateHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("目录更新成功！");
                    List<IResourceLocator> locators = updateHandle.Result;
                    _catalogKeys.Clear();
                    foreach (IResourceLocator locator in locators)
                    {
                        _catalogKeys.AddRange(locator.Keys);
                    }
                }
                else
                {
                    Debug.LogError("目录更新失败！");
                }

                Addressables.Release(updateHandle);
            }
            else
            {
                Debug.Log("没有目录需要更新");
            }
        }
        else
        {
            Debug.LogError("检查目录失败！");
        }
        Addressables.Release(checkHandle);
    }
    private IEnumerator DownloadAssets()
    {
        if (_catalogKeys.Count == 0)
        {
            Debug.Log("没有资源需要下载");
            // 清理可能存在的旧数据
            if (PlayerPrefs.HasKey("DownLoadKeys"))
            {
                PlayerPrefs.DeleteKey("DownLoadKeys");
                PlayerPrefs.Save();
            }
            yield break;
        }
        AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)_catalogKeys);
        yield return sizeHandle;

        long size = sizeHandle.Result;
        if (size > 0)
        {
            Debug.Log($"需要下载资源大小: {size} bytes");

            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync((IEnumerable)_catalogKeys, Addressables.MergeMode.Union);
            yield return downloadHandle;

            if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("下载成功！");
                // 下载完成后清理记录
                if (PlayerPrefs.HasKey("DownLoadKeys"))
                {
                    PlayerPrefs.DeleteKey("DownLoadKeys");
                    PlayerPrefs.Save();
                }
            }
            else
            {
                Debug.LogError("下载失败！");
                // 下载失败时保留记录，下次继续
            }

            Addressables.Release(downloadHandle);
        }
        else
        {
            Debug.Log("没有资源需要下载！");
            // 清理记录
            if (PlayerPrefs.HasKey("DownLoadKeys"))
            {
                PlayerPrefs.DeleteKey("DownLoadKeys");
                PlayerPrefs.Save();
            }
        }
        Addressables.Release(sizeHandle);
    }
    private IEnumerator LoadMetaData()
    {
        foreach (var str in AOTGenericReferences.PatchedAOTAssemblyList)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(str);
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                byte[] meta = handle.Result.bytes;
                RuntimeApi.LoadMetadataForAOTAssembly(meta, HomologousImageMode.SuperSet);
                Debug.Log("下载元数据成功！");
            }
        }
    }
    private IEnumerator LoadAssembly()
    {
        string lable = "Assembly";
        AsyncOperationHandle<IList<TextAsset>> handle = Addressables.LoadAssetsAsync<TextAsset>(lable,null);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            IList<TextAsset> assets = handle.Result;
            foreach (var asset in assets)
            {
                Debug.Log(asset.name);
                var dll = asset.bytes;
                Assembly.Load(dll);
            }
        }
    }
    private IEnumerator LoadScene(string sceneName)
    {
        var handle = Addressables.LoadSceneAsync(sceneName);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("加载场景成功！");
            yield return new WaitForSeconds(2f);
            yield break;
        }
        Debug.Log("加载场景失败！");
    }
    private IEnumerator LoadSphere()
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("Sphere");
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject obj = handle.Result;
            GameObject obj2 = Instantiate(obj,new Vector3(0,3,0),Quaternion.identity);
        }
    }
}
