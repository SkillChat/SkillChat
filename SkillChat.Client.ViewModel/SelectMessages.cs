using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using Splat;
using System.Collections.ObjectModel;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SelectMessages
    {
        /// <summary>
        /// Переменная - флаг для вкл./выкл. режима выбора сообщений
        /// </summary>
        public bool IsTurnedSelectMode { get; set; }

        /// <summary>
        /// Коллекция для временного (оперативного) хранения выбранных сообщений
        /// </summary>
        public ObservableCollection<MessageViewModel> SelectedMessagesTempCollection { get; set; }

        /// <summary>
        /// Команда вызывается из контектного меню сообщения. Включает режим выбора сообщений, помечает это сообщение выбранным
        /// и добавляет в SelectedMessagesTempCollection
        /// </summary>
        public ICommand SelectedModeTurnOnCommand { get; }

        public SelectMessages()
        {
            SelectedMessagesTempCollection = new ObservableCollection<MessageViewModel>();


        }

    }
}