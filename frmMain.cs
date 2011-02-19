using System;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Resizer
{
    public partial class frmMain : Form
    {
        const int MAXTITLE = 255;
        private static ArrayList mTitlesList;
        private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool _EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int _GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static int GWL_STYLE = -16;
        private static uint WS_CAPTION = 0xC00000;
        private static uint WS_BORDER = 0x800000;
        private static uint SWP_NOZORDER = 0x0004;
        private static uint SWP_NOMOVE = 0x0002;
        private static uint SWP_DRAWFRAME = 0x0020;
        private static uint SWP_NOACTIVATE = 0x0010;
        private static uint SWP_NOREDRAW = 0x0008;
        private static uint SWP_NOREPOSITION = 0x0200;
        private static uint SWP_NOSIZE = 0x0001;

        private bool CLOSENOW = false;

        public frmMain(string[] args)
        {
            InitializeComponent();
            // Find out how many monitors are connected
            for(int x = 0; x < Screen.AllScreens.Length; x++)
            {
                if (Screen.AllScreens[x].Primary)
                {
                    cboMonitors.Items.Add("Monitor " + (x + 1).ToString() + " (Primary)");
                    cboMonitors.SelectedIndex = x;
                }
                else
                {
                    cboMonitors.Items.Add("Monitor " + (x + 1).ToString());
                }
            }
            PopulateWindowList();
            if(args.Length>=1)
                ParseCommandLineArguments(args);
        }

        void ParseCommandLineArguments(string[] args)
        {
            try
            {
                foreach (string arg in args)
                {
                    if (arg.Length > 7 && arg.Substring(0, 7) == "-title:")
                    {
                        for (int x = 0; x < lstWindows.Items.Count; x++)
                        {
                            if ((string)lstWindows.Items[x] == arg.Substring(7))
                            {
                                lstWindows.SelectedIndex = x;
                            }
                        }
                    }
                    if (arg.Length > 8 && arg.Substring(0, 8) == "-params:")
                    {
                        string[] a = arg.Substring(8).Split(',');
                        for (int x = 0; x < 9; x++)
                        {
                            if (a[x] == "0")
                                a[x] = "False";
                            else
                                a[x] = "True";
                        }
                        checkBox1.Checked = Convert.ToBoolean(a[0]);
                        checkBox2.Checked = Convert.ToBoolean(a[1]);
                        checkBox3.Checked = Convert.ToBoolean(a[2]);
                        checkBox4.Checked = Convert.ToBoolean(a[3]);
                        checkBox5.Checked = Convert.ToBoolean(a[4]);
                        checkBox6.Checked = Convert.ToBoolean(a[5]);
                        checkBox7.Checked = Convert.ToBoolean(a[6]);
                        checkBox8.Checked = Convert.ToBoolean(a[7]);
                        checkBox9.Checked = Convert.ToBoolean(a[8]);
                        txtWidth.Text = a[9];
                        txtHeight.Text = a[10];
                        txtLeft.Text = a[11];
                        txtTop.Text = a[12];
                        cboMonitors.SelectedIndex = Convert.ToInt32(a[13]);
                    }
                    if (arg.Length >= 6 && arg.Substring(0, 6) == "-apply")
                    {
                        btnApply_Click(null, null);
                        CLOSENOW = true;
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show("There was a problem parsing one or more command line parameters.");
            }
        }

        void PopulateWindowList()
        {
            lstWindows.Items.Clear();
            string[] desktopWindowsCaptions = GetDesktopWindowsCaptions();
            foreach (string caption in desktopWindowsCaptions)
            {
                if(caption.Length > 0)
                    lstWindows.Items.Add(caption);
            }
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            StringBuilder title = new StringBuilder(MAXTITLE);
            int titleLength = _GetWindowText(hWnd, title, title.Capacity + 1);
            title.Length = titleLength;

            return title.ToString();
        }

        private static bool EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            string title = GetWindowText(hWnd);
            mTitlesList.Add(title);
            return true;
        }

        public static string[] GetDesktopWindowsCaptions()
        {
            mTitlesList = new ArrayList();
            EnumDelegate enumfunc = new EnumDelegate(EnumWindowsProc);
            IntPtr hDesktop = IntPtr.Zero; // current desktop
            bool success = _EnumDesktopWindows(hDesktop, enumfunc, IntPtr.Zero);

            if (success)
            {
                // Copy the result to string array
                string[] titles = new string[mTitlesList.Count];
                mTitlesList.CopyTo(titles);
                return titles;
            }
            else
            {
                // Get the last Win32 error code
                int errorCode = Marshal.GetLastWin32Error();

                string errorMessage = String.Format(
                "EnumDesktopWindows failed with code {0}.", errorCode);
                throw new Exception(errorMessage);
            }
        }

        void RunChanger(string windowTitle, bool noCaption, bool noBorder, bool drawFrame, bool noActivate, bool noMove, bool noRedraw, bool noReposition, bool noSize, bool noZOrder, int width, int height, int mx, int my)
        {
            IntPtr hWnd = FindWindow(null, windowTitle);
            // Set Styles
            uint uStyles = GetWindowLongPtr(hWnd, GWL_STYLE);
            if (noCaption)
                uStyles &= ~WS_CAPTION;
            else
                uStyles = uStyles | WS_CAPTION;
            if (noBorder)
                uStyles &= ~WS_BORDER;
            else
                uStyles = uStyles | WS_BORDER;
            SetWindowLongPtr(hWnd, GWL_STYLE, uStyles);
            // Set Window Position
            uint uFlags = 0;
            if (drawFrame)
                uFlags = uFlags | SWP_DRAWFRAME;
            if(noActivate)
                uFlags = uFlags | SWP_DRAWFRAME;
            if(noMove)
                uFlags = uFlags | SWP_NOMOVE;
            if(noRedraw)
                uFlags = uFlags | SWP_DRAWFRAME;
            if(noReposition)
                uFlags = uFlags | SWP_DRAWFRAME;
            if(noSize)
                uFlags = uFlags | SWP_DRAWFRAME;
            if(noZOrder)
                uFlags = uFlags | SWP_NOZORDER;
            // Set !
            SetWindowPos(hWnd, 0, mx, my, width, height, uFlags);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            PopulateWindowList();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            RunChanger(lstWindows.SelectedItem.ToString(), checkBox1.Checked,
                checkBox2.Checked, checkBox3.Checked, checkBox4.Checked,
                checkBox5.Checked, checkBox6.Checked, checkBox7.Checked,
                checkBox8.Checked, checkBox9.Checked, Convert.ToInt32(txtWidth.Text), Convert.ToInt32(txtHeight.Text),
                Convert.ToInt32(txtLeft.Text), Convert.ToInt32(txtTop.Text));
        }

        private void cboMonitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get width and height of monitor
            int index = cboMonitors.SelectedIndex;
            txtWidth.Text = Screen.AllScreens[index].Bounds.Width.ToString();
            txtHeight.Text = Screen.AllScreens[index].Bounds.Height.ToString();
            txtTop.Text = Screen.AllScreens[index].Bounds.Y.ToString();
            txtLeft.Text = Screen.AllScreens[index].Bounds.X.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int index = cboMonitors.SelectedIndex;
            MessageBox.Show(Screen.AllScreens[index].Bounds.X.ToString() + ", " + Screen.AllScreens[index].Bounds.Y.ToString());
        }

        private void btnCreateParams_Click(object sender, EventArgs e)
        {
            try
            {
                string title = lstWindows.SelectedItem.ToString();
                string args = "";
                if (checkBox1.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox2.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox3.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox4.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox5.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox6.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox7.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox8.Checked) { args += "1,"; } else { args += "0,"; }
                if (checkBox9.Checked) { args += "1,"; } else { args += "0,"; }
                args += txtWidth.Text + "," + txtHeight.Text + ",";
                args += txtLeft.Text + "," + txtTop.Text + ",";
                args += cboMonitors.SelectedIndex.ToString();

                string P = "-title:\"" + title + "\" -params:\"" + args + "\" -apply";

                Clipboard.SetText(P);
                MessageBox.Show("The parameters for the shortcut are:\n\n" + P + "\n\nThey have been placed on your clipboard so you can paste them.");
            }
            catch (Exception err)
            {
                MessageBox.Show("Please select a window title, then try again.");
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            if (CLOSENOW)
                Application.Exit();
        }
    }
}
