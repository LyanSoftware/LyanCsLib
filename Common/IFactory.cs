using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common
{
    public interface IFactory<out T>
    {
        T Create();
    }
}
