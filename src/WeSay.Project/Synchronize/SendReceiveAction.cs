using Chorus.FileTypeHandlers.lift;
using Chorus.UI.Sync;
using Chorus.VcsDrivers.Mercurial;
using Mono.Addins;
using SIL.i18n;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeSay.AddinLib;
using WeSay.Foundation;
using WeSay.Project.Properties;

namespace WeSay.Project.Synchronize
{
	[Extension]
	public class SendReceiveAction : IWeSayAddin//, IWeSayAddinHasSettings
	{
		public static string kId = "SendReceiveAction";
		private SendReceiveSettings _settings;

		public SendReceiveAction()
		{
			_settings = new SendReceiveSettings();
		}

		#region IWeSayAddin Members

		public Image ButtonImage
		{
			get { return Resources.sendReceive32x32; }
		}

		public bool Available
		{
			get { return string.IsNullOrEmpty(HgRepository.GetEnvironmentReadinessMessage("en")); }
		}

		public string LocalizedName
		{
			get
			{
				return StringCatalog.Get("~Send/Receive",
										 "Label for action which uses Chorus to synchronize the dictionary with other users, devices, and servers. Here, 'synchronizing' means putting your changes in the repository, and getting changes that others have put in the repository. In English, we are going with Send/Receive because it is similar to email and this is the term Paratext uses.");
			}
		}

		public string Description
		{
			get
			{
				return
					StringCatalog.Get(
						"~Sends your changes to a USB flash drive, server, or other users, and receives the changes made by other members of your team.",
						"description of send/receive action");
			}
		}

		public string ID
		{
			get { return kId; }
			set { throw new NotImplementedException(); }
		}

		#region IWeSayAddinHasSettings Members

		public bool DoShowSettingsDialog(Form parentForm, ProjectInfo projectInfo)
		{
			return false;
		}

		public object Settings
		{
			get { return _settings; }
			set { _settings = value as SendReceiveSettings; }
		}

		#endregion

		public void Launch(Form parentForm, ProjectInfo projectInfo)
		{
			var configuration = (Chorus.sync.ProjectFolderConfiguration)
								projectInfo.ServiceProvider.GetService(typeof(Chorus.sync.ProjectFolderConfiguration));
			using (var dlg = new SyncDialog(configuration,
										   SyncUIDialogBehaviors.Lazy,
										   SyncUIFeatures.NormalRecommended))
			{
				dlg.Text = "WeSay Send/Receive";
				dlg.SyncOptions.DoMergeWithOthers = true;
				dlg.SyncOptions.DoPullFromOthers = true;
				dlg.SyncOptions.DoSendToOthers = true;
				dlg.SetSynchronizerAdjunct(new LiftSynchronizerAdjunct(projectInfo.PathToLIFT));


				// leave it with the default, for now... dlg.SyncOptions.RepositorySourcesToTry.Clear();
				//dlg.SyncOptions.CheckinDescription = CheckinDescriptionBuilder.GetDescription();

				dlg.ShowDialog(parentForm);

				if (dlg.SyncResult != null && dlg.SyncResult.DidGetChangesFromOthers)
				{
					// This loop is an attempt to handle a collection-modified-while-iterating exception that is thrown in Application.Restart,
					// which http://social.msdn.microsoft.com/Forums/windows/en-US/5ad73551-81bd-4525-aa40-fe2772817a99/applicationrestart-throw-exception-collection-was-modified-enumeration-operation-may-not
					// attributes to closing a window in its OnClosing event. I can't find where we're doing this, but a
					// plausible workaround seems to be to close all the windows, using a loop which carefully copies the collection
					// first, and also tries to avoid closing ones that are already closed.
					foreach (Form form in Application.OpenForms.Cast<object>().Cast<Form>().ToList())
					{
						if (form.Visible)
							form.Close();
					}
					Application.Restart();
				}
			}

		}

		public bool Deprecated
		{
			get { return false; }
		}

		#endregion

		#region IThingOnDashboard Members

		public DashboardGroup Group
		{
			get { return DashboardGroup.Share; }
		}

		public string LocalizedLabel
		{
			get { return LocalizedName; }
		}

		public string LocalizedLongLabel
		{
			get
			{
				return StringCatalog.Get("~Send/Receive Changes to devices, servers, or other users on your network.",
										 "Long label for send/receive action.");
			}
		}

		public ButtonStyle DashboardButtonStyle
		{
			get { return ButtonStyle.IconVariableWidth; }
		}

		public Image DashboardButtonImage
		{
			get { return Resources.sendReceive32x32; }
		}

		#endregion

	}

	[Serializable]
	public class SendReceiveSettings
	{
		//        private string _pathToExecutable = @"$wesayApplicationDirectory\mercurial.bat";
		//        private string _arguments = @"$projectPath";
		//
		//        public string PathToExecutable
		//        {
		//            get { return _pathToExecutable; }
		//            set { _pathToExecutable = value; }
		//        }
		//
		//        public string Arguments
		//        {
		//            get { return _arguments; }
		//            set { _arguments = value; }
		//        }
		//
		//        public string GetRuntimeProcessPath(ProjectInfo info)
		//        {
		//            return _pathToExecutable.Replace("$wesayApplicationDirectory",
		//                                             info.PathToApplicationRootDirectory);
		//        }
		//
		//        public string GetRuntimeArguments(ProjectInfo info)
		//        {
		//            return _arguments.Replace("$projectPath", info.PathToTopLevelDirectory);
		//        }
	}
}
