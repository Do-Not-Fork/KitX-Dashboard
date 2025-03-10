﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Common.BasicHelper.IO;
using Common.BasicHelper.Utils.Extensions;
using FluentAvalonia.Styling;
using KitX_Dashboard.Data;
using KitX_Dashboard.Managers;
using KitX_Dashboard.Services;
using KitX_Dashboard.ViewModels;
using KitX_Dashboard.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;
using System;
using System.Linq;
using System.Threading;

namespace KitX_Dashboard;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        LoadLanguage();

        CalculateThemeColor();

        InitLiveCharts();
    }

    /// <summary>
    /// 加载语言
    /// </summary>
    private void LoadLanguage()
    {
        var lang = ConfigManager.AppConfig.App.AppLanguage;
        var backup_lang = ConfigManager.AppConfig.App.SurpportLanguages.Keys.First();
        var path = $"{GlobalInfo.LanguageFilePath}/{lang}.axaml".GetFullPath();
        var backup_langPath = $"{GlobalInfo.LanguageFilePath}/{backup_lang}.axaml";

        backup_langPath = backup_langPath.GetFullPath();

        try
        {
            Resources.MergedDictionaries.Clear();

            Resources.MergedDictionaries.Add(
                AvaloniaRuntimeXamlLoader.Load(
                    FileHelper.ReadAll(path)
                ) as ResourceDictionary ?? new()
            );
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Language File {lang}.axaml not found.");

            Resources.MergedDictionaries.Clear();

            try
            {
                Resources.MergedDictionaries.Add(
                    AvaloniaRuntimeXamlLoader.Load(
                        FileHelper.ReadAll(backup_langPath)
                    ) as ResourceDictionary ?? new()
                );
                ConfigManager.AppConfig.App.AppLanguage = backup_lang;
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Suspected absence of language files on record.");
            }
        }
        finally
        {
            Log.Warning($"No surpport language file loaded.");
        }

        try
        {
            EventService.Invoke(nameof(EventService.LanguageChanged));
        }
        catch (Exception e)
        {
            Log.Warning(e, $"Failed to invoke language changed event.");
        }
    }

    /// <summary>
    /// 计算主题色
    /// </summary>
    private static void CalculateThemeColor()
    {
        Color c = Color.Parse(ConfigManager.AppConfig.App.ThemeColor);

        if (Current is not null)
        {
            Current.Resources["ThemePrimaryAccent"] =
                new SolidColorBrush(new Color(c.A, c.R, c.G, c.B));
            for (char i = 'A'; i <= 'E'; ++i)
            {
                Current.Resources[$"ThemePrimaryAccentTransparent{i}{i}"] =
                    new SolidColorBrush(new Color((byte)(170 + (i - 'A') * 17), c.R, c.G, c.B));
            }
            for (int i = 1; i <= 9; ++i)
            {
                Current.Resources[$"ThemePrimaryAccentTransparent{i}{i}"] =
                    new SolidColorBrush(new Color((byte)(i * 10 + i), c.R, c.G, c.B));
            }
        }
    }

    /// <summary>
    /// 初始化图表系统
    /// </summary>
    private static void InitLiveCharts()
    {
        LiveCharts.Configure(config =>
        {
            config
                .AddSkiaSharp()
                .AddDefaultMappers();

            switch (AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>()?.RequestedTheme)
            {
                case "Light": config.AddLightTheme(); break;
                case "Dark": config.AddDarkTheme(); break;
                default: config.AddLightTheme(); break;
            };
        });

        EventService.ThemeConfigChanged += () =>
        {
            switch (AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>()?.RequestedTheme)
            {
                case "Light":
                    LiveCharts.Configure(config => config.AddLightTheme());
                    break;
                case "Dark":
                    LiveCharts.Configure(config => config.AddDarkTheme());
                    break;
                default:
                    LiveCharts.Configure(config => config.AddLightTheme());
                    break;
            };
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        if (ConfigManager.AppConfig.App.ShowAnnouncementWhenStart)
            new Thread(async () =>
            {
                try
                {
                    await AnouncementManager.CheckNewAnnouncements();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "In AnouncementManager.CheckNewAccnouncements()");
                }
            }).Start();

        base.OnFrameworkInitializationCompleted();
    }

    public static readonly Bitmap DefaultIcon = new(
        $"{GlobalInfo.AssetsPath}{ConfigManager.AppConfig.App.CoverIconFileName}".GetFullPath()
    );
}

