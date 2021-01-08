using System;
using System.Collections.Generic;
using System.Text;
using WindowsInput;
using WindowsInput.Native;

namespace SkillChat.Client.ViewModel
{
    static public class Emoji
    {
        static public void OpenEmoji() 
        {
            InputSimulator inputSimulator = new InputSimulator();
            inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.OEM_PERIOD);
        }
    }
}
