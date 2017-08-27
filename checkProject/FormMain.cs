using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace checkProject
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.Text = Application.ProductName;
            this.Text += " (check UseDebugLibraries)";
            try
            {
                // checkProject(@"C:\T\Win32Project1\Win32Project1\Win32Project1.vcxproj");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        string getSafeAttribute(XmlNode node, string attr)
        {
            if (node.Attributes[attr] == null)
                return string.Empty;

            return node.Attributes[attr].Value;
        }

        void checkProject(string file)
        {
            XmlDocument xmlDocument = new XmlDocument();

            StringBuilder sb = new StringBuilder();

            xmlDocument.Load(file);
            var nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("a", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlNodeList nodeList = xmlDocument.SelectNodes("//a:PropertyGroup",nsmgr);

            foreach(XmlNode node in nodeList)
            {
                if (getSafeAttribute(node,"Label") != "Configuration")
                    continue;

                string cond = node.Attributes["Condition"].Value;

                                
                if (cond.ToLower().IndexOf("debug") != -1)
                {
                    checkNode(node, sb, true);
                }
                else if (cond.ToLower().IndexOf("release") != -1)
                {
                    checkNode(node, sb, false);
                }
                else if (cond.ToLower().IndexOf("ship") != -1)
                {
                    checkNode(node, sb, false);
                }
                else
                {
                    sb.AppendLine("Unknown Node: " + cond);
                }
            }

            appendResult(sb.ToString());
        }

        void appendResult(string s)
        {
            txtResult.Text += s;
            txtResult.SelectionStart = txtResult.Text.Length; // add some logic if length is 0
            txtResult.SelectionLength = 0;
        }
        void checkNode(XmlNode node, StringBuilder sb, bool isdebug)
        {
            XmlNode udl = null;
            foreach (XmlNode cn in node.ChildNodes)
            {
                if (cn.Name == "UseDebugLibraries")
                {
                    udl = cn;
                    break;
                }
            }

            bool ok = false;
            if (isdebug)
            {
                ok = udl != null && udl.InnerText == "true";
            }
            else
            {
                ok = udl != null && udl.InnerText == "false";
            }

            if (ok)
            {
                sb.Append("OK");
            }
            else
            {
                sb.Append("NG");
            }
            sb.Append(" : ");

            string s = string.Format("{0}={1}", "Condition", node.Attributes["Condition"].Value);
            s += " ";
            s += string.Format("{0}={1}", "UseDebugLibraries", udl != null ? udl.InnerText : "NA");
            sb.AppendLine(s);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (DialogResult.OK != dlg.ShowDialog())
                    return;
                txtFolder.Text = dlg.SelectedPath;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                doStart(new DirectoryInfo(txtFolder.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        bool isFilterProject(string projname)
        {
            if (string.IsNullOrEmpty(txtFilter.Text))
                return false;

            if (-1 != projname.ToLower().IndexOf(txtFilter.Text.ToLower()))
            {
                // found text same as filter
                return false;
            }

            return true;
        }

        void doStart(DirectoryInfo di)
        {
            foreach (FileInfo proj in di.GetFiles("*.vcxproj"))
            {
                if (isFilterProject(proj.Name))
                    continue;
                appendResult(string.Format("===== analyzing {0} =====\r\n", proj.FullName));
                checkProject(proj.FullName);
            }

            foreach (DirectoryInfo childdi in di.GetDirectories())
            {
                doStart(childdi);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtResult.Text = string.Empty;
        }
    }
}
