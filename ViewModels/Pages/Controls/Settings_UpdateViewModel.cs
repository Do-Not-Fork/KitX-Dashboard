﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Common.Update.Checker;
using KitX.Web.Rules;
using KitX_Dashboard.Commands;
using KitX_Dashboard.Converters;
using KitX_Dashboard.Data;
using KitX_Dashboard.Managers;
using KitX_Dashboard.Network;
using KitX_Dashboard.Services;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Component = KitX_Dashboard.Models.Component;
using Timer = System.Timers.Timer;

namespace KitX_Dashboard.ViewModels.Pages.Controls;

internal class Settings_UpdateViewModel : ViewModelBase, INotifyPropertyChanged
{
    private bool _canUpdateDataGridView = true;

    internal Settings_UpdateViewModel()
    {
        InitEvents();

        InitCommands();
    }

    /// <summary>
    /// 初始化事件
    /// </summary>
    internal void InitEvents()
    {
        Components.CollectionChanged += (_, _) =>
        {
            if (_canUpdateDataGridView)
            {
                CanUpdateCount = Components.Count(x => x.CanUpdate);
                PropertyChanged?.Invoke(this, new(nameof(ComponentsCount)));
            }
        };
    }

    /// <summary>
    /// 初始化命令
    /// </summary>
    internal void InitCommands()
    {
        CheckUpdateCommand = new(CheckUpdate);
    }

    internal int canUpdateCount = 0;

    /// <summary>
    /// 可以更新的插件数量
    /// </summary>
    internal int CanUpdateCount
    {
        get => canUpdateCount;
        set
        {
            canUpdateCount = value;
            PropertyChanged?.Invoke(this, new(nameof(CanUpdateCount)));
        }
    }

    /// <summary>
    /// 找到的组件数量
    /// </summary>
    internal static int ComponentsCount { get => Components.Count; }

    internal static ObservableCollection<Component> Components { get; } = new();

    internal string? tip = string.Empty;

    /// <summary>
    /// 进度提示
    /// </summary>
    internal string? Tip
    {
        get => tip;
        set
        {
            tip = value;
            PropertyChanged?.Invoke(this, new(nameof(Tip)));
        }
    }

    internal string diskUseStatus = string.Empty;

    /// <summary>
    /// 磁盘使用情况
    /// </summary>
    internal string DiskUseStatus
    {
        get => diskUseStatus;
        set
        {
            diskUseStatus = value;
            PropertyChanged?.Invoke(this, new(nameof(DiskUseStatus)));
        }
    }

    /// <summary>
    /// 更新频道
    /// </summary>
    public static int UpdateChannel
    {
        get => ConfigManager.AppConfig.Web.UpdateChannel switch
        {
            "stable" => 0,
            "beta" => 1,
            "alpha" => 2,
            _ => 0
        };
        set
        {
            ConfigManager.AppConfig.Web.UpdateChannel = value switch
            {
                0 => "stable",
                1 => "beta",
                2 => "alpha",
                _ => "stable"
            };
            EventService.Invoke(nameof(EventService.ConfigSettingsChanged));
        }
    }

    /// <summary>
    /// 启用或禁用检查更新命令
    /// </summary>
    /// <param name="enable">启用或禁用</param>
    private void AbleCheckUpdateCommand(bool enable)
    {
        CheckUpdateCommand = enable ? new(CheckUpdate) : null;
        PropertyChanged?.Invoke(this, new(nameof(CheckUpdateCommand)));
    }

    /// <summary>
    /// 启用或禁用更新命令
    /// </summary>
    /// <param name="enable">启用或禁用</param>
    private void AbleUpdateCommand(bool enable)
    {
        UpdateCommand = enable ? new(Update) : null;
        PropertyChanged?.Invoke(this, new(nameof(UpdateCommand)));
    }

    /// <summary>
    /// 获取不同语言的提示
    /// </summary>
    /// <param name="key">值</param>
    /// <returns>提示</returns>
    private static string GetResources(string key)
    {
        if (Application.Current is null) return string.Empty;

        _ = Application.Current.TryFindResource(key, out var obj);

        var result = obj as string;

        return result is not null ? result : string.Empty;
    }

    /// <summary>
    /// 获取更新提示
    /// </summary>
    /// <param name="key">提示键</param>
    /// <returns>提示值</returns>
    private static string GetUpdateTip(string key) => GetResources($"Text_Settings_Update_Tip_{key}");

    /// <summary>
    /// 下载新组件
    /// </summary>
    /// <param name="url">下载 URL</param>
    /// <param name="to">下载到</param>
    /// <param name="client">Http 客户端</param>
    private static async void DownloadNewComponent(string url, string to, HttpClient client)
    {
        try
        {
            byte[] bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(to, bytes);
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
        }
    }

