using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Lytec.Wpf;

public static class DragDropExtensions
{
    public static bool TryGetFileNames(this DragEventArgs args, out string[] FileNames)
    {
        FileNames = Array.Empty<string>();
        if (args.Data.GetDataPresent(DataFormats.FileDrop)
            && args.Data.GetData(DataFormats.FileDrop) is string[] fs)
        {
            FileNames = fs;
            return true;
        }
        return false;
    }
}
