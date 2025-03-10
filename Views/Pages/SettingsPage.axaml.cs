﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using KitX_Dashboard.Managers;
using KitX_Dashboard.Services;
using KitX_Dashboard.ViewModels.Pages;
using KitX_Dashboard.Views.Pages.Controls;
using Serilog;
using System;

namespace KitX_Dashboard.Views.Pages;

public partial class SettingsPage : UserControl
{
    private readonly SettingsPageViewModel viewModel = new();

    public SettingsPage()
    {
        InitializeComponent();

        DataContext = viewModel;

        InitSettingsPage();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 初始化设置页面
    /// </summary>
    private void InitSettingsPage()
    {
        this.FindControl<NavigationView>("SettingsNavigationView").SelectedItem
            = this.FindControl<NavigationViewItem>(SelectedViewName);
    }

    /// <summary>
    /// 保存对配置文件的修改
    /// </summary>
    private static void SaveChanges()
    {
        EventService.Invoke(nameof(EventService.ConfigSettingsChanged));
    }

    /// <summary>
    /// 前台页面切换事件
    /// </summary>
    /// <param name="sender">被点击的 NavigationViewItem</param>
    /// <param name="e">路由事件参数</param>
    private void SettingsNavigationView_SelectionChanged(
        object? sender,
        NavigationViewSelectionChangedEventArgs e)
    {
        try
        {
            var tag = ((sender as NavigationView)?.SelectedItem as Control)?.Tag?.ToString();

            if (tag is null) return;

            SelectedViewName = tag;

            this.FindControl<Frame>("SettingsFrame").Navigate(SelectedViewType());
        }
        catch (NullReferenceException o)
        {
            Log.Warning(o, o.Message);
        }
    }

    private static string SelectedViewName
    {
        get => ConfigManager.AppConfig.Pages.Settings.SelectedViewName;
        set
        {
            ConfigManager.AppConfig.Pages.Settings.SelectedViewName = value;
            SaveChanges();
        }
    }

    private static Type SelectedViewType() => SelectedViewName switch
    {
        "View_General" => typeof(Settings_General),
        "View_Personalise" => typeof(Settings_Personalise),
        "View_Performence" => typeof(Settings_Performence),
        "View_Update" => typeof(Settings_Update),
        "View_About" => typeof(Settings_About),
        _ => typeof(Settings_General),
    };
}

//
//      _.-^^---....,,--
//  _--                  --_
// &lt;                        &gt;)
// |                         |
//  \._                   _./
//     ```--. . , ; .--'''
//           | |   |
//        .-=||  | |=-.
//        `-=#$%&amp;%$#=-'
//           | ;  :|
//  _____.,-#%&amp;$@%#~,._____
//
