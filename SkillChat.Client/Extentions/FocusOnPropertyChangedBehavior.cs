using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using System;

namespace SkillChat.Client.Extentions
{
	/// <summary>Фокусировка контрола после его INPC события</summary>
	public class FocusOnPropertyChangedBehavior : Behavior<Control>
	{
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PropertyChanged += FocuseControl;
        }

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.PropertyChanged -= FocuseControl;
		}

		private void FocuseControl(object sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (!AssociatedObject.IsFocused)
			{
				AssociatedObject.Focus();
			}
		}
	}
}
