using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lytec.Wpf;

[Flags]
public enum ClickTrigger
{
    None = 0,

    Left = 1 << 0,
    Right = 1 << 1,
    Middle = 1 << 2,
    XButton1 = 1 << 3,
    XButton2 = 1 << 4,

    Touch = 1 << 5,
    Stylus = 1 << 6,

    Mouse = Left | Right | Middle | XButton1 | XButton2,

    All = Mouse | Touch | Stylus,
}
