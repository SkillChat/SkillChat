using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Client.Extentions
{
	public class ScrollViewerBehavior : Behavior<ScrollViewer>
	{
		protected override void OnAttached()
		{
			AssociatedObject.PropertyChanged += GoToLastMessage;
		}

		protected override void OnDetaching()
		{
			AssociatedObject.Initialized -= GoToLastMessage;
		}

		private void GoToLastMessage(object sender, EventArgs e)
		{
			AssociatedObject.ScrollToEnd();
		}
	}
}
