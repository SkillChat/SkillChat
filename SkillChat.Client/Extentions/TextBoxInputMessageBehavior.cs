using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Client.Extentions
{
	public class TextBoxInputMessageBehavior : Behavior<TextBox>
	{
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.KeyDown += NewLineMethod;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.KeyDown -= NewLineMethod;
		}

		/// <summary>Переопределение стандартного поеведения по нажатиям на клавиши клавиатуры у поля ввода сообщений</summary>
		/// <param name="sender">Инициатор (ТекстБокс)</param>
		/// <param name="e">Переданные параметры</param>
		public void NewLineMethod(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Control)
			{
				AssociatedObject.Text += Environment.NewLine;
			}
		}
	}
}
