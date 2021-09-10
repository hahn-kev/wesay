using SIL.i18n;
using SIL.Reporting;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace WeSay.AddinLib
{
	public partial class ActionItemControl : UserControl //, IControlForListBox
	{
		private readonly IWeSayAddin _addin;
		private readonly bool _inAdminMode;
		private readonly ProjectInfo _projectInfo;
		// private bool _showInWeSay;
		public event EventHandler Launch;

		//        public ActionItemControl(bool inAdminMode)
		//        {
		//            _inAdminMode = inAdminMode;
		//            InitializeComponent();
		//            UpdateVisualThings();
		//            _toggleShowInWeSay.Visible = inAdminMode;
		//            _setupButton.Visible = inAdminMode;
		//        }

		public bool DoShowInWeSay
		{
			get { return AddinSet.Singleton.DoShowInWeSay(_addin.ID); }
			set { AddinSet.Singleton.SetDoShowInWeSay(_addin.ID, value); }
		}

		private static bool ReturnFalse()
		{
			return false;
		}

		public ActionItemControl(IWeSayAddin addin, bool inAdminMode, ProjectInfo projectInfo)
		{
			_addin = addin;
			_inAdminMode = inAdminMode;
			_projectInfo = projectInfo;
			InitializeComponent();
			_description.Font = (Font)StringCatalog.LabelFont.Clone();

			_toggleShowInWeSay.Visible = inAdminMode;
			_setupButton.Visible = inAdminMode;

			if (addin.ButtonImage != null)
			{
				//review: will these be disposed when the button is disposed?
				_launchButton.Image = addin.ButtonImage.GetThumbnailImage(_launchButton.Width - 10,
																		  _launchButton.Height - 10,
																		  ReturnFalse,
																		  IntPtr.Zero);
			}
			LoadSettings();

			UpdateVisualThings();

			//nb: we do want to show the setup, even if the addin says unavailable.
			//Maybe that's *why it's unavailable*.. it may need to be set up first.

			_setupButton.Visible = inAdminMode && _addin is IWeSayAddinHasSettings &&
								   ((IWeSayAddinHasSettings)_addin).Settings != null;


			_moreInfoButton.Visible = inAdminMode && _addin is IWeSayAddinHasMoreInfo;
		}

		public void Draw(Graphics graphics, Rectangle bounds)
		{
			//this.InvokePaint(this, new PaintEventArgs(graphics, bounds));
		}

		public int GetHeight(int width)
		{
			return Height;
		}

		private void _launchButton_Click(object sender, EventArgs e)
		{
			if (Launch != null)
			{
				Cursor = Cursors.WaitCursor;
				Launch.Invoke(_addin, null);
				Cursor = Cursors.Default;
			}
		}

		private void LoadSettings()
		{
			if (!(_addin is IWeSayAddinHasSettings))
			{
				return;
			}
			IWeSayAddinHasSettings addin = (IWeSayAddinHasSettings)_addin;
			object existingSettings = addin.Settings;
			if (existingSettings == null)
			{
				return; // this class doesn't do settings
			}

			//this is not necessarily the right place for this deserialization to be happening
			string settings = AddinSet.Singleton.GetSettingsXmlForAddin(_addin.ID);
			if (!String.IsNullOrEmpty(settings))
			{
				XmlSerializer x = new XmlSerializer(existingSettings.GetType());
				using (StringReader r = new StringReader(settings))
				{
					addin.Settings = x.Deserialize(r);
				}
			}
		}

		private void OnSetupClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			try
			{
				LoadSettings();
				IWeSayAddinHasSettings addin = (IWeSayAddinHasSettings)_addin;
				if (!addin.DoShowSettingsDialog(ParentForm, _projectInfo))
				{
					return;
				}

				object settings = addin.Settings;
				if (settings == null)
				{
					return;
				}
				XmlSerializer serializer = new XmlSerializer(settings.GetType());
				StringBuilder builder = new StringBuilder();
				XmlWriterSettings writerSettings = new XmlWriterSettings();
				writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
				//we don't want the <xml header
				using (StringWriter stringWriter = new StringWriter(builder))
				{
					using (XmlTextWriter writer = new FragmentXmlTextWriter(stringWriter))
					{
						writer.Formatting = Formatting.Indented;
						XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
						ns.Add("", ""); //prevent writing namespaces
						serializer.Serialize(writer, settings, ns);
						writer.Close();
					}
					string settingsXml = builder.ToString();
					stringWriter.Close();
					AddinSet.Singleton.SetSettingsForAddin(((IWeSayAddin)addin).ID, settingsXml);
				}
			}
			catch (Exception error)
			{
				ErrorReport.NotifyUserOfProblem(
						"Sorry, WeSay had a problem storing those settings. {0}", error.Message);
			}

			UpdateVisualThings();
		}

		private void _description_TextChanged(object sender, EventArgs e)
		{
			Size size = new Size(_description.ClientSize.Width, int.MaxValue);
			TextFormatFlags flags = TextFormatFlags.WordBreak;
			size = TextRenderer.MeasureText(_description.Text, _description.Font, size, flags);
			// TextRenderer.MeasureText does not make room for initial line, so we add font height. We also need enough
			// room for border and padding on both top and bottom, instead we add another font height as a hack.
			_description.Height = size.Height + (2 * _description.Font.Height);
			flowLayoutPanel1.Height = _description.Top + _description.Height;
			this.OnResize(e);
		}

		protected override void OnResize(EventArgs e)
		{
			if (flowLayoutPanel1.Height > flowLayoutPanel1.ClientSize.Height)
				flowLayoutPanel1.Size = new Size(flowLayoutPanel1.ClientSize.Width, flowLayoutPanel1.Height);
			if (flowLayoutPanel1.Height > this.ClientSize.Height)
				this.Size = new Size(this.ClientSize.Width, flowLayoutPanel1.Height);
			base.OnResize(e);
		}

		protected void UpdateVisualThings()
		{
			UpdateEnabledStates();
			_actionName.Text = _addin.LocalizedName;
			_actionName.Font = (Font)StringCatalog.LabelFont.Clone();
			_description.Text = _addin.Description;
			_description.Font = (Font)StringCatalog.LabelFont.Clone();

			if (_inAdminMode && !DoShowInWeSay)
			{
				_toggleShowInWeSay.Text = "Make visible in WeSay";
				_toolTip.SetToolTip(_toggleShowInWeSay,
									"Click to make this action available within WeSay.");
				//                e.Graphics.DrawLine(Pens.Red, new Point(0,0), new Point(_toggleShowInWeSay.Width,_toggleShowInWeSay.Height));
				//                e.Graphics.DrawLine(Pens.Red, new Point(0, _toggleShowInWeSay.Height), new Point(_toggleShowInWeSay.Width, 0));
			}
			else
			{
				_toggleShowInWeSay.Text = "Make invisible in WeSay";
				_toolTip.SetToolTip(_toggleShowInWeSay,
									"Click to make this action unavailable within WeSay.");
			}
		}

		private void UpdateEnabledStates()
		{
			_toggleShowInWeSay.Visible = _addin.Available && _inAdminMode;
			if (!_addin.Available)
			{
				_actionName.ForeColor = Color.Gray;
				_description.ForeColor = Color.Gray;
				_launchButton.Enabled = false;
				//  _setupButton.Visible = false;
			}
			else
			{
				_actionName.ForeColor = SystemColors.WindowText;
				_description.ForeColor = SystemColors.WindowText;
				_launchButton.Enabled = true;
			}
		}

		private void _toggleShowInWeSay_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			DoShowInWeSay = !DoShowInWeSay;
			UpdateVisualThings();
			//_toggleShowInWeSay.Invalidate();
		}

		private void OnMoreInfo(object sender, LinkLabelLinkClickedEventArgs e)
		{
			((IWeSayAddinHasMoreInfo)_addin).ShowMoreInfoDialog(ParentForm);
		}
	}

	//lets us serialize to an xml fragment
	internal class FragmentXmlTextWriter : XmlTextWriter
	{
		public FragmentXmlTextWriter(TextWriter w) : base(w) { }
		public FragmentXmlTextWriter(Stream w, Encoding encoding) : base(w, encoding) { }
		public FragmentXmlTextWriter(string filename, Encoding encoding) : base(filename, encoding) { }

		public override void WriteStartDocument() { }
	}
}