using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MsgBox = System.Windows.Forms.MessageBox;
using MsgBoxBtns = System.Windows.Forms.MessageBoxButtons;
using MsgBoxDefBtns = System.Windows.Forms.MessageBoxDefaultButton;
using MsgBoxIcons = System.Windows.Forms.MessageBoxIcon;
using DialogResult = System.Windows.Forms.DialogResult;
using Lytec.Common;
using Lytec.Common.Data;
using System.Windows.Interop;
using System.Diagnostics.CodeAnalysis;

namespace Lytec.Wpf;

public partial class OpenFileDialogRequest : RequestMessage<string[]>
{
    public bool AllowMultiSelect { get; set; } = false;
    public string Filter { get; set; } = "*.*|*.*";
}

public class SaveFileDialogRequest : RequestMessage<string>
{
    public string Filter { get; set; } = "*.*|*.*";
    public string DefaultFileName { get; set; } = "";
}

public enum MsgBoxBtn
{
    OK,
    OKCancel,
    AbortRetryIgnore,
    YesNoCancel,
    YesNo,
    RetryCancel,
}

public enum DlgResult
{
    None,
    OK,
    Cancel,
    Abort,
    Retry,
    Ignore,
    Yes,
    No,
}

public enum MsgBoxDefBtn
{
    Btn1 = 0,
    Btn2 = 0x100,
    Btn3 = 0x200,
}

public enum MsgBoxIcon
{
    None = 0,
    Hand = 0x10,
    Question = 0x20,
    Exclamation = 48,
    Asterisk = 0x40,
    Stop = 0x10,
    Error = 0x10,
    Warning = 48,
    Information = 0x40,
}

public class MsgBoxRequest : RequestMessage<DlgResult>
{
    public string Text { get; set; } = "";
    public string Caption { get; set; } = "";
    public MsgBoxBtn Button { get; set; } = MsgBoxBtn.OK;
    public MsgBoxIcon Icon { get; set; } = MsgBoxIcon.None;
    public MsgBoxDefBtn DefaultButton { get; set; } = MsgBoxDefBtn.Btn1;

    private static readonly IReadOnlyDictionary<MsgBoxBtn, MsgBoxBtns> Btns
    = Enum.GetValues(typeof(MsgBoxBtns))
        .Cast<MsgBoxBtns>()
        .DistinctBy(x => (int)x)
        .ToDictionary(x => (MsgBoxBtn)(int)x);
    
    private static readonly IReadOnlyDictionary<MsgBoxIcon, MsgBoxIcons> Icons
    = Enum.GetValues(typeof(MsgBoxIcons))
        .Cast<MsgBoxIcons>()
        .DistinctBy(x => (int)x)
        .ToDictionary(x => (MsgBoxIcon)(int)x);
    
    private static readonly IReadOnlyDictionary<MsgBoxDefBtn, MsgBoxDefBtns> DefBtns
    = Enum.GetValues(typeof(MsgBoxDefBtns))
        .Cast<MsgBoxDefBtns>()
        .DistinctBy(x => (int)x)
        .ToDictionary(x => (MsgBoxDefBtn)(int)x);
    
    private static readonly IReadOnlyDictionary<DialogResult, DlgResult> Rets
    = Enum.GetValues(typeof(DlgResult))
        .Cast<DlgResult>()
        .DistinctBy(x => (int)x)
        .ToDictionary(x => (DialogResult)(int)x);

    public DlgResult ShowDialog()
    => Rets[MsgBox.Show(
        Text,
        Caption,
        Btns[Button],
        Icons[Icon],
        DefBtns[DefaultButton])];
    public DlgResult ShowDialog(System.Windows.Forms.IWin32Window owner)
    => Rets[MsgBox.Show(
        owner,
        Text,
        Caption,
        Btns[Button],
        Icons[Icon],
        DefBtns[DefaultButton])];
    public DlgResult ShowDialog(Window owner)
    {
        // 获取窗口句柄
        var handle = new WindowInteropHelper(owner).Handle;
        // 将句柄包装为 IWin32Window
        return ShowDialog(System.Windows.Forms.Control.FromHandle(handle));
    }
}

public interface IMsgBuilder<TMessage> where TMessage : class
{
    public TMessage Build();
}

public static class MsgBuilderUtils
{
    public static TMessage BuildAndSend<TMessage>(this IMsgBuilder<TMessage> builder)
        where TMessage : class
    {
        return WeakReferenceMessenger.Default.Send(builder.Build());
    }

    public static TMessage BuildAndSend<TMessage, TToken>(this IMsgBuilder<TMessage> builder, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        return WeakReferenceMessenger.Default.Send(builder.Build(), token);
    }
}

public static class MsgUtils
{
    public static bool GetResponse<TResult>(this RequestMessage<TResult> msg, [NotNullWhen(true)] out TResult? Result)
    {
        Result = default;
        if (msg.HasReceivedResponse)
            Result = msg.Response;
        return msg.HasReceivedResponse;
    }
}

//[BuilderFor(typeof(OpenFileDialogRequest))]
//public partial class OpenFileRequestBuilder : IMsgBuilder<OpenFileDialogRequest> { }

//[BuilderFor(typeof(SaveFileDialogRequest))]
//public partial class SaveFileRequestBuilder : IMsgBuilder<SaveFileDialogRequest> { }

//[BuilderFor(typeof(MsgBoxRequest))]
//public partial class MsgBoxRequestBuilder : IMsgBuilder<MsgBoxRequest>
//{
//    public static MsgBoxRequestBuilder CreateInfoBox(Func<string, string>? i18n = null)
//    {
//        i18n ??= str => str;
//        return new MsgBoxRequestBuilder()
//            .WithCaption(i18n("Info"))
//            .WithIcon(MsgBoxIcon.Information);
//    }
//    public static MsgBoxRequestBuilder CreateWarnBox(Func<string, string>? i18n = null)
//    {
//        i18n ??= str => str;
//        return new MsgBoxRequestBuilder()
//            .WithCaption(i18n("Warning"))
//            .WithIcon(MsgBoxIcon.Warning);
//    }
//    public static MsgBoxRequestBuilder CreateErrBox(Func<string, string>? i18n = null)
//    {
//        i18n ??= str => str;
//        return new MsgBoxRequestBuilder()
//            .WithCaption(i18n("Error"))
//            .WithIcon(MsgBoxIcon.Error);
//    }
//}