    /// <summary>
    /// 检查更新命令
    /// </summary>
    internal DelegateCommand? CheckUpdateCommand { get; set; }

    /// <summary>
    /// 更新命令
    /// </summary>
    internal DelegateCommand? UpdateCommand { get; set; }

    private void CheckUpdate(object? _)
    {
        try
        {
            CheckUpdate();
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
        }
    }

    /// <summary>
    /// 获取大小
    /// </summary>
    /// <param name="size">字节数</param>
    /// <returns>大小表示</returns>
    private static string GetDisplaySize(long size)
    {
        if (size / (1024 * 1024) > 2000) return $"{size / (1024 * 1024 * 1024)} GB";
        else if (size / 1024 > 1200) return $"{size / (1024 * 1024)} MB";
        else return $"{size / 1024} KB";
    }

    /// <summary>
    /// 扫描组件
    /// </summary>
    /// <param name="workbase">工作路径</param>
    /// <returns>检查器</returns>
    private Checker ScanComponents(string workbase)
    {
        var wd = workbase;
        var ld = Path.GetFullPath(GlobalInfo.LanguageFilePath);

        Checker checker = new Checker()
            .SetRootDirectory(wd)
            .SetPerThreadFilesCount(ConfigManager.AppConfig.IO.UpdatingCheckPerThreadFilesCount)
            .SetTransHash2String(true)
            .AppendIgnoreFolder("AppConfig")
            .AppendIgnoreFolder("Core")
            .AppendIgnoreFolder("Data")
            .AppendIgnoreFolder("Languages")
            .AppendIgnoreFolder("Log")
            .AppendIgnoreFolder("Update")
            .AppendIgnoreFolder(ConfigManager.AppConfig.App.LocalPluginsFileFolder)
            .AppendIgnoreFolder(ConfigManager.AppConfig.App.LocalPluginsDataFolder);
        foreach (var item in ConfigManager.AppConfig.App.SurpportLanguages)
            _ = checker.AppendIncludeFile($"{ld}/{item.Key}.axaml");
        Tip = GetUpdateTip("Scan");
        checker.Scan();
        return checker;
    }

    /// <summary>
    /// 计算组件哈希值
    /// </summary>
    /// <param name="checker">检查器</param>
    private void CalculateComponentsHash(Checker checker)
    {
        bool _calculateFinished = false;
        Timer timer = new()
        {
            Interval = 10,
            AutoReset = true
        };
        timer.Elapsed += (_, _) =>
        {
            try
            {
                var progress = checker.GetProgress();
                Tip = GetUpdateTip("Calculate")
                    .Replace("%Progress%",
                        $"({progress.Item1}/{progress.Item2})");
                if (_calculateFinished) timer.Stop();
            }
            catch (Exception e)
            {
                var location = $"{nameof(Settings_UpdateViewModel)}.{nameof(CheckUpdate)}";
                Log.Warning(e, $"In {location}: {e.Message}");
            }
        };
        timer.Start();

        checker.Calculate();
        _calculateFinished = true;
    }

    /// <summary>
    /// 异步获取最新组件列表
    /// </summary>
    /// <returns>最新的组件列表字典</returns>
    private static async Task<Dictionary<string, (string, string, long)>?> GetLatestComponentsAsync
    (
        HttpClient client
    )
    {
        #region 获取最新的组件列表

        client.DefaultRequestHeaders.Accept.Clear();    //  清除请求头部
        string link = "https://" +
ConfigManager.AppConfig.Web.UpdateServer +
ConfigManager.AppConfig.Web.UpdatePath.Replace("%platform%",
                DevicesDiscoveryServer.DefaultDeviceInfoStruct.DeviceOSType switch
                {
                    OperatingSystems.Windows => "win",
                    OperatingSystems.Linux => "linux",
                    OperatingSystems.MacOS => "mac",
                    _ => ""
                }) +
            $"{ConfigManager.AppConfig.Web.UpdateChannel}/" +
ConfigManager.AppConfig.Web.UpdateSource;
        string json = await client.GetStringAsync(link);

        #endregion 获取最新的组件列表

        #region 反序列化最新的组件列表到字典

        JsonSerializerOptions option = new()
        {
            WriteIndented = true,
            IncludeFields = true,
            PropertyNamingPolicy = new UpdateHashNamePolicy(),
        };
        var latestComponents =
            JsonSerializer
                .Deserialize<Dictionary<string, (string, string, long)>>(json, option);

        #endregion 反序列化最新的组件列表到字典

        return latestComponents;
    }

