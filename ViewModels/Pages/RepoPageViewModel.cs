﻿using Avalonia.Controls;
using KitX.Web.Rules;
using KitX_Dashboard.Commands;
using KitX_Dashboard.Managers;
using KitX_Dashboard.Models;
using KitX_Dashboard.Services;
using KitX_Dashboard.Views.Pages.Controls;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace KitX_Dashboard.ViewModels.Pages;

internal class RepoPageViewModel : ViewModelBase, INotifyPropertyChanged
{
    public RepoPageViewModel()
    {

        InitEvents();

        InitCommands();

        SearchingText = "";
        PluginsCount = PluginBars.Count.ToString();

        PluginBars.CollectionChanged += (_, _) =>
        {
            PluginsCount = PluginBars.Count.ToString();
            NoPlugins_TipHeight = PluginBars.Count == 0 ? 300 : 0;
        };

        RefreshPlugins(new());
    }

    /// <summary>
    /// 初始化事件
    /// </summary>
    private void InitEvents()
    {
        EventService.ConfigSettingsChanged += () =>
        {
            ImportButtonVisibility = ConfigManager.AppConfig.App.DeveloperSetting;
        };
    }

    /// <summary>
    /// 初始化命令
    /// </summary>
    private void InitCommands()
    {
        ImportPluginCommand = new(ImportPlugin);
        RefreshPluginsCommand = new(RefreshPlugins);
    }

    internal string SearchingText { get; set; }

    internal string pluginsCount = "0";

    internal string PluginsCount
    {
        get => pluginsCount;
        set
        {
            pluginsCount = value;
            PropertyChanged?.Invoke(this, new(nameof(PluginsCount)));
        }
    }

    internal double noPlugins_tipHeight = 300;

    internal double NoPlugins_TipHeight
    {
        get => noPlugins_tipHeight;
        set
        {
            noPlugins_tipHeight = value;
            PropertyChanged?.Invoke(this, new(nameof(NoPlugins_TipHeight)));
        }
    }

    internal bool ImportButtonVisibility
    {
        get => ConfigManager.AppConfig.App.DeveloperSetting;
        set
        {
            ConfigManager.AppConfig.App.DeveloperSetting = value;
            PropertyChanged?.Invoke(this, new(nameof(ImportButtonVisibility)));
        }
    }

    internal ObservableCollection<PluginBar> pluginBars = new();

    internal ObservableCollection<PluginBar> PluginBars
    {
        get => pluginBars;
        set => pluginBars = value;
    }

    internal DelegateCommand? ImportPluginCommand { get; set; }

    internal DelegateCommand? RefreshPluginsCommand { get; set; }

    /// <summary>
    /// 导入插件
    /// </summary>
    /// <param name="_"></param>
    internal async void ImportPlugin(object? win)
    {
        if (win is not Window window) return;

        var ofd = new OpenFileDialog()
        {
            AllowMultiple = true,
        };

        ofd.Filters?.Add(new()
        {
            Name = "KitX Extensions Packages",
            Extensions = { "kxp" }
        });

        var files = await ofd.ShowAsync(window);

        if (files is not null && files?.Length > 0)
        {
            new Thread(() =>
            {
                try
                {
                    PluginsManager.ImportPlugin(files, true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "In RepoPageViewModel.ImportPlugin()");
                }
            }).Start();
        }
    }

    /// <summary>
    /// 刷新插件
    /// </summary>
    /// <param name="_"></param>
    internal void RefreshPlugins(object? _)
    {
        //LiteDatabase? pgdb = Program.PluginsDataBase;

        PluginBars.Clear();
        lock (PluginsNetwork.PluginsListOperationLock)
        {
            foreach (var item in PluginsManager.Plugins)
            {
                try
                {
                    Plugin plugin = new()
                    {
                        InstallPath = item.InstallPath,
                        PluginDetails = JsonSerializer.Deserialize<PluginStruct>(
                            File.ReadAllText(
                                Path.GetFullPath($"{item.InstallPath}/PluginStruct.json"))),
                        RequiredLoaderStruct = JsonSerializer.Deserialize<LoaderStruct>(
                            File.ReadAllText(
                                Path.GetFullPath($"{item.InstallPath}/LoaderStruct.json"))),
                        InstalledDevices = new()
                    };
                    PluginBars.Add(new(plugin, ref pluginBars));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "In RefreshPlugins()");
                }
            }
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
}

//
//            ~                  ~
//      *                   *                *       *
//                   *               *
//   ~       *                *         ~    *          
//               *       ~        *              *   ~
//                   )         (         )              *
//     *    ~     ) (_)   (   (_)   )   (_) (  *
//            *  (_) # ) (_) ) # ( (_) ( # (_)       *
//               _#.-#(_)-#-(_)#(_)-#-(_)#-.#_    
//   *         .' #  # #  #  # # #  #  # #  # `.   ~     *
//            :   #    #  #  #   #  #  #    #   :   
//     ~      :.       #     #   #     #       .:      *
//         *  | `-.__                     __.-' | *
//            |      `````"""""""""""`````      |         *
//      *     |         | ||\ |~)|~)\ /         |    
//            |         |~||~\|~ |~  |          |       ~
//    ~   *   |                                 | * 
//            |      |~)||~)~|~| ||~\|\ \ /     |         *
//    *    _.-|      |~)||~\ | |~|| /|~\ |      |-._  
//       .'   '.      ~            ~           .'   `.  *
//  1117 :      `-.__                     __.-'      :
//        `.         `````"""""""""""`````         .'
//          `-.._                             _..-'
//               `````""""-----------""""`````
// 
//