//                                         .....'',;;::cccllllllllllllcccc:::;;,,,''...'',,'..
//                              ..';cldkO00KXNNNNXXXKK000OOkkkkkxxxxxddoooddddddxxxxkkkkOO0XXKx:.
//                        .':ok0KXXXNXK0kxolc:;;,,,,,,,,,,,;;,,,''''''',,''..              .'lOXKd'
//                   .,lx00Oxl:,'............''''''...................    ...,;;'.             .oKXd.
//                .ckKKkc'...'',:::;,'.........'',;;::::;,'..........'',;;;,'.. .';;'.           'kNKc.
//             .:kXXk:.    ..       ..................          .............,:c:'...;:'.         .dNNx.
//            :0NKd,          .....''',,,,''..               ',...........',,,'',,::,...,,.        .dNNx.
//           .xXd.         .:;'..         ..,'             .;,.               ...,,'';;'. ...       .oNNo
//           .0K.         .;.              ;'              ';                      .'...'.           .oXX:
//          .oNO.         .                 ,.              .     ..',::ccc:;,..     ..                lXX:
//         .dNX:               ......       ;.                'cxOKK0OXWWWWWWWNX0kc.                    :KXd.
//       .l0N0;             ;d0KKKKKXK0ko:...              .l0X0xc,...lXWWWWWWWWKO0Kx'                   ,ONKo.
//     .lKNKl...'......'. .dXWN0kkk0NWWWWWN0o.            :KN0;.  .,cokXWWNNNNWNKkxONK: .,:c:.      .';;;;:lk0XXx;
//    :KN0l';ll:'.         .,:lodxxkO00KXNWWWX000k.       oXNx;:okKX0kdl:::;'',;coxkkd, ...'. ...'''.......',:lxKO:.
//   oNNk,;c,'',.                      ...;xNNOc,.         ,d0X0xc,.     .dOd,           ..;dOKXK00000Ox:.   ..''dKO,
//  'KW0,:,.,:..,oxkkkdl;'.                'KK'              ..           .dXX0o:'....,:oOXNN0d;.'. ..,lOKd.   .. ;KXl.
//  ;XNd,;  ;. l00kxoooxKXKx:..ld:         ;KK'                             .:dkO000000Okxl;.   c0;      :KK;   .  ;XXc
//  'XXdc.  :. ..    '' 'kNNNKKKk,      .,dKNO.                                   ....       .'c0NO'      :X0.  ,.  xN0.
//  .kNOc'  ,.      .00. ..''...      .l0X0d;.             'dOkxo;...                    .;okKXK0KNXx;.   .0X:  ,.  lNX'
//   ,KKdl  .c,    .dNK,            .;xXWKc.                .;:coOXO,,'.......       .,lx0XXOo;...oNWNXKk:.'KX;  '   dNX.
//    :XXkc'....  .dNWXl        .';l0NXNKl.          ,lxkkkxo' .cK0.          ..;lx0XNX0xc.     ,0Nx'.','.kXo  .,  ,KNx.
//     cXXd,,;:, .oXWNNKo'    .'..  .'.'dKk;        .cooollox;.xXXl     ..,cdOKXXX00NXc.      'oKWK'     ;k:  .l. ,0Nk.
//      cXNx.  . ,KWX0NNNXOl'.           .o0Ooldk;            .:c;.':lxOKKK0xo:,.. ;XX:   .,lOXWWXd.      . .':,.lKXd.
//       lXNo    cXWWWXooNWNXKko;'..       .lk0x;       ...,:ldk0KXNNOo:,..       ,OWNOxO0KXXNWNO,        ....'l0Xk,
//       .dNK.   oNWWNo.cXK;;oOXNNXK0kxdolllllooooddxk00KKKK0kdoc:c0No        .'ckXWWWNXkc,;kNKl.          .,kXXk,
//        'KXc  .dNWWX;.xNk.  .kNO::lodxkOXWN0OkxdlcxNKl,..        oN0'..,:ox0XNWWNNWXo.  ,ONO'           .o0Xk;
//        .ONo    oNWWN0xXWK, .oNKc       .ONx.      ;X0.          .:XNKKNNWWWWNKkl;kNk. .cKXo.           .ON0;
//        .xNd   cNWWWWWWWWKOkKNXxl:,'...;0Xo'.....'lXK;...',:lxk0KNWWWWNNKOd:..   lXKclON0:            .xNk.
//        .dXd   ;XWWWWWWWWWWWWWWWWWWNNNNNWWNNNNNNNNNWWNNNNNNWWWWWNXKNNk;..        .dNWWXd.             cXO.
//        .xXo   .ONWNWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWNNK0ko:'..OXo          'l0NXx,              :KK,
//        .OXc    :XNk0NWXKNWWWWWWWWWWWWWWWWWWWWWNNNX00NNx:'..       lXKc.     'lONN0l.              .oXK:
//        .KX;    .dNKoON0;lXNkcld0NXo::cd0NNO:;,,'.. .0Xc            lXXo..'l0NNKd,.              .c0Nk,
//        :XK.     .xNX0NKc.cXXl  ;KXl    .dN0.       .0No            .xNXOKNXOo,.               .l0Xk;.
//       .dXk.      .lKWN0d::OWK;  lXXc    .OX:       .ONx.     . .,cdk0XNXOd;.   .'''....;c:'..;xKXx,
//       .0No         .:dOKNNNWNKOxkXWXo:,,;ONk;,,,,,;c0NXOxxkO0XXNXKOdc,.  ..;::,...;lol;..:xKXOl.
//       ,XX:             ..';cldxkOO0KKKXXXXXXXXXXKKKKK00Okxdol:;'..   .';::,..':llc,..'lkKXkc.
//       :NX'    .     ''            ..................             .,;:;,',;ccc;'..'lkKX0d;.
//       lNK.   .;      ,lc,.         ................        ..,,;;;;;;:::,....,lkKX0d:.
//      .oN0.    .'.      .;ccc;,'....              ....'',;;;;;;;;;;'..   .;oOXX0d:.
//      .dN0.      .;;,..       ....                ..''''''''....     .:dOKKko;.
//       lNK'         ..,;::;;,'.........................           .;d0X0kc'.
//       .xXO'                                                 .;oOK0x:.
//        .cKKo.                                    .,:oxkkkxk0K0xc'.
//          .oKKkc,.                         .';cok0XNNNX0Oxoc,.
//            .;d0XX0kdlc:;,,,',,,;;:clodkO0KK0Okdl:,'..
//                .,coxO0KXXXXXXXKK0OOxdoc:,..
//                          ...

