using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BoughtItems
{
    public class LogController
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        private LogController()
        {
            //do nothing
            myTextBox = null;
        }

        private static LogController _instance;
        private TextBox myTextBox;
        private ProgressBar bar;
        private Label labelPercentage;
        public string CurrentURL;

        public static LogController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LogController();
                }
                return _instance;
            }
        }

        public void SetTextBox(TextBox t)
        {
            this.myTextBox = t;
        }

        public void SetProgressBar(ProgressBar b)
        {
            this.bar = b;
        }

        public void SetLabelPercentage(Label b)
        {
            this.labelPercentage = b;
        }

        public void SetValueProgress(int t, string additionText = "")
        {
            if (t < 0)
            {
                t = 0;
            }
            else if (t > 100)
            {
                t = 100;
            }
            bar.Value = t;
            labelPercentage.Content = t + "% " + additionText;
        }

        private void PrintToTextbox(string s, string tag = "")
        {
            log.Debug("Textbox: " + s);
            string message = DateTime.Now.ToString("HH:mm:ss") + tag + " - " + s + Environment.NewLine;
            if (myTextBox != null)
            {
                if (!myTextBox.Dispatcher.CheckAccess())
                {
                    myTextBox.Dispatcher.BeginInvoke(new Action(() => myTextBox.AppendText(message)));
                }
                else
                {
                    myTextBox.AppendText(message);
                }
            }
        }

        public void Error(string s)
        {
            PrintToTextbox(s, " Error");
        }

        public void Debug(string s)
        {
            PrintToTextbox(s);
        }
    }
}