    /// <summary>
    /// 添加本地组件到界面
    /// </summary>
    /// <param name="result">上文组件计算结果</param>
    /// <param name="wd">工作路径</param>
    /// <param name="localComponentsTotalSize">本地组件总体积</param>
    private void AddLocalComponentsToView
    (
        Dictionary<string, (string, string)> result,
        string wd,
        ref long localComponentsTotalSize
    )
    {
        List<Component> localComponents = new();
        foreach (var item in result)
        {
            long size = new FileInfo(Path.GetFullPath($"{wd}/{item.Key}")).Length;
            localComponentsTotalSize += size;
            localComponents.Add(new()
            {
                CanUpdate = false,
                Name = item.Key,
                MD5 = item.Value.Item1.ToUpper(),
                SHA1 = item.Value.Item2.ToUpper(),
                Size = GetDisplaySize(size),
            });
        }
        Dispatcher.UIThread.Post(() =>
        {
            int index = 0;
            foreach (var item in localComponents)
            {
                ++index;
                if (index == localComponents.Count) _canUpdateDataGridView = true;
                Components.Add(item);
            }
        });

        Tip = GetUpdateTip("Compare");
        while (Components.Count != result.Count) { }    //  阻塞直到前台加载完毕
    }

    /// <summary>
    /// 对比有变更的, 新增的, 减少的文件
    /// </summary>
    /// <param name="wd"></param>
    /// <param name="latestComponents"></param>
    /// <param name="result"></param>
    /// <param name="latestComponentsTotalSize"></param>
    /// <returns>object (updatedComponents, new2addComponents, tdeleteComponents)</returns>
    private static object CompareDifferentComponents
    (
        ref string wd,
        ref Dictionary<string, (string, string, long)> latestComponents,
        ref Dictionary<string, (string, string)> result,
        ref long latestComponentsTotalSize
    )
    {
        Dictionary<string, long> updatedComponents = new();
        Dictionary<string, long> new2addComponents = new();
        Dictionary<string, long> tdeleteComponents = new();
        foreach (var component in latestComponents)
        {
            if (result.ContainsKey(component.Key))
            {
                var current = result[component.Key];
                if (!current.Item1.ToUpper().Equals(component.Value.Item1.ToUpper()) ||
                    !current.Item2.ToUpper().Equals(component.Value.Item2.ToUpper()))
                    updatedComponents.Add(component.Key, component.Value.Item3);
            }
            else
            {
                new2addComponents.Add(component.Key, component.Value.Item3);
            }
            latestComponentsTotalSize += component.Value.Item3;
        }
        foreach (var item in result)
            if (!latestComponents.ContainsKey(item.Key))
                tdeleteComponents
                    .Add(item.Key,
                        new FileInfo(
                                Path.GetFullPath($"{wd}/{item.Key}")
                        ).Length
                    );

        return (updatedComponents, new2addComponents, tdeleteComponents);
    }

