using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public class CheckBoxEx : CheckBox
    {
        public enum OnDisableActions
        {
            DoNothing,
            CheckButNotUpdateCheckState,
            CheckAndUpdateCheckState,
            UncheckButNotUpdateCheckState,
            UncheckAndUpdateCheckState,
        }

        [Browsable(true)]
        [DefaultValue(typeof(OnDisableActions), nameof(OnDisableActions.DoNothing))]
        public OnDisableActions OnDisableAction { get; set; } = OnDisableActions.DoNothing;

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (OnDisableAction == OnDisableActions.UncheckAndUpdateCheckState)
                CheckState = CheckState.Unchecked;
            else if (OnDisableAction == OnDisableActions.CheckAndUpdateCheckState)
                CheckState = CheckState.Checked;
            base.OnEnabledChanged(e);
        }

        private bool DontEmitCheckEvents = false;

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if ((OnDisableAction == OnDisableActions.UncheckButNotUpdateCheckState
                || OnDisableAction == OnDisableActions.CheckButNotUpdateCheckState) && !Enabled)
            {
                DontEmitCheckEvents = true;
                var state = CheckState;
                try
                {
                    CheckState = OnDisableAction == OnDisableActions.UncheckButNotUpdateCheckState ? CheckState.Unchecked : CheckState.Checked;
                    base.OnPaint(pevent);
                }
                finally
                {
                    CheckState = state;
                    DontEmitCheckEvents = false;
                }
            }
            else base.OnPaint(pevent);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            if (DontEmitCheckEvents)
                return;
            base.OnCheckedChanged(e);
        }

        protected override void OnCheckStateChanged(EventArgs e)
        {
            if (DontEmitCheckEvents)
                return;
            base.OnCheckStateChanged(e);
        }
    }
}
