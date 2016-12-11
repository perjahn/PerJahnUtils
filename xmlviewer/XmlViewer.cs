using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace xmlviewer
{
    public partial class XmlViewer : Form
    {
        public XmlViewer()
        {
            InitializeComponent();
        }

        private void XmlViewer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])(e.Data.GetData(DataFormats.FileDrop));
                if (files.Length == 1 && File.Exists(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void XmlViewer_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])(e.Data.GetData(DataFormats.FileDrop));
                if (files.Length == 1 && File.Exists(files[0]))
                {
                    XDocument xdoc;
                    try
                    {
                        xdoc = XDocument.Load(files[0]);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException)
                    {
                        this.Text = files[0] + ": " + ex.Message;
                        return;
                    }

                    treeView1.Nodes.Clear();

                    this.Text = files[0] + ": " + xdoc.Descendants().Count() + " elements.";

                    AddNode(treeView1.Nodes, xdoc.Root);
                }
            }
        }

        private void AddNode(TreeNodeCollection parentTreeNodes, XElement element)
        {
            StringBuilder sb = new StringBuilder();

            string attrstring = string.Join(" ", element.Attributes().Select(a => a.Name + "=\"" + a.Value + "\""));
            if (attrstring != string.Empty)
            {
                sb.Append(" " + attrstring);
            }

            string innertext = element.Value;
            if (innertext != string.Empty)
            {
                sb.Append(" Value=\"" + innertext + "\"");
            }

            TreeNode treenode = new TreeNode();
            treenode.Text = element.Name.LocalName + sb.ToString();
            parentTreeNodes.Add(treenode);

            foreach (XElement child in element.Descendants())
            {
                AddNode(treenode.Nodes, child);
            }

            treenode.Expand();
        }
    }
}
