using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace SimpleCodeSnippet
{
	public class SimpleCodeSnippet
	{
		List<Snippet> snippets = new List<Snippet>();
		
		public List<Snippet> Snippets
		{
			get { return snippets; }
			set { snippets = value; }
		}
	}
	
	public class Snippet
	{
		string name;
		string content;
		
		public string Name
		{
			get { return name; }
			set { name = value; }
		}
		
		public string Content
		{
			get { return content; }
			set { content = value; }
		}
	}
	
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			string folderPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Application.ProductName);
			
			Directory.CreateDirectory(folderPath);
			
			string dataPath = Path.Combine(folderPath, Application.ProductName + ".xml");
			
			XmlSerializerFactory factory = new XmlSerializerFactory();
			XmlSerializer serializer = factory.CreateSerializer(typeof(SimpleCodeSnippet), new Type[]{ typeof(Snippet) });
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			ToolStripMenuItem renameMenu = new ToolStripMenuItem("Rename");
			renameMenu.ShortcutKeys = Keys.F2;
			
			ToolStripMenuItem deleteMenu = new ToolStripMenuItem("Delete");
			
			ToolStripMenuItem newMenu = new ToolStripMenuItem("New");
			newMenu.ShortcutKeys = Keys.Control | Keys.N;
			
			ContextMenuStrip menu = new ContextMenuStrip();
			
			menu.Items.Add(renameMenu);
			menu.Items.Add(deleteMenu);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(newMenu);
			
			List<ListViewItem> items = new List<ListViewItem>();
			
			if (File.Exists(dataPath))
			{
				using (FileStream stream = new FileStream(dataPath, FileMode.Open))
				{
					SimpleCodeSnippet simpleCodeSnippet = (SimpleCodeSnippet) serializer.Deserialize(stream);
					foreach (Snippet snippet in simpleCodeSnippet.Snippets)
					{
						ListViewItem item = new ListViewItem(snippet.Name);
						item.Tag = snippet.Content.Replace("\n", "\r\n");
						items.Add(item);
					}
				}
			}

			ListView filesListView = new ListView();
			filesListView.Dock = DockStyle.Fill;
			filesListView.LabelEdit = true;
			filesListView.GridLines = true;
			filesListView.FullRowSelect = true;
			filesListView.MultiSelect = false;
			filesListView.View = View.List;
			filesListView.ContextMenuStrip = menu;
			
			filesListView.Items.AddRange(items.ToArray());
			
			TextBox editorTextBox = new TextBox();
			editorTextBox.AcceptsTab = true;
			editorTextBox.Dock = DockStyle.Fill;
			editorTextBox.Multiline = true;
			editorTextBox.ScrollBars = ScrollBars.Vertical;
			editorTextBox.Enabled = false;
			editorTextBox.Font = new Font("Consolas", 10);
			
			SplitContainer mainContainer = new SplitContainer();
			mainContainer.Dock = DockStyle.Fill;
			mainContainer.FixedPanel = FixedPanel.Panel1;
			mainContainer.Panel1.Controls.Add(filesListView);
			mainContainer.Panel2.Controls.Add(editorTextBox);
			
			Form mainForm = new Form();
			mainForm.Text = Application.ProductName;
			mainForm.Padding = new Padding(3);
			mainForm.Size = new Size(700, 500);
			mainForm.StartPosition = FormStartPosition.CenterScreen;
			mainForm.Controls.Add(mainContainer);

			Timer timer = new Timer();
			timer.Interval = 3000;
			
			// Needs to be done here
			mainContainer.SplitterDistance = 200;
			
			EventHandler save = delegate
			{
				timer.Stop();
				timer.Start();
				mainForm.Text = Application.ProductName + "*";	
			};

			menu.Opening += delegate 
			{
				Point mouse = filesListView.PointToClient(Cursor.Position);
				filesListView.SelectedIndices.Clear();
				ListViewItem item = filesListView.GetItemAt(mouse.X, mouse.Y);
				if (item != null)
					filesListView.SelectedIndices.Add(item.Index);
			};
			
			filesListView.SelectedIndexChanged += delegate
			{
				if (editorTextBox.Enabled = filesListView.SelectedItems.Count > 0)
					editorTextBox.Text = (string) filesListView.SelectedItems[0].Tag;
				else
					editorTextBox.Clear();
			};
			
			filesListView.AfterLabelEdit += delegate
			{
				save(null, null);
			};
			
			editorTextBox.TextChanged += delegate
			{
				if (filesListView.SelectedItems.Count > 0 && editorTextBox.Modified)
				{
					filesListView.SelectedItems[0].Tag = editorTextBox.Text;
					save(null, null);
				}
			};
			
			renameMenu.Click += delegate
			{
				if (filesListView.SelectedItems.Count > 0)
					filesListView.SelectedItems[0].BeginEdit();
			};
			
			deleteMenu.Click += delegate
			{
				if (filesListView.SelectedItems.Count > 0)
				{
					filesListView.SelectedItems[0].Remove();
					save(null, null);
				}
			};
			
			newMenu.Click += delegate
			{
				ListViewItem item = new ListViewItem("Untitled");
				filesListView.Items.Add(item);
				item.BeginEdit();
				save(null, null);
			};
			
			timer.Tick += delegate
			{
				timer.Stop();
				SimpleCodeSnippet simpleCodeSnippet = new SimpleCodeSnippet();
				foreach (ListViewItem item in filesListView.Items)
				{
					Snippet snippet = new Snippet();
					snippet.Name = item.Text;
					snippet.Content = (string) item.Tag;
					simpleCodeSnippet.Snippets.Add(snippet);
				}
				using (FileStream stream = new FileStream(dataPath + "_", FileMode.Create))
					serializer.Serialize(stream, simpleCodeSnippet);
				File.Delete(dataPath);
				File.Move(dataPath + "_", dataPath);
				mainForm.Text = Application.ProductName;
			};
			
			Application.Run(mainForm);
		}
	}
}
