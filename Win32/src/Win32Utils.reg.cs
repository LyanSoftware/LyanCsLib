using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lytec.Win32;

public static partial class Win32Utils
{
    #region 辅助函数

    /// <summary>
    /// 读dword
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static int? GetDWord(this RegistryKey Reg, string name)
    {
        try
        {
            if (Reg.GetValueKind(name) == RegistryValueKind.DWord)
                return (int)Reg.GetValue(name)!;
        }
        catch (IOException) { }
        return null;
    }

    /// <summary>
    /// 读string
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string? GetString(this RegistryKey Reg, string name)
    {
        try
        {
            if (Reg.GetValueKind(name) == RegistryValueKind.String)
                return (string)Reg.GetValue(name)!;
        }
        catch (IOException) { }
        return null;
    }

    /// <summary>
    /// 写dword
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SetDWord(this RegistryKey Reg, string name, int value)
    {
        try
        {
            Reg.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch (ArgumentException)
        {
            Reg.DeleteValue(name, false);
            Reg.SetValue(name, value, RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// 写string
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SetString(this RegistryKey Reg, string name, string value)
    {
        try
        {
            if (value == null)
                Reg.DeleteValue(name, false);
            else Reg.SetValue(name, value, RegistryValueKind.String);
        }
        catch (ArgumentException)
        {
            Reg.DeleteValue(name, false);
            Reg.SetValue(name, value, RegistryValueKind.String);
        }
    }

    /// <summary>
    /// 设置自启动项
    /// </summary>
    /// <param name="name">启动项名称</param>
    /// <param name="command">启动项命令行</param>
    /// <param name="autoStart">是否自启动</param>
    /// <param name="onLocalMachine">设置到所有用户（Local Machine）</param>
    public static void SetAutoRun(string name, string command, bool autoStart = true, bool onLocalMachine = false)
    {
        using var reg = (onLocalMachine ? Registry.LocalMachine : Registry.CurrentUser).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (reg == null)
            return;
        try
        {
            if (autoStart)
                reg.SetValue(name, command);
            else reg.DeleteValue(name);
        }
        catch (Exception) { }
    }

    public static bool GetAutoRun(string name, string command, bool onLocalMachine = false)
    {
        using var reg = (onLocalMachine ? Registry.LocalMachine : Registry.CurrentUser).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
        if (reg == null)
            return false;
        return reg.GetString(name) != null;
    }

    #endregion

}
