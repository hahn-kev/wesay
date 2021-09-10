using Mono.Addins;
using System;
using System.Drawing;
using System.Windows.Forms;
using WeSay.Foundation;

namespace WeSay.AddinLib
{
	public delegate string FileLocater(string fileName);

	[TypeExtensionPoint]
	public interface IWeSayAddin : IThingOnDashboard
	{
		Image ButtonImage { get; }

		bool Available { get; }

		string LocalizedName { get; }

		String ID { get; }

		void Launch(Form parentForm, ProjectInfo projectInfo);

		/// <summary>
		/// Use when we want to remove an action from the offerings, but don't want to break people already using it
		/// </summary>
		bool Deprecated { get; }
	}

	public interface IWeSayAddinHasSettings
	{
		bool DoShowSettingsDialog(Form parentForm, ProjectInfo projectInfo);

		object Settings { get; set; }
	}

	public interface IWeSayAddinHasMoreInfo
	{
		void ShowMoreInfoDialog(Form parentForm);

	}
}