    /// <summary>
    /// 更新前端视图
    /// </summary>
    /// <param name="updatedComponents">已更新的组件</param>
    /// <param name="new2addComponents">待增加的组件</param>
    /// <param name="tdeleteComponents">待删除的组件</param>
    /// <param name="latestComponents">最新的组件列表</param>
    private void UpdateFrontendViewAfterCompare
    (
        Dictionary<string, long> updatedComponents,
        Dictionary<string, long> new2addComponents,
        Dictionary<string, long> tdeleteComponents,
        ref Dictionary<string, (string, string, long)> latestComponents,
        ref long localComponentsTotalSize,
        ref long latestComponentsTotalSize,
        ref Dictionary<string, (string, string)> result
    )
    {
        _canUpdateDataGridView = false;

        foreach (var item in Components)
        {
            if (item.Name is null) continue;

            if (updatedComponents.ContainsKey(item.Name))
            {
                item.CanUpdate = true;
                item.Task = GetResources("Text_Public_Replace");
            }
            else if (tdeleteComponents.ContainsKey(item.Name))
            {
                item.CanUpdate = true;
                item.Task = GetResources("Text_Public_Delete");
            }
        }

        List<Component> newComponents = new();
        foreach (var item in new2addComponents)
        {
            newComponents.Add(new Component()
            {
                Name = item.Key,
                CanUpdate = true,
                MD5 = latestComponents[item.Key].Item1,
                SHA1 = latestComponents[item.Key].Item2,
                Task = GetResources("Text_Public_Add"),
                Size = GetDisplaySize(item.Value)
            });
        }
        Dispatcher.UIThread.Post(() =>
        {
            int index = 0;
            foreach (var item in newComponents)
            {
                ++index;
                if (index == new2addComponents.Count) _canUpdateDataGridView = true;
                Components.Add(item);
            }
        });

        if (new2addComponents.Count == 0)
        {
            CanUpdateCount = Components.Count(x => x.CanUpdate);
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(Components)));
        }

        while (Components.Count != result.Count + new2addComponents.Count) { }

        DiskUseStatus =
            localComponentsTotalSize > latestComponentsTotalSize
            ?
            $"- {GetDisplaySize(localComponentsTotalSize - latestComponentsTotalSize)}"
            :
            $"+ {GetDisplaySize(latestComponentsTotalSize - localComponentsTotalSize)}"
        ;
    }

    /// <summary>
    /// 下载新组件
    /// </summary>
    /// <param name="updatedComponents">有更新的组件</param>
    /// <param name="client">网络请求 http 客户端</param>
    private void DownloadNewComponents
    (
        ref Dictionary<string, long> updatedComponents,
        ref HttpClient client
    )
    {
        Tip = GetUpdateTip("Download");

        //TODO: 下载有变更的文件
        string downloadLinkBase = "https://" +
ConfigManager.AppConfig.Web.UpdateServer +
ConfigManager.AppConfig.Web.UpdateDownloadPath.Replace("%platform%",
            DevicesDiscoveryServer.DefaultDeviceInfoStruct.DeviceOSType switch
            {
                OperatingSystems.Windows => "win",
                OperatingSystems.Linux => "linux",
                OperatingSystems.MacOS => "mac",
                _ => ""
            }) +
        $"{ConfigManager.AppConfig.Web.UpdateChannel}/";
        if (!Directory.Exists(Path.GetFullPath(GlobalInfo.UpdateSavePath)))
            Directory.CreateDirectory(Path.GetFullPath(GlobalInfo.UpdateSavePath));
        foreach (var item in updatedComponents)
        {
            DownloadNewComponent($"{downloadLinkBase}{item.Key.Replace(@"\", "/")}",
                Path.GetFullPath($"{GlobalInfo.UpdateSavePath}{item}"), client);
        }
    }

    /// <summary>
    /// 检查更新主流程
    /// </summary>
    private void CheckUpdate()
    {
        #region 更新前准备工作

        Components.Clear();
        AbleUpdateCommand(false);
        AbleCheckUpdateCommand(false);
        _canUpdateDataGridView = false;
        Tip = GetUpdateTip("Start");

        #endregion 更新前准备工作

        new Thread(async () =>
        {
            try
            {
                string? wd = Path.GetFullPath("./");

                if (wd is not null)
                {
                    var checker = ScanComponents(wd);

                    CalculateComponentsHash(checker);

                    var result = checker.GetCalculateResult()
                    .OrderBy(
                        x =>
                        x.Key.Contains('/') || x.Key.Contains('\\') ? $"0{x.Key}" : x.Key
                    ).ToDictionary(
                        x => x.Key,
                        x => x.Value
                    );

                    checker.CleanMemoryUsage();

                    long localComponentsTotalSize = 0;
                    long latestComponentsTotalSize = 0;

                    AddLocalComponentsToView(result, wd, ref localComponentsTotalSize);

                    HttpClient client = new();  //  Http客户端

                    var latestComponents
                        = await GetLatestComponentsAsync(client);

                    if (latestComponents is not null)
                    {
                        var difference =
                        ((
                            Dictionary<string, long>,
                            Dictionary<string, long>,
                            Dictionary<string, long>
                        ))
                        CompareDifferentComponents
                        (
                            ref wd, ref latestComponents, ref result,
                            ref latestComponentsTotalSize
                        );

                        var updatedComponents = difference.Item1;
                        var new2addComponents = difference.Item2;
                        var tdeleteComponents = difference.Item3;

                        UpdateFrontendViewAfterCompare
                        (
                            updatedComponents,
                            new2addComponents,
                            tdeleteComponents,
                            ref latestComponents,
                            ref localComponentsTotalSize,
                            ref latestComponentsTotalSize,
                            ref result
                        );

                        DownloadNewComponents(ref updatedComponents, ref client);

                        Tip = GetUpdateTip("Prepared");

                        if (CanUpdateCount > 0) AbleUpdateCommand(true);
                    }
                    else
                    {
                    }

                    client.Dispose();
                }
                else
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow("Error",
                            "Can't get working directory!", ButtonEnum.Ok,
                            Icon.Warning)
                            .Show();
                    });
                }

                AbleCheckUpdateCommand(true);
                GC.Collect();
            }
            catch (Exception e)
            {
                Tip = GetUpdateTip("Failed");
                AbleUpdateCommand(false);
                Dispatcher.UIThread.Post(async () =>
                {
                    await MessageBoxManager.GetMessageBoxStandardWindow(GetUpdateTip("Failed"),
                        e.Message, ButtonEnum.Ok, Icon.Error).Show();
                });
                Log.Error(e, $"{e.Message}");
            }
        }).Start();
    }

    private void Update(object? _)
    {
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
}

//                         ,  o ' .
//                        .  -  -  o
//                        o . _ ` '  ` ; .
//       ________________________________  H___
//      P                           MK'98| H ..\
//      |                         _______| H |_\\
//      |                        |.========H -  |
//      B_,-._,-._______________/ |,-._,-._H_,-.)
//      --`-'-`-'------------------`-'-`-'---`-'--MK
