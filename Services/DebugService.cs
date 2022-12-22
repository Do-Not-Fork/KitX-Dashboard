﻿using System.Collections.Generic;
using System.Reflection;

namespace KitX_Dashboard.Services
{
    internal class DebugService
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <returns>执行结果</returns>
        internal static string? ExecuteCommand(string cmd)
        {
            var header = cmd.GetCommandHeader();
            if (header is null) return null;
            var args = cmd.Trim()[header.Length..].GetCommandArgs();

            return header switch
            {
                "version" => Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(),
                _ => null,
            };
        }
    }

    internal static class DebugServiceTool
    {
        /// <summary>
        /// 获取命令头
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <returns>命令头</returns>
        internal static string? GetCommandHeader(this string cmd)
        {
            var command = cmd.Trim();
            if (command is null) return null;
            var header = command.Split(' ')[0];
            return header;
        }

        /// <summary>
        /// 获取命令参数
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <returns>参数字典</returns>
        internal static Dictionary<string, string>? GetCommandArgs(this string cmd)
        {
            var args = new Dictionary<string, string>();
            var command = cmd.Trim();
            var pairs = command?.Split(' ');
            if (pairs is null) return null;
            if (pairs.Length % 2 != 0) return null;
            for (var i = 0; i < pairs.Length; i += 2)
            {
                if (!pairs[i].Trim().StartsWith("--")) return null;
                args.Add(pairs[i].Trim(), pairs[i + 1].Trim());
            }
            return args;
        }

        /// <summary>
        /// 获取字典中的值
        /// </summary>
        /// <param name="src">字典</param>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        internal static string? Value(this Dictionary<string, string> src, string key)
            => src.ContainsKey(key) ? src[key] : null;
    }
}
