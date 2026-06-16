using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lytec.WinForms;

public class LyForm : System.Windows.Forms.Form
{
    public T Invoke<T>(Func<T> action) => (T)Invoke((Delegate)action);
}
